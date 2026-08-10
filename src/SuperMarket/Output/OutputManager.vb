Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text
Imports SuperMarket.Core
Imports SuperMarket.Entities

Namespace SuperMarket.Output

    ''' <summary>
    ''' 输出管理器：协调所有数据的缓冲与写入，维护输出目录结构。
    ''' 每步缓冲数据，定期刷盘；模拟结束后写入拓扑 JSON 和事件流 JSON。
    ''' </summary>
    Public Class OutputManager

        Private ReadOnly _config As SimulationConfig
        Private _rootPath As String

        ' CSV 写入器字典：Key=文件相对路径
        Private _writers As New Dictionary(Of String, CsvWriter)()

        ' 事件流缓冲（全部累积，模拟结束写入）
        Private _eventLogBuffer As New List(Of Events.MarketEvent)()

        ' 写入频率：每 N 天刷盘一次
        Private Const FlushInterval As Integer = 30
        Private _dayCount As Integer = 0

        Public Sub New(config As SimulationConfig)
            _config = config
            _rootPath = Path.GetFullPath(config.OutputPath)
        End Sub

        ''' <summary>初始化输出目录结构。</summary>
        Public Sub Initialize()
            ' 清空或创建根目录
            If Directory.Exists(_rootPath) Then
                Directory.Delete(_rootPath, recursive:=True)
            End If
            Directory.CreateDirectory(_rootPath)

            ' 创建子目录
            Directory.CreateDirectory(Path.Combine(_rootPath, OutputSchema.DirPrice))
            Directory.CreateDirectory(Path.Combine(_rootPath, OutputSchema.DirFundamentals))
            Directory.CreateDirectory(Path.Combine(_rootPath, OutputSchema.DirMacro))
            Directory.CreateDirectory(Path.Combine(_rootPath, OutputSchema.DirAlternative, "supply_chain"))
            Directory.CreateDirectory(Path.Combine(_rootPath, OutputSchema.DirAlternative, "insider"))
            Directory.CreateDirectory(Path.Combine(_rootPath, OutputSchema.DirAlternative, "satellite"))
            Directory.CreateDirectory(Path.Combine(_rootPath, OutputSchema.DirDerivatives, "options"))
            Directory.CreateDirectory(Path.Combine(_rootPath, OutputSchema.DirDerivatives, "futures"))
            Directory.CreateDirectory(Path.Combine(_rootPath, OutputSchema.DirDerivatives, "commodities"))
            Directory.CreateDirectory(Path.Combine(_rootPath, OutputSchema.DirDerivatives, "fx"))
            Directory.CreateDirectory(Path.Combine(_rootPath, OutputSchema.DirMicrostructure))
            Directory.CreateDirectory(Path.Combine(_rootPath, OutputSchema.DirTopology))
        End Sub

        ''' <summary>写入量价数据。</summary>
        Public Sub WritePriceData(day As Date, stk As Stock)
            Dim relPath = Path.Combine(OutputSchema.DirPrice, $"{stk.Ticker}_price.csv")
            Dim w = GetWriter(relPath, OutputSchema.PriceColumns)
            w.AddRow({
                day.ToString("yyyy-MM-dd"),
                Math.Round(stk.Open, 4).ToString(),
                Math.Round(stk.High, 4).ToString(),
                Math.Round(stk.Low, 4).ToString(),
                Math.Round(stk.Close, 4).ToString(),
                Math.Round(stk.VWAP, 4).ToString(),
                Math.Round(stk.Volume, 2).ToString(),
                Math.Round(stk.Amount, 2).ToString(),
                Math.Round(stk.TurnoverRate, 5).ToString(),
                Math.Round(stk.MarketCap, 2).ToString(),
                Math.Round(stk.PE, 2).ToString(),
                Math.Round(stk.PB, 3).ToString(),
                Math.Round(stk.PS, 3).ToString(),
                Math.Round(stk.DividendYield, 5).ToString(),
                Math.Round(stk.Beta, 3).ToString(),
                Math.Round(stk.Volatility, 4).ToString(),
                Math.Round(stk.FairValue, 4).ToString(),
                Math.Round(stk.Mispricing, 4).ToString()
            })
        End Sub

        ''' <summary>写入基本面数据。</summary>
        Public Sub WriteFundamentalsData(day As Date, comp As Company)
            Dim relPath = Path.Combine(OutputSchema.DirFundamentals, $"{comp.Ticker}_fundamentals.csv")
            Dim w = GetWriter(relPath, OutputSchema.FundamentalsColumns)
            w.AddRow({
                day.ToString("yyyy-MM-dd"),
                Math.Round(comp.Revenue, 2).ToString(),
                Math.Round(comp.CostOfGoodsSold, 2).ToString(),
                Math.Round(comp.OperatingExpense, 2).ToString(),
                Math.Round(comp.NetProfit, 2).ToString(),
                Math.Round(comp.TotalAssets, 2).ToString(),
                Math.Round(comp.TotalLiabilities, 2).ToString(),
                Math.Round(comp.Equity, 2).ToString(),
                Math.Round(comp.CashAndEquivalents, 2).ToString(),
                Math.Round(comp.OperatingCashFlow, 2).ToString(),
                Math.Round(comp.FreeCashFlow, 2).ToString(),
                Math.Round(comp.SharesOutstanding, 2).ToString(),
                Math.Round(comp.FreeFloatRatio, 4).ToString(),
                Math.Round(comp.InsiderHoldingRatio, 4).ToString(),
                Math.Round(comp.PledgeRatio, 4).ToString(),
                Math.Round(comp.RDLevel, 4).ToString(),
                Math.Round(comp.Competitiveness, 4).ToString(),
                Math.Round(comp.GrossMargin, 4).ToString(),
                Math.Round(comp.NetMargin, 4).ToString(),
                Math.Round(comp.ROE, 4).ToString(),
                Math.Round(comp.ROA, 4).ToString()
            })
        End Sub

        ''' <summary>写入宏观经济数据。</summary>
        Public Sub WriteMacroData(day As Date, c As Country)
            Dim relPath = Path.Combine(OutputSchema.DirMacro, $"{c.CurrencyCode}_macro.csv")
            Dim w = GetWriter(relPath, OutputSchema.MacroColumns)
            w.AddRow({
                day.ToString("yyyy-MM-dd"),
                Math.Round(c.GDP, 2).ToString(),
                Math.Round(c.GDPGrowth, 3).ToString(),
                Math.Round(c.CPI, 3).ToString(),
                Math.Round(c.PPI, 3).ToString(),
                Math.Round(c.PMI, 2).ToString(),
                Math.Round(c.NonFarmPayroll, 2).ToString(),
                Math.Round(c.InterestRate, 3).ToString(),
                Math.Round(c.InterbankRate, 3).ToString(),
                Math.Round(c.TreasuryYield2Y, 3).ToString(),
                Math.Round(c.TreasuryYield10Y, 3).ToString(),
                Math.Round(c.M1, 2).ToString(),
                Math.Round(c.M2, 2).ToString(),
                Math.Round(c.ExchangeRate, 6).ToString(),
                Math.Round(c.PoliticalStability, 3).ToString(),
                Math.Round(c.MarginBalance, 2).ToString(),
                Math.Round(c.NorthFlow, 2).ToString()
            })
        End Sub

        ''' <summary>写入供应链数据。</summary>
        Public Sub WriteSupplyChainData(rec As Simulation.SupplyChainRecord)
            Dim relPath = Path.Combine(OutputSchema.DirAlternative, "supply_chain", $"{rec.Ticker}_supplychain.csv")
            Dim w = GetWriter(relPath, OutputSchema.SupplyChainColumns)
            w.AddRow({
                rec.Date.ToString("yyyy-MM-dd"),
                rec.OrderVolume.ToString(),
                rec.SupplierOrderValue.ToString(),
                rec.InventoryTurnover.ToString(),
                rec.CustomerDemandIndex.ToString()
            })
        End Sub

        ''' <summary>写入高管/质押数据。</summary>
        Public Sub WriteInsiderData(rec As Simulation.InsiderRecord)
            Dim relPath = Path.Combine(OutputSchema.DirAlternative, "insider", $"{rec.Ticker}_insider.csv")
            Dim w = GetWriter(relPath, OutputSchema.InsiderColumns)
            w.AddRow({
                rec.Date.ToString("yyyy-MM-dd"),
                rec.Action,
                rec.BuyAmount.ToString(),
                rec.SellAmount.ToString(),
                rec.PledgeRatio.ToString(),
                If(rec.UnlockDate.HasValue, rec.UnlockDate.Value.ToString("yyyy-MM-dd"), ""),
                rec.UnlockShares.ToString()
            })
        End Sub

        ''' <summary>写入卫星代理数据。</summary>
        Public Sub WriteSatelliteData(rec As Simulation.SatelliteRecord)
            Dim relPath = Path.Combine(OutputSchema.DirAlternative, "satellite", $"{rec.Ticker}_satellite.csv")
            Dim w = GetWriter(relPath, OutputSchema.SatelliteColumns)
            w.AddRow({
                rec.Date.ToString("yyyy-MM-dd"),
                rec.VehicleCount.ToString(),
                rec.RevenueProxy.ToString()
            })
        End Sub

        ''' <summary>写入期权数据。</summary>
        Public Sub WriteOptionsData(rec As Simulation.OptionsRecord)
            Dim relPath = Path.Combine(OutputSchema.DirDerivatives, "options", $"{rec.Underlying}_options.csv")
            Dim w = GetWriter(relPath, OutputSchema.OptionsColumns)
            w.AddRow({
                rec.Date.ToString("yyyy-MM-dd"),
                rec.ImpliedVolatility.ToString(),
                rec.PutCallRatio.ToString(),
                rec.OptionVolume.ToString()
            })
        End Sub

        ''' <summary>写入期货数据。</summary>
        Public Sub WriteFuturesData(rec As Simulation.FuturesRecord)
            Dim relPath = Path.Combine(OutputSchema.DirDerivatives, "futures", $"{rec.Contract}_futures.csv")
            Dim w = GetWriter(relPath, OutputSchema.FuturesColumns)
            w.AddRow({
                rec.Date.ToString("yyyy-MM-dd"),
                rec.FuturesPrice.ToString(),
                rec.SpotPrice.ToString(),
                rec.Basis.ToString(),
                rec.BasisPct.ToString()
            })
        End Sub

        ''' <summary>写入大宗商品数据。</summary>
        Public Sub WriteCommodityData(rec As Simulation.CommodityRecord)
            Dim relPath = Path.Combine(OutputSchema.DirDerivatives, "commodities", $"{rec.Commodity}_commodity.csv")
            Dim w = GetWriter(relPath, OutputSchema.CommodityColumns)
            w.AddRow({
                rec.Date.ToString("yyyy-MM-dd"),
                rec.Price.ToString(),
                rec.Volume.ToString(),
                rec.ChangePct.ToString()
            })
        End Sub

        ''' <summary>写入汇率数据。</summary>
        Public Sub WriteFxData(rec As Simulation.FxRecord)
            Dim relPath = Path.Combine(OutputSchema.DirDerivatives, "fx", $"{rec.Pair}_fx.csv")
            Dim w = GetWriter(relPath, OutputSchema.FxColumns)
            w.AddRow({
                rec.Date.ToString("yyyy-MM-dd"),
                rec.Rate.ToString(),
                rec.ChangePct.ToString()
            })
        End Sub

        ''' <summary>写入微观结构数据。</summary>
        Public Sub WriteMicrostructureData(rec As Simulation.MicrostructureRecord)
            Dim relPath = Path.Combine(OutputSchema.DirMicrostructure, $"{rec.Ticker}_microstructure.csv")
            Dim w = GetWriter(relPath, OutputSchema.MicrostructureColumns)
            w.AddRow({
                rec.Date.ToString("yyyy-MM-dd"),
                rec.OrderImbalance.ToString(),
                rec.LargeOrderCount.ToString(),
                rec.LargeOrderAmount.ToString(),
                rec.BuyRatio.ToString(),
                rec.SellRatio.ToString(),
                rec.Slippage.ToString()
            })
        End Sub

        ''' <summary>累积事件到事件流缓冲。</summary>
        Public Sub BufferEvents(events As List(Of Events.MarketEvent))
            _eventLogBuffer.AddRange(events)
        End Sub

        ''' <summary>定期刷盘（每步调用）。</summary>
        Public Sub BufferStep()
            _dayCount += 1
            If _dayCount Mod FlushInterval = 0 Then
                FlushCsv()
            End If
        End Sub

        ''' <summary>刷盘所有 CSV 缓冲。</summary>
        Public Sub FlushCsv()
            For Each kv In _writers
                kv.Value.Flush()
            Next
        End Sub

        ''' <summary>模拟结束后写入所有 JSON 拓扑和事件流文件。</summary>
        Public Sub WriteTopologyFiles(
            countries As List(Of Country),
            companies As List(Of Company),
            stocks As List(Of Stock),
            chains As List(Of IndustryChain),
            investors As List(Of Investor),
            relationships As List(Of Relationship)
        )
            ' 1. 世界拓扑
            Dim topology = New With {
                .countries = countries.Select(Function(c) New With {
                    .id = c.CountryId, .name = c.Name, .currency = c.CurrencyCode,
                    .exchangeRate = c.ExchangeRate, .interestRate = c.InterestRate,
                    .gdpGrowth = c.GDPGrowth, .cpi = c.CPI, .pmi = c.PMI,
                    .politicalStability = c.PoliticalStability,
                    .industryChainIds = c.IndustryChainIds,
                    .companyIds = c.CompanyIds
                }).ToArray(),
                .companies = companies.Select(Function(c) New With {
                    .id = c.CompanyId, .ticker = c.Ticker, .name = c.Name,
                    .countryId = c.CountryId, .industryChainId = c.IndustryChainId,
                    .category = c.ProductCategory.ToString(), .layer = c.ChainLayer.ToString(),
                    .competitiveness = c.Competitiveness, .rdLevel = c.RDLevel
                }).ToArray(),
                .stocks = stocks.Select(Function(s) New With {
                    .id = s.StockId, .ticker = s.Ticker, .companyId = s.CompanyId,
                    .countryId = s.CountryId, .beta = s.Beta, .volatility = s.Volatility
                }).ToArray(),
                .investors = investors.Select(Function(i) New With {
                    .id = i.InvestorId, .name = i.Name, .type = i.Type.ToString(),
                    .capitalSize = i.CapitalSize, .countryId = i.CountryId,
                    .holdings = i.Holdings
                }).ToArray()
            }
            JsonWriter.WriteToFile(Path.Combine(_rootPath, OutputSchema.DirTopology, OutputSchema.FileWorldTopology), topology)

            ' 2. 关系图
            Dim relData = relationships.Select(Function(r) New With {
                .id = r.RelationId,
                .sourceType = r.SourceType.ToString(), .sourceId = r.SourceId,
                .targetType = r.TargetType.ToString(), .targetId = r.TargetId,
                .type = r.Type.ToString(), .weight = r.Weight,
                .description = r.Description
            }).ToArray()
            JsonWriter.WriteToFile(Path.Combine(_rootPath, OutputSchema.DirTopology, OutputSchema.FileRelationships), relData)

            ' 3. 产业链结构
            Dim chainData = chains.Select(Function(ch) New With {
                .id = ch.ChainId, .name = ch.Name, .countryId = ch.CountryId,
                .category = ch.Category.ToString(), .layer = ch.Layer.ToString(),
                .productionCapacity = ch.ProductionCapacity,
                .utilizationRate = ch.UtilizationRate,
                .productPrice = ch.ProductPrice,
                .upstreamChainIds = ch.UpstreamChainIds,
                .downstreamChainIds = ch.DownstreamChainIds,
                .companyIds = ch.CompanyIds
            }).ToArray()
            JsonWriter.WriteToFile(Path.Combine(_rootPath, OutputSchema.DirTopology, OutputSchema.FileIndustryChains), chainData)

            ' 4. 事件流
            Dim eventData = _eventLogBuffer.Select(Function(e) New With {
                .eventId = e.EventId,
                .timestamp = e.Timestamp.ToString("yyyy-MM-dd"),
                .eventType = e.EventType.ToString(),
                .sourceEntityType = e.SourceEntityType.ToString(),
                .sourceEntityId = e.SourceEntityId,
                .description = e.Description,
                .initialImpact = Math.Round(e.InitialImpact, 4),
                .affectedEntities = e.AffectedEntities.Select(Function(kv) New With {.companyId = kv.Key, .impact = Math.Round(kv.Value, 4)}).ToArray(),
                .propagationPath = e.PropagationPath.Select(Function(p) New With {.entityType = p.Item1.ToString(), .entityId = p.Item2, .impact = Math.Round(p.Item3, 4)}).ToArray()
            }).ToArray()
            JsonWriter.WriteToFile(Path.Combine(_rootPath, OutputSchema.DirTopology, OutputSchema.FileEventLog), eventData)
        End Sub

        ''' <summary>获取或创建 CSV 写入器。</summary>
        Private Function GetWriter(relPath As String, columns As String()) As CsvWriter
            If Not _writers.ContainsKey(relPath) Then
                Dim fullPath = Path.Combine(_rootPath, relPath)
                Dim w As New CsvWriter(fullPath, columns)
                w.EnsureHeader()
                _writers(relPath) = w
            End If
            Return _writers(relPath)
        End Function

        ''' <summary>输出统计摘要。</summary>
        Public Function GetStats() As String
            Dim sb As New StringBuilder()
            sb.AppendLine($"输出目录: {_rootPath}")
            sb.AppendLine($"CSV 文件数: {_writers.Count}")
            sb.AppendLine($"事件流记录数: {_eventLogBuffer.Count}")
            Return sb.ToString()
        End Function

    End Class

End Namespace
