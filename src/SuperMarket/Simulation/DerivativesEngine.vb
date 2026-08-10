Option Strict On
Option Explicit On

Imports SuperMarket.Core
Imports SuperMarket.Entities

Namespace SuperMarket.Simulation

    ''' <summary>
    ''' 衍生品引擎：期权隐含波动率、看跌/看涨比率、股指期货基差、
    ''' 大宗商品价格、汇率数据。基于标的股波动率+VIX+事件冲击。
    ''' </summary>
    Public Class DerivativesEngine

        Private ReadOnly _rng As SeededRandom
        Private ReadOnly _config As SimulationConfig
        Private _countries As List(Of Country)
        Private _stocks As List(Of Stock)
        Private _chains As List(Of IndustryChain)
        Private _macroEngine As MacroEngine
        Private _eventPropagator As Events.EventPropagator

        ' 当日衍生品数据记录（供输出层使用）
        Public Property OptionsData As New List(Of OptionsRecord)()
        Public Property FuturesData As New List(Of FuturesRecord)()
        Public Property CommodityData As New List(Of CommodityRecord)()
        Public Property FxData As New List(Of FxRecord)()

        Public Sub New(rng As SeededRandom, config As SimulationConfig)
            _rng = rng
            _config = config
        End Sub

        Public Sub SetWorld(
            countries As List(Of Country),
            stocks As List(Of Stock),
            chains As List(Of IndustryChain),
            macroEngine As MacroEngine,
            eventPropagator As Events.EventPropagator
        )
            _countries = countries
            _stocks = stocks
            _chains = chains
            _macroEngine = macroEngine
            _eventPropagator = eventPropagator
        End Sub

        Public Sub [Step](day As Date)
            OptionsData.Clear()
            FuturesData.Clear()
            CommodityData.Clear()
            FxData.Clear()

            If _stocks Is Nothing Then Return

            Dim vix As Double = If(_macroEngine IsNot Nothing, _macroEngine.GlobalVIX, 15.0)
            Dim sentiment As Double = If(_macroEngine IsNot Nothing, _macroEngine.MarketSentiment, 0.0)

            ' === 期权数据：每只股票生成一条期权汇总 ===
            For Each stk In _stocks
                Dim eventImpact As Double = If(_eventPropagator IsNot Nothing, _eventPropagator.GetCompanyImpact(stk.CompanyId), 0.0)
                ' 隐含波动率 = 标的波动率 + VIX 贡献 + 事件冲击
                Dim iv As Double = stk.Volatility * 100.0 + (vix - 15.0) * 0.3 + Math.Abs(eventImpact) * 20.0
                iv = Math.Max(5.0, Math.Min(150.0, iv))
                ' 看跌/看涨比率：情绪悲观时升高
                Dim pcr As Double = 1.0 - sentiment * 0.5 + _rng.NextGaussian(0, 0.1)
                pcr = Math.Max(0.3, Math.Min(3.0, pcr))
                ' 期权成交量
                Dim optVol As Double = stk.Volume * _rng.NextDouble(0.1, 0.5)

                OptionsData.Add(New OptionsRecord() With {
                    .Date = day,
                    .Underlying = stk.Ticker,
                    .ImpliedVolatility = Math.Round(iv, 2),
                    .PutCallRatio = Math.Round(pcr, 3),
                    .OptionVolume = Math.Round(optVol, 2)
                })
            Next

            ' === 期货数据：股指期货基差（以每国代表股票为标的） ===
            If _countries IsNot Nothing Then
                For Each c In _countries
                    Dim countryStocks = _stocks.Where(Function(s) s.CountryId = c.CountryId).ToList()
                    If countryStocks.Count = 0 Then Continue For
                    ' 选取该国前 5 只股票作为指数成分
                    Dim indexStocks = countryStocks.Take(Math.Min(5, countryStocks.Count)).ToList()
                    Dim spotIndex As Double = indexStocks.Average(Function(s) s.Close)
                    ' 期货价格 = 现货 × (1 + 无风险利率 - 股息率) ^ T
                    Dim riskFreeRate As Double = c.InterestRate / 100.0
                    Dim dividendYield As Double = indexStocks.Average(Function(s) s.DividendYield)
                    Dim T As Double = 0.25  ' 3 个月合约
                    Dim futuresPrice As Double = spotIndex * Math.Exp((riskFreeRate - dividendYield) * T)
                    Dim basis As Double = futuresPrice - spotIndex
                    Dim basisPct As Double = If(spotIndex > 0, basis / spotIndex, 0.0)

                    FuturesData.Add(New FuturesRecord() With {
                        .Date = day,
                        .Contract = $"{c.CurrencyCode}_IDX_FUT",
                        .FuturesPrice = Math.Round(futuresPrice, 4),
                        .SpotPrice = Math.Round(spotIndex, 4),
                        .Basis = Math.Round(basis, 4),
                        .BasisPct = Math.Round(basisPct, 4)
                    })
                Next
            End If

            ' === 大宗商品数据：按产品类别聚合 ===
            If _chains IsNot Nothing Then
                Dim byCategory = _chains.GroupBy(Function(ch) ch.Category).ToList()
                For Each grp In byCategory
                    Dim avgPrice As Double = grp.Average(Function(ch) ch.ProductPrice)
                    Dim totalVolume As Double = grp.Sum(Function(ch) ch.Supply)
                    Dim changePct As Double = If(grp.First().BasePrice > 0, avgPrice / grp.First().BasePrice - 1.0, 0.0)

                    CommodityData.Add(New CommodityRecord() With {
                        .Date = day,
                        .Commodity = grp.Key.ToString(),
                        .Price = Math.Round(avgPrice, 4),
                        .Volume = Math.Round(totalVolume, 2),
                        .ChangePct = Math.Round(changePct, 4)
                    })
                Next
            End If

            ' === 汇率数据：每对本币 vs USD ===
            If _countries IsNot Nothing Then
                For Each c In _countries
                    If c.CurrencyCode = "USD" Then Continue For
                    Dim changePct As Double = If(c.PreviousExchangeRate > 0, c.ExchangeRate / c.PreviousExchangeRate - 1.0, 0.0)
                    FxData.Add(New FxRecord() With {
                        .Date = day,
                        .Pair = $"{c.CurrencyCode}USD",
                        .Rate = Math.Round(c.ExchangeRate, 6),
                        .ChangePct = Math.Round(changePct, 5)
                    })
                Next
            End If
        End Sub

    End Class

    ' === 衍生品数据记录结构 ===
    Public Class OptionsRecord
        Public Property [Date] As Date
        Public Property Underlying As String
        Public Property ImpliedVolatility As Double
        Public Property PutCallRatio As Double
        Public Property OptionVolume As Double
    End Class

    Public Class FuturesRecord
        Public Property [Date] As Date
        Public Property Contract As String
        Public Property FuturesPrice As Double
        Public Property SpotPrice As Double
        Public Property Basis As Double
        Public Property BasisPct As Double
    End Class

    Public Class CommodityRecord
        Public Property [Date] As Date
        Public Property Commodity As String
        Public Property Price As Double
        Public Property Volume As Double
        Public Property ChangePct As Double
    End Class

    Public Class FxRecord
        Public Property [Date] As Date
        Public Property Pair As String
        Public Property Rate As Double
        Public Property ChangePct As Double
    End Class

End Namespace
