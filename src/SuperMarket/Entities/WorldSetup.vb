Option Strict On
Option Explicit On

Imports SuperMarket.Core

Namespace SuperMarket.Entities

    ''' <summary>
    ''' 世界设定生成器：根据配置生成国家、公司、产业链、产品、投资者及其关系网络。
    ''' 全过程使用种子化随机，保证可复现。
    ''' </summary>
    Public Class WorldSetup

        Private ReadOnly _rng As SeededRandom
        Private ReadOnly _config As SimulationConfig

        ' 预设国家名称与货币（取前 N 个，N=CountryCount）
        Private Shared ReadOnly CountryTemplates As (Name As String, Currency As String, Symbol As String, Stability As Double, StartRate As Double)() = {
            ("United States", "USD", "$", 0.85, 1.0),
            ("China", "CNY", "¥", 0.8, 0.14),
            ("Japan", "JPY", "¥", 0.75, 0.0067),
            ("Germany", "EUR", "€", 0.82, 1.08),
            ("United Kingdom", "GBP", "£", 0.78, 1.25),
            ("France", "EUR", "€", 0.76, 1.08),
            ("South Korea", "KRW", "₩", 0.68, 0.00075),
            ("India", "INR", "₹", 0.6, 0.012),
            ("Canada", "CAD", "C$", 0.83, 0.74),
            ("Australia", "AUD", "A$", 0.84, 0.66),
            ("Brazil", "BRL", "R$", 0.55, 0.2),
            ("Russia", "RUB", "₽", 0.45, 0.011),
            ("Saudi Arabia", "SAR", "﷼", 0.6, 0.27),
            ("Singapore", "SGD", "S$", 0.9, 0.74),
            ("Mexico", "MXN", "$", 0.58, 0.059),
            ("South Africa", "ZAR", "R", 0.52, 0.054),
            ("Italy", "EUR", "€", 0.7, 1.08),
            ("Spain", "EUR", "€", 0.72, 1.08),
            ("Netherlands", "EUR", "€", 0.86, 1.08),
            ("Switzerland", "CHF", "Fr", 0.92, 1.12)
        }

        Private Shared ReadOnly ChainTemplates As (Category As ProductCategory, Layer As ChainLayer, Name As String)() = {
            (ProductCategory.Energy, ChainLayer.Upstream, "原油开采"),
            (ProductCategory.Energy, ChainLayer.Midstream, "炼化加工"),
            (ProductCategory.Energy, ChainLayer.Downstream, "能源分销"),
            (ProductCategory.IndustrialRawMaterial, ChainLayer.Upstream, "金属矿采选"),
            (ProductCategory.IndustrialRawMaterial, ChainLayer.Midstream, "钢铁冶炼"),
            (ProductCategory.IndustrialRawMaterial, ChainLayer.Downstream, "建材制造"),
            (ProductCategory.Agricultural, ChainLayer.Upstream, "农作物种植"),
            (ProductCategory.Agricultural, ChainLayer.Midstream, "食品加工"),
            (ProductCategory.Agricultural, ChainLayer.Downstream, "食品零售"),
            (ProductCategory.InformationTechnology, ChainLayer.Upstream, "半导体晶圆"),
            (ProductCategory.InformationTechnology, ChainLayer.Midstream, "芯片制造"),
            (ProductCategory.InformationTechnology, ChainLayer.Downstream, "消费电子组装"),
            (ProductCategory.HighTech, ChainLayer.Upstream, "生物医药研发"),
            (ProductCategory.HighTech, ChainLayer.Midstream, "医疗器械制造"),
            (ProductCategory.HighTech, ChainLayer.Downstream, "医疗服务"),
            (ProductCategory.ConsumerGoods, ChainLayer.Upstream, "纺织原料"),
            (ProductCategory.ConsumerGoods, ChainLayer.Midstream, "服装制造"),
            (ProductCategory.ConsumerGoods, ChainLayer.Downstream, "品牌零售")
        }

        ' 公司类型模板（每个模板对应一个产业链环节，便于公司绑定产业链）
        Private Shared ReadOnly CompanyTemplates As (Category As ProductCategory, Layer As ChainLayer, Industry As String)() = {
            (ProductCategory.Energy, ChainLayer.Upstream, "EnergyExtraction"),
            (ProductCategory.Energy, ChainLayer.Midstream, "EnergyRefining"),
            (ProductCategory.IndustrialRawMaterial, ChainLayer.Upstream, "Mining"),
            (ProductCategory.IndustrialRawMaterial, ChainLayer.Midstream, "SteelMaking"),
            (ProductCategory.Agricultural, ChainLayer.Upstream, "Farming"),
            (ProductCategory.Agricultural, ChainLayer.Midstream, "FoodProcessing"),
            (ProductCategory.InformationTechnology, ChainLayer.Upstream, "SemiconductorDesign"),
            (ProductCategory.InformationTechnology, ChainLayer.Midstream, "ChipManufacturing"),
            (ProductCategory.InformationTechnology, ChainLayer.Downstream, "ConsumerElectronics"),
            (ProductCategory.HighTech, ChainLayer.Upstream, "BiotechRnD"),
            (ProductCategory.HighTech, ChainLayer.Midstream, "MedicalDevices"),
            (ProductCategory.HighTech, ChainLayer.Downstream, "HealthcareServices"),
            (ProductCategory.ConsumerGoods, ChainLayer.Midstream, "ApparelManufacturing"),
            (ProductCategory.ConsumerGoods, ChainLayer.Downstream, "BrandRetail")
        }

        ' 投资者名字模板
        Private Shared ReadOnly InvestorNames As String() = {
            "BlackStone Capital", "Vanguard Trust", "Pioneer Fund", "Apex Whale",
            "GoldenBridge Holdings", "Quantum Edge LLC", "Pacifica Asset", "Meridian Partners",
            "Summit Sovereign", "Helix Capital", "Retail Crowd Alpha", "Retail Crowd Beta"
        }

        Public Sub New(rng As SeededRandom, config As SimulationConfig)
            _rng = rng
            _config = config
        End Sub

        ''' <summary>生成整个世界：国家、产业链、产品、公司、股票、投资者、关系。</summary>
        Public Sub Generate(
            ByRef countries As List(Of Country),
            ByRef companies As List(Of Company),
            ByRef stocks As List(Of Stock),
            ByRef chains As List(Of IndustryChain),
            ByRef products As List(Of Product),
            ByRef investors As List(Of Investor),
            ByRef relationships As List(Of Relationship)
        )
            countries = New List(Of Country)()
            companies = New List(Of Company)()
            stocks = New List(Of Stock)()
            chains = New List(Of IndustryChain)()
            products = New List(Of Product)()
            investors = New List(Of Investor)()
            relationships = New List(Of Relationship)()

            GenerateCountries(countries)
            GenerateChainsAndProducts(countries, chains, products)
            GenerateCompanies(countries, chains, companies, stocks)
            GenerateInvestors(countries, investors)
            GenerateRelationships(countries, companies, chains, products, investors, relationships)
        End Sub

        ' ===== 国家生成 =====
        Private Sub GenerateCountries(countries As List(Of Country))
            Dim count As Integer = Math.Min(_config.CountryCount, CountryTemplates.Length)
            For i = 0 To count - 1
                Dim tpl = CountryTemplates(i)
                Dim c As New Country() With {
                    .CountryId = i,
                    .Name = tpl.Name,
                    .CurrencyCode = tpl.Currency,
                    .CurrencySymbol = tpl.Symbol,
                    .ExchangeRate = tpl.StartRate,
                    .PreviousExchangeRate = tpl.StartRate,
                    .PoliticalStability = tpl.Stability,
                    .InterestRate = Math.Round(_rng.NextGaussian(2.5, 1.5), 2),
                    .InterbankRate = Math.Round(_rng.NextGaussian(2.5, 1.5), 2),
                    .GDP = _rng.NextDouble(500000, 5000000),
                    .GDPGrowth = Math.Round(_rng.NextGaussian(2.5, 1.5), 2),
                    .CPI = Math.Round(_rng.NextGaussian(2.0, 1.2), 2),
                    .PPI = Math.Round(_rng.NextGaussian(1.5, 1.5), 2),
                    .PMI = Math.Round(_rng.NextGaussian(50.0, 3.0), 2),
                    .NonFarmPayroll = _rng.NextDouble(5000, 20000),
                    .M1 = _rng.NextDouble(200000, 2000000),
                    .M2 = _rng.NextDouble(500000, 4000000),
                    .TreasuryYield10Y = Math.Round(_rng.NextGaussian(2.5, 1.0), 2),
                    .TreasuryYield2Y = Math.Round(_rng.NextGaussian(1.5, 1.0), 2),
                    .MarginBalance = _rng.NextDouble(0, 500000),
                    .NorthFlow = _rng.NextGaussian(0, 5000)
                }
                countries.Add(c)
            Next

            ' 为每对国家初始化关税表（按产品类别）
            For Each c In countries
                For Each partner In countries
                    If partner.CountryId = c.CountryId Then Continue For
                    Dim tariffTable As New Dictionary(Of ProductCategory, Double)()
                    For Each cat As ProductCategory In [Enum].GetValues(GetType(ProductCategory))
                        tariffTable(cat) = Math.Round(_rng.NextDouble(0.0, 0.25), 3)
                    Next
                    c.Tariffs(partner.CountryId) = tariffTable
                Next
            Next
        End Sub

        ' ===== 产业链与产品生成 =====
        Private Sub GenerateChainsAndProducts(
            countries As List(Of Country),
            chains As List(Of IndustryChain),
            products As List(Of Product)
        )
            Dim chainId As Integer = 0
            Dim prodId As Integer = 0

            ' 为每个国家分配若干产业链（基于国家特征）
            For Each c In countries
                ' 根据国家 ID 和随机决定该国擅长的产业链类别（2-5 条产业链）
                Dim numChains As Integer = _rng.[Next](2, 5)
                Dim chosenIndices As New List(Of Integer)
                For i = 0 To ChainTemplates.Length - 1
                    chosenIndices.Add(i)
                Next
                _rng.Shuffle(chosenIndices)
                Dim picked = chosenIndices.Take(numChains).ToList()

                ' 按 Category 分组：同一 Category 的上中下游应构成一条完整产业链
                Dim byCategory As New Dictionary(Of ProductCategory, List(Of Integer))()
                For Each idx In picked
                    Dim tpl = ChainTemplates(idx)
                    If Not byCategory.ContainsKey(tpl.Category) Then
                        byCategory(tpl.Category) = New List(Of Integer)()
                    End If
                    ' 创建该环节产业链
                    Dim chain As New IndustryChain() With {
                        .ChainId = chainId,
                        .Name = $"{c.Name}-{tpl.Name}",
                        .CountryId = c.CountryId,
                        .Category = tpl.Category,
                        .Layer = tpl.Layer,
                        .ProductionCapacity = _rng.NextDouble(500, 2000),
                        .UtilizationRate = _rng.NextDouble(0.5, 0.9),
                        .Inventory = _rng.NextDouble(50, 500),
                        .ProductPrice = _rng.NextDouble(50, 200),
                        .BasePrice = 100.0,
                        .Demand = _rng.NextDouble(50, 200),
                        .Supply = _rng.NextDouble(50, 200)
                    }
                    chain.BasePrice = chain.ProductPrice
                    chains.Add(chain)
                    c.IndustryChainIds.Add(chainId)
                    byCategory(tpl.Category).Add(chainId)
                    chainId += 1
                Next

                ' 为该国的每条产业链建立上下游连接
                For Each kv In byCategory
                    Dim ids = kv.Value
                    If ids.Count <= 1 Then Continue For
                    ' 按 Layer 排序
                    Dim ordered = ids.OrderBy(Function(id) chains(id).Layer).ToList()
                    For i = 0 To ordered.Count - 2
                        chains(ordered(i)).DownstreamChainIds.Add(ordered(i + 1))
                        chains(ordered(i + 1)).UpstreamChainIds.Add(ordered(i))
                    Next
                Next

                ' 为每个产业链环节创建对应产品
                For Each chainId_local In c.IndustryChainIds
                    Dim chain = chains(chainId_local)
                    Dim prod As New Product() With {
                        .ProductId = prodId,
                        .Name = $"{chain.Name}-Product",
                        .Category = chain.Category,
                        .BasePrice = chain.ProductPrice,
                        .CurrentPrice = chain.ProductPrice,
                        .DemandElasticity = Math.Round(_rng.NextDouble(0.2, 0.8), 2),
                        .SupplyElasticity = Math.Round(_rng.NextDouble(0.1, 0.5), 2),
                        .GlobalDemand = _rng.NextDouble(5000, 50000),
                        .GlobalSupply = _rng.NextDouble(5000, 50000)
                    }
                    prod.ProducerCountryIds.Add(c.CountryId)
                    products.Add(prod)
                    prodId += 1
                Next
            Next
        End Sub

        ' ===== 公司与股票生成 =====
        Private Sub GenerateCompanies(
            countries As List(Of Country),
            chains As List(Of IndustryChain),
            companies As List(Of Company),
            stocks As List(Of Stock)
        )
            Dim companyId As Integer = 0
            Dim stockId As Integer = 0

            For i = 0 To _config.CompanyCount - 1
                ' 选国家
                Dim c = _rng.Choice(countries)
                ' 选产业链模板
                Dim tpl = CompanyTemplates(i Mod CompanyTemplates.Length)
                ' 找该国该类别的产业链
                Dim matchingChains = chains.Where(Function(ch) ch.CountryId = c.CountryId AndAlso ch.Category = tpl.Category AndAlso ch.Layer = tpl.Layer).ToList()
                If matchingChains.Count = 0 Then
                    ' 退而求其次：同国家同类别
                    matchingChains = chains.Where(Function(ch) ch.CountryId = c.CountryId AndAlso ch.Category = tpl.Category).ToList()
                End If
                If matchingChains.Count = 0 Then
                    ' 再退：同国家任意
                    matchingChains = chains.Where(Function(ch) ch.CountryId = c.CountryId).ToList()
                End If
                If matchingChains.Count = 0 Then Continue For

                Dim chain = _rng.Choice(matchingChains)

                ' 生成 ticker
                Dim ticker As String = GenerateTicker(c, tpl, companyId)
                Dim name As String = $"{c.Name} {tpl.Industry} {companyId + 1:D3}"

                Dim comp As New Company() With {
                    .CompanyId = companyId,
                    .Name = name,
                    .Ticker = ticker,
                    .CountryId = c.CountryId,
                    .IndustryChainId = chain.ChainId,
                    .ProductCategory = chain.Category,
                    .ChainLayer = chain.Layer,
                    .Revenue = _rng.NextDouble(500, 5000),
                    .CostOfGoodsSold = 0,
                    .OperatingExpense = _rng.NextDouble(30, 300),
                    .NetProfit = 0,
                    .TotalAssets = _rng.NextDouble(2000, 20000),
                    .TotalLiabilities = 0,
                    .Equity = 0,
                    .CashAndEquivalents = _rng.NextDouble(100, 2000),
                    .OperatingCashFlow = _rng.NextDouble(50, 800),
                    .FreeCashFlow = _rng.NextDouble(20, 400),
                    .SharesOutstanding = _rng.NextDouble(50, 500),
                    .FreeFloatRatio = _rng.NextDouble(0.3, 0.8),
                    .InsiderHoldingRatio = _rng.NextDouble(0.1, 0.4),
                    .PledgeRatio = _rng.NextDouble(0, 0.3),
                    .RDLevel = _rng.NextDouble(0.05, 0.25),
                    .Competitiveness = _rng.NextDouble(0.3, 0.9),
                    .BrandValue = _rng.NextDouble(0, 1000)
                }
                comp.CostOfGoodsSold = comp.Revenue * _rng.NextDouble(0.5, 0.8)
                comp.TotalLiabilities = comp.TotalAssets * _rng.NextDouble(0.2, 0.6)
                comp.Equity = comp.TotalAssets - comp.TotalLiabilities
                comp.NetProfit = comp.Revenue - comp.CostOfGoodsSold - comp.OperatingExpense
                ' 设置限售解禁（随机未来 30-365 天）
                If _rng.NextBoolean(0.3) Then
                    comp.NextUnlockDate = _config.StartDate.AddDays(_rng.[Next](30, 365))
                    comp.NextUnlockShares = comp.SharesOutstanding * _rng.NextDouble(0.05, 0.2)
                End If

                companies.Add(comp)
                c.CompanyIds.Add(comp.CompanyId)
                chain.CompanyIds.Add(comp.CompanyId)

                ' 生成股票
                Dim eps As Double = If(comp.SharesOutstanding > 0, comp.NetProfit / comp.SharesOutstanding, 1.0)
                Dim initPE As Double = _rng.NextDouble(10, 40)
                Dim initPrice As Double = Math.Max(1.0, eps * initPE)
                Dim stk As New Stock() With {
                    .StockId = stockId,
                    .Ticker = ticker,
                    .CompanyId = comp.CompanyId,
                    .CountryId = comp.CountryId,
                    .PreviousClose = initPrice,
                    .[Open] = initPrice,
                    .High = initPrice,
                    .Low = initPrice,
                    .Close = initPrice,
                    .VWAP = initPrice,
                    .Volume = _rng.NextDouble(100, 5000),
                    .Amount = 0,
                    .TurnoverRate = _rng.NextDouble(0.005, 0.05),
                    .MarketCap = initPrice * comp.SharesOutstanding,
                    .PE = initPE,
                    .PB = _rng.NextDouble(1, 5),
                    .PS = _rng.NextDouble(0.5, 3),
                    .DividendYield = _rng.NextDouble(0, 0.05),
                    .Beta = Math.Round(_rng.NextGaussian(1.0, 0.3), 2),
                    .Volatility = Math.Round(_rng.NextDouble(0.15, 0.45), 3),
                    .DailyVolatility = 0,
                    .FairValue = initPrice
                }
                stk.DailyVolatility = stk.Volatility / Math.Sqrt(_config.TradingDaysPerYear)
                stk.Amount = stk.Volume * initPrice
                stocks.Add(stk)

                companyId += 1
                stockId += 1
            Next
        End Sub

        Private Function GenerateTicker(c As Country, tpl As (Category As ProductCategory, Layer As ChainLayer, Industry As String), id As Integer) As String
            Dim prefix As String = c.CurrencyCode.Substring(0, Math.Min(2, c.CurrencyCode.Length))
            Dim catCode As String = tpl.Category.ToString().Substring(0, 2)
            Return $"{prefix}{catCode}{id + 1:D3}"
        End Function

        ' ===== 投资者生成 =====
        Private Sub GenerateInvestors(countries As List(Of Country), investors As List(Of Investor))
            For i = 0 To InvestorNames.Length - 1
                Dim type As InvestorType
                Dim risk As Double, chase As Double, shortT As Double, capital As Double
                If i < 4 Then
                    type = InvestorType.Whale
                    risk = _rng.NextDouble(0.4, 0.8)
                    chase = _rng.NextDouble(0.2, 0.5)
                    shortT = _rng.NextDouble(0.3, 0.7)
                    capital = _rng.NextDouble(5000000, 50000000)
                ElseIf i < 8 Then
                    type = InvestorType.Institutional
                    risk = _rng.NextDouble(0.3, 0.6)
                    chase = _rng.NextDouble(0.2, 0.4)
                    shortT = _rng.NextDouble(0.1, 0.3)
                    capital = _rng.NextDouble(1000000, 5000000)
                Else
                    type = InvestorType.Retail
                    risk = _rng.NextDouble(0.2, 0.5)
                    chase = _rng.NextDouble(0.4, 0.8)
                    shortT = _rng.NextDouble(0.05, 0.2)
                    capital = _rng.NextDouble(10000, 200000)
                End If
                Dim inv As New Investor() With {
                    .InvestorId = i,
                    .Name = InvestorNames(i),
                    .Type = type,
                    .RiskAppetite = risk,
                    .HoldingPeriod = _rng.[Next](5, 180),
                    .ChaseTendency = chase,
                    .ShortTendency = shortT,
                    .CapitalSize = capital,
                    .CountryId = _rng.Choice(countries).CountryId
                }
                investors.Add(inv)
            Next
        End Sub

        ' ===== 关系网络生成 =====
        Private Sub GenerateRelationships(
            countries As List(Of Country),
            companies As List(Of Company),
            chains As List(Of IndustryChain),
            products As List(Of Product),
            investors As List(Of Investor),
            relationships As List(Of Relationship)
        )
            Dim relId As Integer = 0

            ' 1. 公司归属国家（Ownership）
            For Each comp In companies
                relationships.Add(New Relationship() With {
                    .RelationId = relId,
                    .SourceType = EntityType.Company,
                    .SourceId = comp.CompanyId,
                    .TargetType = EntityType.Country,
                    .TargetId = comp.CountryId,
                    .Type = RelationType.Ownership,
                    .Weight = 1.0,
                    .Description = $"{comp.Name} 总部位于 {countries(comp.CountryId).Name}"
                })
                relId += 1
            Next

            ' 2. 产业链上下游关系（SupplyChain）：已在 IndustryChain 中建立 UpstreamChainIds/DownstreamChainIds
            For Each ch In chains
                For Each downId In ch.DownstreamChainIds
                    relationships.Add(New Relationship() With {
                        .RelationId = relId,
                        .SourceType = EntityType.IndustryChain,
                        .SourceId = ch.ChainId,
                        .TargetType = EntityType.IndustryChain,
                        .TargetId = downId,
                        .Type = RelationType.SupplyChain,
                        .Weight = _rng.NextDouble(0.5, 1.0),
                        .Description = $"{ch.Name} → {chains(downId).Name} 供应链"
                    })
                    relId += 1
                Next
            Next

            ' 3. 公司与产业链的归属关系
            For Each comp In companies
                relationships.Add(New Relationship() With {
                    .RelationId = relId,
                    .SourceType = EntityType.Company,
                    .SourceId = comp.CompanyId,
                    .TargetType = EntityType.IndustryChain,
                    .TargetId = comp.IndustryChainId,
                    .Type = RelationType.SupplyChain,
                    .Weight = 0.8,
                    .Description = $"{comp.Name} 参与 {chains(comp.IndustryChainId).Name}"
                })
                relId += 1
            Next

            ' 4. 国家间贸易关系（Trade）：基于比较优势（不同产品类别）
            For i = 0 To countries.Count - 1
                For j = 0 To countries.Count - 1
                    If i = j Then Continue For
                    ' 60% 概率存在贸易关系
                    If _rng.NextBoolean(0.6) Then
                        relationships.Add(New Relationship() With {
                            .RelationId = relId,
                            .SourceType = EntityType.Country,
                            .SourceId = countries(i).CountryId,
                            .TargetType = EntityType.Country,
                            .TargetId = countries(j).CountryId,
                            .Type = RelationType.Trade,
                            .Weight = Math.Round(_rng.NextDouble(0.2, 0.9), 2),
                            .Description = $"{countries(i).Name} → {countries(j).Name} 贸易"
                        })
                        relId += 1
                    End If
                Next
            Next

            ' 5. 国家间国债持有关系（TreasuryHolding）：大国持小国国债
            For i = 0 To countries.Count - 1
                For j = 0 To countries.Count - 1
                    If i = j Then Continue For
                    If _rng.NextBoolean(0.25) Then
                        Dim amt As Double = _rng.NextDouble(1000, 50000)
                        countries(i).TreasuryHoldings(countries(j).CountryId) = amt
                        relationships.Add(New Relationship() With {
                            .RelationId = relId,
                            .SourceType = EntityType.Country,
                            .SourceId = countries(i).CountryId,
                            .TargetType = EntityType.Country,
                            .TargetId = countries(j).CountryId,
                            .Type = RelationType.TreasuryHolding,
                            .Weight = Math.Round(Math.Min(1.0, amt / 50000), 2),
                            .Description = $"{countries(i).Name} 持有 {countries(j).Name} 国债 {amt:F0}"
                        })
                        relId += 1
                    End If
                Next
            Next

            ' 6. 公司间竞争关系（Competition）：同类别同层的公司
            Dim byCatLayer = companies.GroupBy(Function(c) (c.ProductCategory, c.ChainLayer)).ToList()
            For Each grp In byCatLayer
                Dim list = grp.ToList()
                If list.Count < 2 Then Continue For
                ' 为每对竞争对手建立关系（限制数量避免过多）
                For i = 0 To list.Count - 2
                    For j = i + 1 To Math.Min(i + 3, list.Count - 1)
                        If _rng.NextBoolean(0.5) Then
                            relationships.Add(New Relationship() With {
                                .RelationId = relId,
                                .SourceType = EntityType.Company,
                                .SourceId = list(i).CompanyId,
                                .TargetType = EntityType.Company,
                                .TargetId = list(j).CompanyId,
                                .Type = RelationType.Competition,
                                .Weight = Math.Round(_rng.NextDouble(0.3, 0.8), 2),
                                .Description = $"{list(i).Ticker} ↔ {list(j).Ticker} 竞争"
                            })
                            relId += 1
                        End If
                    Next
                Next
            Next

            ' 7. 投资者持仓关系（Portfolio）：每个投资者随机持有 5-20 只股票
            For Each inv In investors
                Dim numHoldings As Integer = _rng.[Next](5, 20)
                Dim pool = companies.ToList()
                _rng.Shuffle(pool)
                For k = 0 To Math.Min(numHoldings, pool.Count) - 1
                    Dim comp = pool(k)
                    Dim amt As Double = inv.CapitalSize * _rng.NextDouble(0.02, 0.15)
                    inv.Holdings(comp.CompanyId) = amt
                    relationships.Add(New Relationship() With {
                        .RelationId = relId,
                        .SourceType = EntityType.Investor,
                        .SourceId = inv.InvestorId,
                        .TargetType = EntityType.Company,
                        .TargetId = comp.CompanyId,
                        .Type = RelationType.Portfolio,
                        .Weight = Math.Round(Math.Min(1.0, amt / inv.CapitalSize * 5), 2),
                        .Description = $"{inv.Name} 持有 {comp.Ticker} {amt:F0}"
                    })
                    relId += 1
                Next
            Next

        End Sub

    End Class

End Namespace
