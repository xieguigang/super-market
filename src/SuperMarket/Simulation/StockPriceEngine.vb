Option Strict On
Option Explicit On

Imports SuperMarket.Core
Imports SuperMarket.Entities

Namespace SuperMarket.Simulation

    ''' <summary>
    ''' 股价引擎：多因子定价模型——
    ''' 基本面价值(EPS×合理PE) × 宏观调整因子(利率/经济增长) × 事件冲击因子 × 情绪因子 × 随机噪声。
    ''' 生成 OHLC/VWAP/成交量/换手率/成交金额，处理涨跌停限制。
    ''' </summary>
    Public Class StockPriceEngine

        Private ReadOnly _rng As SeededRandom
        Private ReadOnly _config As SimulationConfig
        Private _countries As List(Of Country)
        Private _companies As List(Of Company)
        Private _stocks As List(Of Stock)
        Private _eventPropagator As Events.EventPropagator
        Private _macroEngine As MacroEngine

        Public Sub New(rng As SeededRandom, config As SimulationConfig)
            _rng = rng
            _config = config
        End Sub

        Public Sub SetWorld(
            countries As List(Of Country),
            companies As List(Of Company),
            stocks As List(Of Stock),
            eventPropagator As Events.EventPropagator,
            macroEngine As MacroEngine
        )
            _countries = countries
            _companies = companies
            _stocks = stocks
            _eventPropagator = eventPropagator
            _macroEngine = macroEngine
        End Sub

        Public Sub [Step](day As Date)
            If _stocks Is Nothing Then Return

            For Each stk In _stocks
                Dim comp As Company = If(stk.CompanyId < _companies.Count, _companies(stk.CompanyId), Nothing)
                Dim country As Country = If(stk.CountryId < _countries.Count, _countries(stk.CountryId), Nothing)
                If comp Is Nothing Then Continue For

                ' 重置当日数据
                stk.ResetForNewDay()

                ' === 1. 基本面价值：EPS × 合理 PE ===
                Dim eps As Double = If(comp.SharesOutstanding > 0, comp.NetProfit / comp.SharesOutstanding, 1.0)
                ' 合理 PE 受行业增长率和利率影响
                Dim fairPE As Double = 20.0
                If comp.Momentum > 0 Then fairPE *= (1.0 + comp.Momentum * 0.5)
                If country IsNot Nothing Then
                    fairPE *= (1.0 - (country.InterestRate - 2.0) * 0.05)
                End If
                fairPE = Math.Max(5.0, Math.Min(60.0, fairPE))
                stk.FairValue = Math.Max(0.5, eps * fairPE)

                ' === 2. 宏观调整因子 ===
                Dim macroFactor As Double = 1.0
                If country IsNot Nothing Then
                    ' 利率上升 → 估值压缩
                    macroFactor *= (1.0 - (country.InterestRate - 2.5) * 0.02)
                    ' GDP 增长 → 盈利预期上调
                    macroFactor *= (1.0 + country.GDPGrowth / 100.0 * 0.1)
                End If

                ' === 3. 事件冲击因子 ===
                Dim eventImpact As Double = If(_eventPropagator IsNot Nothing, _eventPropagator.GetCompanyImpact(stk.CompanyId), 0.0)
                Dim eventFactor As Double = 1.0 + eventImpact * 0.15

                ' === 4. 情绪因子 ===
                Dim vix As Double = If(_macroEngine IsNot Nothing, _macroEngine.GlobalVIX, 15.0)
                Dim sentiment As Double = If(_macroEngine IsNot Nothing, _macroEngine.MarketSentiment, 0.0)
                ' VIX 高 → 偏离基本面，恐慌折价
                Dim sentimentFactor As Double = 1.0 - (vix - 15.0) / 100.0 + sentiment * 0.05
                ' 动量效应
                sentimentFactor *= (1.0 + comp.Momentum * 0.1)

                ' === 5. 随机噪声 ===
                Dim noise As Double = _rng.NextGaussian(0, stk.DailyVolatility)

                ' === 综合目标价 ===
                Dim targetClose As Double = stk.FairValue * macroFactor * eventFactor * sentimentFactor * (1.0 + noise)

                ' === 涨跌停限制 ===
                Dim limitUp As Double = stk.PreviousClose * (1.0 + _config.PriceLimitPct)
                Dim limitDown As Double = stk.PreviousClose * (1.0 - _config.PriceLimitPct)
                targetClose = Math.Max(limitDown, Math.Min(limitUp, targetClose))
                targetClose = Math.Max(0.1, targetClose)

                ' === 开盘价：前收 + 隔夜跳空 ===
                Dim gap As Double = _rng.NextGaussian(0, stk.DailyVolatility * 0.5)
                stk.Open = Math.Max(0.1, stk.PreviousClose * (1.0 + gap))
                stk.Open = Math.Max(limitDown, Math.Min(limitUp, stk.Open))

                ' === 收盘价 ===
                stk.Close = targetClose

                ' === 最高价/最低价：基于日内波动率反推 ===
                Dim intradayRange As Double = stk.DailyVolatility * stk.Close * _rng.NextDouble(0.5, 2.0)
                stk.High = Math.Max(stk.Open, stk.Close) + intradayRange * _rng.NextDouble(0.2, 0.8)
                stk.Low = Math.Min(stk.Open, stk.Close) - intradayRange * _rng.NextDouble(0.2, 0.8)
                stk.High = Math.Max(Math.Max(stk.High, stk.Open), stk.Close)
                stk.Low = Math.Min(Math.Min(stk.Low, stk.Open), stk.Close)
                stk.Low = Math.Max(0.05, stk.Low)
                stk.High = Math.Max(stk.High, stk.Low + 0.01)

                ' === VWAP ===
                stk.VWAP = (stk.Open + stk.High + stk.Low + stk.Close) / 4.0

                ' === 成交量与换手率：受事件冲击和情绪放大 ===
                Dim baseTurnover As Double = 0.02
                Dim turnoverMultiplier As Double = 1.0 + Math.Abs(eventImpact) * 5.0 + Math.Abs(noise) * 10.0
                stk.TurnoverRate = baseTurnover * turnoverMultiplier * comp.FreeFloatRatio
                stk.TurnoverRate = Math.Max(0.001, Math.Min(0.5, stk.TurnoverRate))
                stk.Volume = stk.TurnoverRate * comp.SharesOutstanding * comp.FreeFloatRatio
                stk.Amount = stk.Volume * stk.VWAP

                ' === 市值与估值指标 ===
                stk.MarketCap = stk.Close * comp.SharesOutstanding
                stk.PE = If(eps > 0, stk.Close / eps, 0)
                stk.PB = If(comp.Equity > 0, stk.MarketCap / (comp.Equity * comp.SharesOutstanding / comp.SharesOutstanding), 0)
                stk.PS = If(comp.Revenue > 0, stk.MarketCap / (comp.Revenue * comp.SharesOutstanding / comp.SharesOutstanding), 0)
                stk.DividendYield = Math.Max(0, _rng.NextDouble(0, 0.05))

                ' === 错价程度 ===
                stk.Mispricing = If(stk.FairValue > 0, stk.Close / stk.FairValue - 1.0, 0.0)

                ' === 波动率更新（EMA） ===
                Dim dailyReturn As Double = If(stk.PreviousClose > 0, stk.Close / stk.PreviousClose - 1.0, 0.0)
                stk.DailyVolatility = stk.DailyVolatility * 0.95 + Math.Abs(dailyReturn) * 0.05
                stk.Volatility = stk.DailyVolatility * Math.Sqrt(_config.TradingDaysPerYear)

                ' === 公司动量更新 ===
                comp.RecentReturn = dailyReturn
                comp.Momentum = comp.Momentum * 0.9 + dailyReturn * 0.1
            Next
        End Sub

    End Class

End Namespace
