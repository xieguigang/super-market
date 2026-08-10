Option Strict On
Option Explicit On

Imports System.IO
Imports SuperMarket.Core
Imports SuperMarket.Entities
Imports SuperMarket.Events
Imports SuperMarket.Simulation
Imports SuperMarket.Output

Namespace SuperMarket

    ''' <summary>
    ''' 市场模拟器：主循环编排所有引擎，按时间步顺序执行：
    ''' 宏观→产业链→公司经营→事件生成→事件传播→股价定价→衍生品→微观结构→另类数据→输出。
    ''' </summary>
    Public Class MarketSimulator

        Private ReadOnly _config As SimulationConfig
        Private ReadOnly _rng As SeededRandom

        ' 世界实体
        Private _countries As List(Of Country)
        Private _companies As List(Of Company)
        Private _stocks As List(Of Stock)
        Private _chains As List(Of IndustryChain)
        Private _products As List(Of Product)
        Private _investors As List(Of Investor)
        Private _relationships As List(Of Relationship)

        ' 事件系统
        Private _network As ButterflyEffectNetwork
        Private _eventGenerator As EventGenerator
        Private _eventPropagator As EventPropagator

        ' 核心引擎
        Private _macroEngine As MacroEngine
        Private _chainEngine As IndustryChainEngine
        Private _companyEngine As CompanyEngine
        Private _stockEngine As StockPriceEngine

        ' 扩展引擎
        Private _derivativesEngine As DerivativesEngine
        Private _microstructureEngine As MicrostructureEngine
        Private _alternativeDataEngine As AlternativeDataEngine

        ' 输出
        Private _outputManager As OutputManager

        ' 索引（用于事件传播）
        Private _companiesByChainId As Dictionary(Of Integer, List(Of Integer))
        Private _companiesByCountryId As Dictionary(Of Integer, List(Of Integer))

        ' 统计
        Private _totalDays As Integer = 0
        Private _totalEvents As Integer = 0
        Private _startTime As DateTime

        Public Sub New(config As SimulationConfig)
            _config = config
            _rng = New SeededRandom(config.Seed)
        End Sub

        ''' <summary>运行完整模拟。</summary>
        Public Sub Run()
            _startTime = DateTime.Now
            Console.WriteLine("=== SuperMarket 股票交易数据生成模拟器 ===")
            Console.WriteLine($"配置: 起始={_config.StartDate:yyyy-MM-dd} 结束={_config.EndDate:yyyy-MM-dd} 分辨率={_config.Resolution}")
            Console.WriteLine($"规模: 国家={_config.CountryCount} 公司={_config.CompanyCount} 种子={_config.Seed}")
            Console.WriteLine($"输出: {_config.OutputPath}")
            Console.WriteLine()

            ' 1. 初始化世界
            Console.WriteLine("[1/4] 初始化世界...")
            InitializeWorld()
            Console.WriteLine($"  国家: {_countries.Count}, 公司: {_companies.Count}, 股票: {_stocks.Count}")
            Console.WriteLine($"  产业链: {_chains.Count}, 产品: {_products.Count}, 投资者: {_investors.Count}")
            Console.WriteLine($"  关系边: {_relationships.Count}")

            ' 2. 构建蝴蝶效应网络
            Console.WriteLine("[2/4] 构建蝴蝶效应网络...")
            _network = New ButterflyEffectNetwork()
            _network.BuildFromRelationships(_relationships)

            ' 3. 初始化引擎
            Console.WriteLine("[3/4] 初始化引擎...")
            InitializeEngines()

            ' 4. 初始化输出
            Console.WriteLine("[4/4] 初始化输出目录...")
            _outputManager = New OutputManager(_config)
            _outputManager.Initialize()

            ' 写入拓扑文件（在模拟开始前，反映初始世界结构）
            ' 注意：事件流在模拟过程中累积，最后写入

            ' 主模拟循环
            Console.WriteLine()
            Console.WriteLine("开始模拟...")
            Dim tradingDays = GetTradingDays(_config.StartDate, _config.EndDate)
            _totalDays = tradingDays.Count
            Dim currentDayIndex As Integer = 0

            For Each tradingDay In tradingDays
                RunSingleDay(tradingDay)
                currentDayIndex += 1

                ' 进度输出（每 10% 或最后一天）
                If currentDayIndex Mod Math.Max(1, _totalDays \ 10) = 0 OrElse currentDayIndex = _totalDays Then
                    Dim pct As Integer = CInt(currentDayIndex * 100.0 / _totalDays)
                    Console.WriteLine($"  进度: {currentDayIndex}/{_totalDays} ({pct}%) - {tradingDay:yyyy-MM-dd}")
                End If
            Next

            ' 最终刷盘
            Console.WriteLine()
            Console.WriteLine("写入最终数据...")
            _outputManager.FlushCsv()
            _outputManager.WriteTopologyFiles(_countries, _companies, _stocks, _chains, _investors, _relationships)

            ' 统计
            Dim elapsed = DateTime.Now - _startTime
            Console.WriteLine()
            Console.WriteLine("=== 模拟完成 ===")
            Console.WriteLine($"总交易日: {_totalDays}")
            Console.WriteLine($"总事件数: {_totalEvents}")
            Console.WriteLine($"耗时: {elapsed.TotalSeconds:F1} 秒")
            Console.WriteLine(_outputManager.GetStats())
        End Sub

        ''' <summary>初始化世界实体。</summary>
        Private Sub InitializeWorld()
            Dim setup As New WorldSetup(_rng, _config)
            setup.Generate(_countries, _companies, _stocks, _chains, _products, _investors, _relationships)

            ' 构建索引
            _companiesByChainId = New Dictionary(Of Integer, List(Of Integer))()
            _companiesByCountryId = New Dictionary(Of Integer, List(Of Integer))()

            For Each comp In _companies
                ' 按产业链索引
                If Not _companiesByChainId.ContainsKey(comp.IndustryChainId) Then
                    _companiesByChainId(comp.IndustryChainId) = New List(Of Integer)()
                End If
                _companiesByChainId(comp.IndustryChainId).Add(comp.CompanyId)

                ' 按国家索引
                If Not _companiesByCountryId.ContainsKey(comp.CountryId) Then
                    _companiesByCountryId(comp.CountryId) = New List(Of Integer)()
                End If
                _companiesByCountryId(comp.CountryId).Add(comp.CompanyId)
            Next
        End Sub

        ''' <summary>初始化所有引擎并注入依赖。</summary>
        Private Sub InitializeEngines()
            ' 事件传播器
            _eventPropagator = New EventPropagator(_network, _config)
            _eventPropagator.SetIndices(_companiesByChainId, _companiesByCountryId)

            ' 事件生成器
            _eventGenerator = New EventGenerator(_rng, _config)
            _eventGenerator.SetWorld(_countries, _companies, _investors, _chains)

            ' 核心引擎
            _macroEngine = New MacroEngine(_rng, _config)
            _macroEngine.SetWorld(_countries, _eventPropagator)

            _chainEngine = New IndustryChainEngine(_rng, _config)
            _chainEngine.SetWorld(_countries, _chains, _products, _eventPropagator)

            _companyEngine = New CompanyEngine(_rng, _config)
            _companyEngine.SetWorld(_countries, _companies, _chains, _eventPropagator, _macroEngine)

            _stockEngine = New StockPriceEngine(_rng, _config)
            _stockEngine.SetWorld(_countries, _companies, _stocks, _eventPropagator, _macroEngine)

            ' 扩展引擎
            _derivativesEngine = New DerivativesEngine(_rng, _config)
            _derivativesEngine.SetWorld(_countries, _stocks, _chains, _macroEngine, _eventPropagator)

            _microstructureEngine = New MicrostructureEngine(_rng, _config)
            _microstructureEngine.SetWorld(_stocks, _investors, _macroEngine, _eventPropagator)

            _alternativeDataEngine = New AlternativeDataEngine(_rng, _config)
            _alternativeDataEngine.SetWorld(_countries, _companies, _stocks, _chains, _macroEngine, _eventPropagator)
        End Sub

        ''' <summary>执行单个交易日的全部模拟步骤。</summary>
        Private Sub RunSingleDay(tradingDay As Date)
            ' 清空传播器累积
            _eventPropagator.ResetForNewStep()

            ' 1. 宏观引擎
            _macroEngine.[Step](tradingDay)

            ' 2. 产业链引擎
            _chainEngine.[Step](tradingDay)

            ' 3. 公司引擎
            _companyEngine.[Step](tradingDay)

            ' 4. 事件生成
            Dim events = _eventGenerator.Generate(tradingDay)
            _totalEvents += events.Count

            ' 5. 事件传播
            For Each ev In events
                _eventPropagator.Propagate(ev)
            Next

            ' 6. 股价定价
            _stockEngine.[Step](tradingDay)

            ' 7. 衍生品
            _derivativesEngine.[Step](tradingDay)

            ' 8. 微观结构
            _microstructureEngine.[Step](tradingDay)

            ' 9. 另类数据
            _alternativeDataEngine.[Step](tradingDay)

            ' 10. 输出
            WriteDailyOutput(tradingDay)

            ' 累积事件流
            _outputManager.BufferEvents(events)

            ' 定期刷盘
            _outputManager.BufferStep()
        End Sub

        ''' <summary>写入当日所有数据到输出管理器。</summary>
        Private Sub WriteDailyOutput(tradingDay As Date)
            ' 量价数据
            For Each stk In _stocks
                _outputManager.WritePriceData(tradingDay, stk)
            Next

            ' 基本面数据
            For Each comp In _companies
                _outputManager.WriteFundamentalsData(tradingDay, comp)
            Next

            ' 宏观数据
            For Each c In _countries
                _outputManager.WriteMacroData(tradingDay, c)
            Next

            ' 供应链数据
            For Each rec In _alternativeDataEngine.SupplyChainRecords
                _outputManager.WriteSupplyChainData(rec)
            Next

            ' 高管数据
            For Each rec In _alternativeDataEngine.InsiderRecords
                _outputManager.WriteInsiderData(rec)
            Next

            ' 卫星数据
            For Each rec In _alternativeDataEngine.SatelliteRecords
                _outputManager.WriteSatelliteData(rec)
            Next

            ' 期权数据
            For Each rec In _derivativesEngine.OptionsData
                _outputManager.WriteOptionsData(rec)
            Next

            ' 期货数据
            For Each rec In _derivativesEngine.FuturesData
                _outputManager.WriteFuturesData(rec)
            Next

            ' 大宗商品数据
            For Each rec In _derivativesEngine.CommodityData
                _outputManager.WriteCommodityData(rec)
            Next

            ' 汇率数据
            For Each rec In _derivativesEngine.FxData
                _outputManager.WriteFxData(rec)
            Next

            ' 微观结构数据
            For Each rec In _microstructureEngine.Records
                _outputManager.WriteMicrostructureData(rec)
            Next
        End Sub

        ''' <summary>生成交易日历（跳过周末）。</summary>
        Private Function GetTradingDays(startDate As Date, endDate As Date) As List(Of Date)
            Dim days As New List(Of Date)()
            Dim current = startDate
            While current <= endDate
                ' 跳过周六周日
                If current.DayOfWeek <> DayOfWeek.Saturday AndAlso current.DayOfWeek <> DayOfWeek.Sunday Then
                    days.Add(current)
                End If
                current = current.AddDays(1)
            End While
            Return days
        End Function

    End Class

End Namespace
