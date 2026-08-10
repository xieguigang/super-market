Option Strict On
Option Explicit On

Imports SuperMarket.Core
Imports SuperMarket.Entities

Namespace SuperMarket.Simulation

    ''' <summary>
    ''' 宏观引擎：每步更新各国 GDP 增速、CPI/PPI/PMI、利率（泰勒规则）、
    ''' 汇率（利率平价+贸易平衡）、国债收益率曲线、M1/M2、VIX、融资融券、资金流向。
    ''' </summary>
    Public Class MacroEngine

        Private ReadOnly _rng As SeededRandom
        Private ReadOnly _config As SimulationConfig
        Private _countries As List(Of Country)
        Private _eventPropagator As Events.EventPropagator

        ' 全局 VIX（由各国市场波动聚合）
        Public Property GlobalVIX As Double = 15.0

        ' 全局市场情绪 [-1, 1]
        Public Property MarketSentiment As Double = 0.0

        Public Sub New(rng As SeededRandom, config As SimulationConfig)
            _rng = rng
            _config = config
        End Sub

        Public Sub SetWorld(countries As List(Of Country), eventPropagator As Events.EventPropagator)
            _countries = countries
            _eventPropagator = eventPropagator
        End Sub

        ''' <summary>每个时间步执行。</summary>
        Public Sub [Step](day As Date)
            If _countries Is Nothing Then Return

            Dim dailyFactor As Double = 1.0 / _config.TradingDaysPerYear

            For Each c In _countries
                Dim eventImpact As Double = If(_eventPropagator IsNot Nothing, _eventPropagator.GetCountryImpact(c.CountryId), 0.0)

                ' === GDP 增速：受消费+投资+净出口+事件冲击 ===
                Dim consumption As Double = 0.5 + 0.01 * (c.PMI - 50)
                Dim investment As Double = 0.3 - 0.05 * (c.InterestRate - 2.0)
                Dim netExport As Double = 0.2 * eventImpact
                Dim gdpTarget As Double = consumption + investment + netExport
                c.GDPGrowth += (gdpTarget - c.GDPGrowth) * 0.05 * dailyFactor * 252
                c.GDPGrowth += _rng.NextGaussian(0, 0.02)
                c.GDPGrowth = Math.Max(-5.0, Math.Min(10.0, c.GDPGrowth))
                c.GDP *= (1.0 + c.GDPGrowth / 100.0 * dailyFactor)

                ' === CPI：受原材料价格+关税+货币供应+事件 ===
                Dim moneyEffect As Double = (c.M2 / 1000000.0 - 1.0) * 0.5
                Dim cpiTarget As Double = 2.0 + moneyEffect + eventImpact * 2.0
                c.CPI += (cpiTarget - c.CPI) * 0.05
                c.CPI += _rng.NextGaussian(0, 0.05)
                c.CPI = Math.Max(-3.0, Math.Min(15.0, c.CPI))

                ' === PPI：跟随 CPI 但波动更大 ===
                c.PPI += (c.CPI - c.PPI) * 0.03
                c.PPI += _rng.NextGaussian(0, 0.1)
                c.PPI = Math.Max(-5.0, Math.Min(20.0, c.PPI))

                ' === PMI：景气扩散，受 GDP 增长和事件影响 ===
                Dim pmiTarget As Double = 50.0 + c.GDPGrowth * 2.0 + eventImpact * 10.0
                c.PMI += (pmiTarget - c.PMI) * 0.05
                c.PMI += _rng.NextGaussian(0, 0.2)
                c.PMI = Math.Max(30.0, Math.Min(70.0, c.PMI))

                ' === 非农就业 ===
                c.NonFarmPayroll *= (1.0 + c.GDPGrowth / 100.0 * dailyFactor * 0.5)
                c.NonFarmPayroll += _rng.NextGaussian(0, 5)

                ' === 利率：泰勒规则 r = r* + π + 0.5(π - π*) + 0.5(y - y*) ===
                Dim inflationGap As Double = c.CPI - 2.0
                Dim outputGap As Double = c.GDPGrowth - 2.5
                Dim taylorRate As Double = 2.0 + c.CPI + 0.5 * inflationGap + 0.5 * outputGap
                taylorRate = Math.Max(0.0, taylorRate)
                c.InterestRate += (taylorRate - c.InterestRate) * 0.02
                c.InterbankRate = c.InterestRate + _rng.NextGaussian(0, 0.1)

                ' === 国债收益率曲线 ===
                c.TreasuryYield2Y = c.InterestRate + _rng.NextGaussian(0.2, 0.1)
                c.TreasuryYield10Y = c.InterestRate + _rng.NextGaussian(1.0, 0.2) + eventImpact * 0.5
                c.TreasuryYield10Y = Math.Max(0.0, c.TreasuryYield10Y)

                ' === M1/M2 ===
                c.M1 *= (1.0 + (c.GDPGrowth + c.CPI) / 100.0 * dailyFactor)
                c.M2 *= (1.0 + (c.GDPGrowth + c.CPI + 1.0) / 100.0 * dailyFactor)

                ' === 汇率：利率平价 + 贸易平衡 + 事件 ===
                c.PreviousExchangeRate = c.ExchangeRate
                Dim rateDiff As Double = (c.InterestRate - 2.5) / 100.0 * dailyFactor
                Dim tradeBalance As Double = eventImpact * 0.01
                c.ExchangeRate *= (1.0 + rateDiff + tradeBalance + _rng.NextGaussian(0, 0.005))
                c.ExchangeRate = Math.Max(0.0001, c.ExchangeRate)

                ' === 融资融券余额 ===
                c.MarginBalance *= (1.0 + _rng.NextGaussian(0, 0.005))

                ' === 北向/资金流向 ===
                c.NorthFlow += _rng.NextGaussian(0, 200)
                c.NorthFlow *= 0.98  ' 均值回归
            Next

            ' === 全局 VIX：聚合各国市场波动 ===
            Dim avgImpact As Double = 0
            If _countries.Count > 0 Then
                For Each c In _countries
                    avgImpact += Math.Abs(_eventPropagator.GetCountryImpact(c.CountryId))
                Next
                avgImpact /= _countries.Count
            End If
            Dim vixTarget As Double = 15.0 + avgImpact * 40.0
            GlobalVIX += (vixTarget - GlobalVIX) * 0.1
            GlobalVIX += _rng.NextGaussian(0, 0.5)
            GlobalVIX = Math.Max(8.0, Math.Min(80.0, GlobalVIX))

            ' === 市场情绪 ===
            MarketSentiment = -avgImpact + _rng.NextGaussian(0, 0.05)
            MarketSentiment = Math.Max(-1.0, Math.Min(1.0, MarketSentiment))
        End Sub

    End Class

End Namespace
