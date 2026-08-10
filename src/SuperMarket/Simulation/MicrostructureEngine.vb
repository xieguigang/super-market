Option Strict On
Option Explicit On

Imports SuperMarket.Core
Imports SuperMarket.Entities

Namespace SuperMarket.Simulation

    ''' <summary>
    ''' 微观结构引擎（日频聚合）：订单流不平衡度、大单成交统计、
    ''' 主买/主卖比例、滑点估算。基于当日量价数据和情绪估算。
    ''' </summary>
    Public Class MicrostructureEngine

        Private ReadOnly _rng As SeededRandom
        Private ReadOnly _config As SimulationConfig
        Private _stocks As List(Of Stock)
        Private _investors As List(Of Investor)
        Private _macroEngine As MacroEngine
        Private _eventPropagator As Events.EventPropagator

        ' 当日微观结构记录（供输出层使用）
        Public Property Records As New List(Of MicrostructureRecord)()

        Public Sub New(rng As SeededRandom, config As SimulationConfig)
            _rng = rng
            _config = config
        End Sub

        Public Sub SetWorld(
            stocks As List(Of Stock),
            investors As List(Of Investor),
            macroEngine As MacroEngine,
            eventPropagator As Events.EventPropagator
        )
            _stocks = stocks
            _investors = investors
            _macroEngine = macroEngine
            _eventPropagator = eventPropagator
        End Sub

        Public Sub [Step](day As Date)
            Records.Clear()
            If _stocks Is Nothing Then Return

            Dim sentiment As Double = If(_macroEngine IsNot Nothing, _macroEngine.MarketSentiment, 0.0)

            For Each stk In _stocks
                Dim eventImpact As Double = If(_eventPropagator IsNot Nothing, _eventPropagator.GetCompanyImpact(stk.CompanyId), 0.0)

                ' === 订单流不平衡度 [-1, 1] ===
                ' 基于当日涨跌和事件冲击估算买卖压力差异
                Dim dailyReturn As Double = If(stk.PreviousClose > 0, stk.Close / stk.PreviousClose - 1.0, 0.0)
                Dim orderImbalance As Double = Math.Tanh(dailyReturn * 20.0 + eventImpact * 2.0 + _rng.NextGaussian(0, 0.1))

                ' === 大单成交统计 ===
                ' 大单比例取决于巨头和机构持仓
                Dim whaleRatio As Double = 0.1
                Dim instRatio As Double = 0.3
                If _investors IsNot Nothing Then
                    Dim whales = _investors.Where(Function(i) i.Type = InvestorType.Whale).ToList()
                    Dim insts = _investors.Where(Function(i) i.Type = InvestorType.Institutional).ToList()
                    If _investors.Count > 0 Then
                        whaleRatio = whales.Count / _investors.Count
                        instRatio = insts.Count / _investors.Count
                    End If
                End If
                Dim largeOrderRatio As Double = whaleRatio + instRatio * 0.5 + Math.Abs(eventImpact) * 0.2
                largeOrderRatio = Math.Max(0.05, Math.Min(0.6, largeOrderRatio))
                Dim largeOrderCount As Integer = CInt(stk.Volume * largeOrderRatio / 10.0)
                Dim largeOrderAmount As Double = stk.Amount * largeOrderRatio

                ' === 主买/主卖比例 ===
                ' 情绪乐观时主买比例高
                Dim buyRatio As Double = 0.5 + sentiment * 0.2 + orderImbalance * 0.15 + _rng.NextGaussian(0, 0.03)
                buyRatio = Math.Max(0.2, Math.Min(0.8, buyRatio))
                Dim sellRatio As Double = 1.0 - buyRatio

                ' === 滑点估算（bps） ===
                ' 滑点与成交量成反比，与波动率成正比
                Dim slippageBps As Double = stk.DailyVolatility * 10000.0 * 0.5 / Math.Max(1.0, Math.Sqrt(stk.Volume / 1000.0))
                slippageBps += _rng.NextGaussian(0, 2.0)
                slippageBps = Math.Max(0.5, Math.Min(100.0, slippageBps))

                Records.Add(New MicrostructureRecord() With {
                    .Date = day,
                    .Ticker = stk.Ticker,
                    .OrderImbalance = Math.Round(orderImbalance, 4),
                    .LargeOrderCount = largeOrderCount,
                    .LargeOrderAmount = Math.Round(largeOrderAmount, 2),
                    .BuyRatio = Math.Round(buyRatio, 4),
                    .SellRatio = Math.Round(sellRatio, 4),
                    .Slippage = Math.Round(slippageBps, 2)
                })
            Next
        End Sub

    End Class

    Public Class MicrostructureRecord
        Public Property [Date] As Date
        Public Property Ticker As String
        Public Property OrderImbalance As Double
        Public Property LargeOrderCount As Integer
        Public Property LargeOrderAmount As Double
        Public Property BuyRatio As Double
        Public Property SellRatio As Double
        Public Property Slippage As Double
    End Class

End Namespace
