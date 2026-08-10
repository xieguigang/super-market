Option Strict On
Option Explicit On

Imports SuperMarket.Core
Imports SuperMarket.Entities

Namespace SuperMarket.Simulation

    ''' <summary>
    ''' 另类数据引擎：新闻情绪文本、社交媒体情绪、供应链订单数据、
    ''' 高管增减持记录、股权质押比例、限售解禁日期、卫星图像代理数据。
    ''' </summary>
    Public Class AlternativeDataEngine

        Private ReadOnly _rng As SeededRandom
        Private ReadOnly _config As SimulationConfig
        Private _countries As List(Of Country)
        Private _companies As List(Of Company)
        Private _stocks As List(Of Stock)
        Private _chains As List(Of IndustryChain)
        Private _macroEngine As MacroEngine
        Private _eventPropagator As Events.EventPropagator

        ' 当日另类数据记录
        Public Property NewsRecords As New List(Of NewsRecord)()
        Public Property SupplyChainRecords As New List(Of SupplyChainRecord)()
        Public Property InsiderRecords As New List(Of InsiderRecord)()
        Public Property SatelliteRecords As New List(Of SatelliteRecord)()

        ' 新闻模板（按事件类型）
        Private Shared ReadOnly NewsTemplates As New Dictionary(Of EventType, String()) From
        {
            {EventType.TariffChange, (New String() {"{0}宣布调整进口关税，市场担忧贸易摩擦升级", "关税政策突变，{0}相关板块应声波动"})},
            {EventType.PolicyChange, (New String() {"{0}出台新监管政策，行业格局或重塑", "监管层出手，{0}市场迎来政策拐点"})},
            {EventType.RDBreakthrough, (New String() {"{0}宣布重大研发突破，股价跳涨", "{0}新品亮相，技术壁垒再升级"})},
            {EventType.InvestorChase, (New String() {"资金涌入{0}，散户追高情绪蔓延", "{0}成交放量，游资接力炒作"})},
            {EventType.WhaleShort, (New String() {"巨头做空{0}，市场恐慌情绪加剧", "空头突袭{0}，股价承压下行"})},
            {EventType.WarFriction, (New String() {"地缘冲突升级，{0}市场剧烈震荡", "战争阴云笼罩，{0}风险资产遭抛售"})},
            {EventType.CurrencyDevaluation, (New String() {"{0}货币大幅贬值，输入性通胀压力上升", "汇率波动加剧，{0}外向型企业承压"})},
            {EventType.InterestRateChange, (New String() {"{0}央行调整利率，流动性预期生变", "利率决议落地，{0}债市股市联动反应"})},
            {EventType.SupplyChainDisruption, (New String() {"{0}供应链中断，生产端告急", "原材料断供，{0}产业链上下游受冲击"})},
            {EventType.MarketCrash, (New String() {"全球市场崩盘，{0}无一幸免", "恐慌性抛售蔓延，{0}股价雪崩"})},
            {EventType.EarningsSurprise, (New String() {"{0}财报超预期，机构上调评级", "{0}业绩亮眼，股价应声上涨"})},
            {EventType.InsiderTrading, (New String() {"{0}高管大举增持，内部人看多信号", "{0}高管减持套现，市场关注后续动向"})},
            {EventType.PledgeUnlock, (New String() {"{0}限售股解禁在即，抛压担忧升温", "{0}解禁洪峰来袭，流动性面临考验"})},
            {EventType.CommodityShock, (New String() {"大宗商品异动，{0}成本端承压", "原材料价格飙升，{0}利润空间受挤压"})},
            {EventType.Pandemic, (New String() {"黑天鹅突袭，{0}市场陷入混乱", "不确定性飙升，{0}避险情绪爆发"})}
        }

        Public Sub New(rng As SeededRandom, config As SimulationConfig)
            _rng = rng
            _config = config
        End Sub

        Public Sub SetWorld(
            countries As List(Of Country),
            companies As List(Of Company),
            stocks As List(Of Stock),
            chains As List(Of IndustryChain),
            macroEngine As MacroEngine,
            eventPropagator As Events.EventPropagator
        )
            _countries = countries
            _companies = companies
            _stocks = stocks
            _chains = chains
            _macroEngine = macroEngine
            _eventPropagator = eventPropagator
        End Sub

        Public Sub [Step](day As Date)
            NewsRecords.Clear()
            SupplyChainRecords.Clear()
            InsiderRecords.Clear()
            SatelliteRecords.Clear()

            If _eventPropagator Is Nothing Then Return

            ' === 新闻情绪：基于当日事件生成 ===
            For Each ev In _eventPropagator.ProcessedEvents
                Dim templates As String() = Nothing
                If NewsTemplates.ContainsKey(ev.EventType) Then
                    templates = NewsTemplates(ev.EventType)
                Else
                    templates = New String() {"{0}发生市场事件，影响待观察"}
                End If
                Dim tmpl = _rng.Choice(templates)
                Dim entityName As String = GetEntityName(ev.SourceEntityType, ev.SourceEntityId)
                Dim headline = String.Format(tmpl, entityName)

                ' 情绪得分 [-1, 1]：基于事件初始冲击
                Dim sentimentScore As Double = ev.InitialImpact
                Dim level As SentimentLevel = ScoreToLevel(sentimentScore)

                NewsRecords.Add(New NewsRecord() With {
                    .Date = day,
                    .EventId = ev.EventId,
                    .EventType = ev.EventType.ToString(),
                    .Headline = headline,
                    .SourceEntity = $"{ev.SourceEntityType}#{ev.SourceEntityId}",
                    .SentimentScore = Math.Round(sentimentScore, 3),
                    .SentimentLevel = level.ToString(),
                    .PropagationDepth = ev.PropagationPath.Count
                })
            Next

            ' === 供应链订单数据：每家公司 ===
            If _companies IsNot Nothing AndAlso _chains IsNot Nothing Then
                For Each comp In _companies
                    Dim chain As IndustryChain = If(comp.IndustryChainId < _chains.Count, _chains(comp.IndustryChainId), Nothing)
                    If chain Is Nothing Then Continue For

                    Dim eventImpact As Double = _eventPropagator.GetCompanyImpact(comp.CompanyId)
                    Dim orderVolume As Double = chain.Demand * (1.0 + eventImpact * 0.2) * _rng.NextDouble(0.8, 1.2)
                    Dim supplierOrderValue As Double = orderVolume * chain.ProductPrice * 0.7
                    Dim inventoryTurnover As Double = If(chain.Inventory > 0, chain.Supply / chain.Inventory, 1.0)
                    Dim customerDemandIndex As Double = chain.Demand / 100.0 + eventImpact

                    SupplyChainRecords.Add(New SupplyChainRecord() With {
                        .Date = day,
                        .Ticker = comp.Ticker,
                        .OrderVolume = Math.Round(orderVolume, 2),
                        .SupplierOrderValue = Math.Round(supplierOrderValue, 2),
                        .InventoryTurnover = Math.Round(inventoryTurnover, 3),
                        .CustomerDemandIndex = Math.Round(customerDemandIndex, 3)
                    })
                Next
            End If

            ' === 高管增减持记录 ===
            If _companies IsNot Nothing Then
                ' 每日随机选取若干公司生成高管交易记录
                Dim numInsiderEvents As Integer = _rng.[Next](0, 5)
                For i = 0 To numInsiderEvents - 1
                    Dim comp = _rng.Choice(_companies)
                    Dim eventImpact As Double = _eventPropagator.GetCompanyImpact(comp.CompanyId)
                    ' 高管在公司有利好时增持，利空时减持
                    Dim isBuy As Boolean = eventImpact > 0 OrElse _rng.NextBoolean(0.4)
                    ' 通过总资产估算市值代理
                    Dim marketCapProxy As Double = comp.TotalAssets * 1.5
                    Dim amount As Double = marketCapProxy * _rng.NextDouble(0.001, 0.01)
                    InsiderRecords.Add(New InsiderRecord() With {
                        .Date = day,
                        .Ticker = comp.Ticker,
                        .Action = If(isBuy, "Buy", "Sell"),
                        .BuyAmount = If(isBuy, Math.Round(amount, 2), 0.0),
                        .SellAmount = If(Not isBuy, Math.Round(amount, 2), 0.0),
                        .PledgeRatio = Math.Round(comp.PledgeRatio, 4),
                        .UnlockDate = comp.NextUnlockDate,
                        .UnlockShares = Math.Round(comp.NextUnlockShares, 2)
                    })
                Next
            End If

            ' === 卫星图像代理数据（零售/消费类公司） ===
            If _companies IsNot Nothing Then
                For Each comp In _companies.Where(Function(c) c.ProductCategory = ProductCategory.ConsumerGoods OrElse c.ProductCategory = ProductCategory.Agricultural).Take(20)
                    Dim eventImpact As Double = _eventPropagator.GetCompanyImpact(comp.CompanyId)
                    ' 停车场车辆数 = 营收代理指标
                    Dim vehicleCount As Integer = CInt(comp.Revenue * 0.01 * (1.0 + eventImpact * 0.1) * _rng.NextDouble(0.8, 1.2))
                    SatelliteRecords.Add(New SatelliteRecord() With {
                        .Date = day,
                        .Ticker = comp.Ticker,
                        .VehicleCount = vehicleCount,
                        .RevenueProxy = Math.Round(comp.Revenue * (1.0 + eventImpact * 0.1), 2)
                    })
                Next
            End If
        End Sub

        Private Function GetEntityName(entityType As EntityType, entityId As Integer) As String
            Select Case entityType
                Case EntityType.Country
                    If _countries IsNot Nothing AndAlso entityId < _countries.Count Then
                        Return _countries(entityId).Name
                    End If
                Case EntityType.Company
                    If _companies IsNot Nothing AndAlso entityId < _companies.Count Then
                        Return _companies(entityId).Ticker
                    End If
                Case EntityType.Investor
                    Return $"投资者#{entityId}"
                Case EntityType.IndustryChain
                    If _chains IsNot Nothing AndAlso entityId < _chains.Count Then
                        Return _chains(entityId).Name
                    End If
            End Select
            Return $"实体#{entityId}"
        End Function

        Private Function ScoreToLevel(score As Double) As SentimentLevel
            If score <= -0.3 Then Return SentimentLevel.VeryNegative
            If score < -0.05 Then Return SentimentLevel.Negative
            If score <= 0.05 Then Return SentimentLevel.Neutral
            If score < 0.3 Then Return SentimentLevel.Positive
            Return SentimentLevel.VeryPositive
        End Function

    End Class

    ' === 另类数据记录结构 ===
    Public Class NewsRecord
        Public Property [Date] As Date
        Public Property EventId As Integer
        Public Property EventType As String
        Public Property Headline As String
        Public Property SourceEntity As String
        Public Property SentimentScore As Double
        Public Property SentimentLevel As String
        Public Property PropagationDepth As Integer
    End Class

    Public Class SupplyChainRecord
        Public Property [Date] As Date
        Public Property Ticker As String
        Public Property OrderVolume As Double
        Public Property SupplierOrderValue As Double
        Public Property InventoryTurnover As Double
        Public Property CustomerDemandIndex As Double
    End Class

    Public Class InsiderRecord
        Public Property [Date] As Date
        Public Property Ticker As String
        Public Property Action As String
        Public Property BuyAmount As Double
        Public Property SellAmount As Double
        Public Property PledgeRatio As Double
        Public Property UnlockDate As Date?
        Public Property UnlockShares As Double
    End Class

    Public Class SatelliteRecord
        Public Property [Date] As Date
        Public Property Ticker As String
        Public Property VehicleCount As Integer
        Public Property RevenueProxy As Double
    End Class

End Namespace
