Option Strict On
Option Explicit On

Namespace Core

    ''' <summary>
    ''' 模拟配置。描述模拟时间范围、规模、随机种子、事件频率与输出路径等。
    ''' </summary>
    Public Class SimulationConfig

        ''' <summary>模拟起始日期（含）。</summary>
        Public Property StartDate As Date = #2020-01-02#

        ''' <summary>模拟结束日期（含）。</summary>
        Public Property EndDate As Date = #2024-12-31#

        ''' <summary>时间分辨率。默认日频。</summary>
        Public Property Resolution As Resolution = Resolution.Daily

        ''' <summary>分钟级模式下每日交易分钟数（默认 240 分钟 = 4 小时）。</summary>
        Public Property MinutesPerDay As Integer = 240

        ''' <summary>国家数量。默认 16。</summary>
        Public Property CountryCount As Integer = 16

        ''' <summary>上市公司数量。默认 120。</summary>
        Public Property CompanyCount As Integer = 120

        ''' <summary>随机种子。默认 42。</summary>
        Public Property Seed As Integer = 42

        ''' <summary>输出根目录。默认 "./Data"。</summary>
        Public Property OutputPath As String = "./Data"

        ''' <summary>每日平均事件数（Poisson λ）。</summary>
        Public Property EventFrequencyPerDay As Double = 2.0

        ''' <summary>每年交易日数。默认 252。</summary>
        Public Property TradingDaysPerYear As Integer = 252

        ''' <summary>蝴蝶效应传播衰减系数（每跳 ×decay）。</summary>
        Public Property EventImpactDecay As Double = 0.6

        ''' <summary>蝴蝶效应最大传播深度（跳数）。</summary>
        Public Property EventMaxDepth As Integer = 3

        ''' <summary>涨跌停限制（0 表示不限制，0.1 表示 ±10%）。</summary>
        Public Property PriceLimitPct As Double = 0.1

        ''' <summary>事件类型概率权重。若为 Nothing 则使用默认权重。</summary>
        Public Property EventWeights As Dictionary(Of EventType, Double)

        ''' <summary>获取默认事件权重表。</summary>
        Public Shared Function DefaultEventWeights() As Dictionary(Of EventType, Double)
            Dim w As New Dictionary(Of EventType, Double) From
            {
                {EventType.TariffChange, 1.0},
                {EventType.PolicyChange, 0.8},
                {EventType.RDBreakthrough, 1.2},
                {EventType.InvestorChase, 1.5},
                {EventType.WhaleShort, 0.8},
                {EventType.WarFriction, 0.3},
                {EventType.CurrencyDevaluation, 0.4},
                {EventType.InterestRateChange, 0.5},
                {EventType.SupplyChainDisruption, 0.9},
                {EventType.MarketCrash, 0.15},
                {EventType.SectorRotation, 0.7},
                {EventType.EarningsSurprise, 1.0},
                {EventType.InsiderTrading, 0.8},
                {EventType.PledgeUnlock, 0.4},
                {EventType.CommodityShock, 0.6},
                {EventType.Pandemic, 0.1}
            }
            Return w
        End Function

        ''' <summary>校验并填充默认值。</summary>
        Public Sub Validate()
            If EndDate < StartDate Then
                Throw New ArgumentException("EndDate 不能早于 StartDate")
            End If
            If CountryCount < 2 Then CountryCount = 16
            If CompanyCount < 10 Then CompanyCount = 120
            If EventFrequencyPerDay < 0 Then EventFrequencyPerDay = 2.0
            If TradingDaysPerYear <= 0 Then TradingDaysPerYear = 252
            If EventImpactDecay <= 0 OrElse EventImpactDecay > 1 Then EventImpactDecay = 0.6
            If EventMaxDepth < 1 Then EventMaxDepth = 3
            If EventWeights Is Nothing Then EventWeights = DefaultEventWeights()
            If String.IsNullOrWhiteSpace(OutputPath) Then OutputPath = "./Data"
        End Sub

        ''' <summary>从命令行参数解析配置。返回 Nothing 表示应退出（如 --help）。</summary>
        Public Shared Function FromArgs(args As String()) As SimulationConfig
            Dim cfg As New SimulationConfig()
            If args Is Nothing Then Return cfg

            Dim i As Integer = 0
            While i < args.Length
                Dim a As String = args(i)
                Select Case a.ToLowerInvariant()
                    Case "--start", "/start"
                        i += 1
                        If i < args.Length Then cfg.StartDate = Date.Parse(args(i))
                    Case "--end", "/end"
                        i += 1
                        If i < args.Length Then cfg.EndDate = Date.Parse(args(i))
                    Case "--resolution", "/resolution"
                        i += 1
                        If i < args.Length Then
                            cfg.Resolution = If(String.Equals(args(i), "minute", StringComparison.OrdinalIgnoreCase),
                                                Resolution.Minute, Resolution.Daily)
                        End If
                    Case "--countries", "/countries"
                        i += 1
                        If i < args.Length Then cfg.CountryCount = Integer.Parse(args(i))
                    Case "--companies", "/companies"
                        i += 1
                        If i < args.Length Then cfg.CompanyCount = Integer.Parse(args(i))
                    Case "--seed", "/seed"
                        i += 1
                        If i < args.Length Then cfg.Seed = Integer.Parse(args(i))
                    Case "--output", "/output"
                        i += 1
                        If i < args.Length Then cfg.OutputPath = args(i)
                    Case "--event-freq", "/event-freq"
                        i += 1
                        If i < args.Length Then cfg.EventFrequencyPerDay = Double.Parse(args(i))
                    Case "--help", "/help", "-h", "/?"
                        Console.WriteLine("SuperMarket 股票交易数据生成模拟器")
                        Console.WriteLine()
                        Console.WriteLine("用法: SuperMarket [--start yyyy-MM-dd] [--end yyyy-MM-dd]")
                        Console.WriteLine("                   [--resolution daily|minute] [--countries N] [--companies N]")
                        Console.WriteLine("                   [--seed N] [--output PATH] [--event-freq LAMBDA]")
                        Console.WriteLine()
                        Console.WriteLine("默认: start=2020-01-02 end=2024-12-31 resolution=daily")
                        Console.WriteLine("      countries=16 companies=120 seed=42 output=./Data event-freq=2.0")
                        Return Nothing
                End Select
                i += 1
            End While

            cfg.Validate()
            Return cfg
        End Function

    End Class

End Namespace
