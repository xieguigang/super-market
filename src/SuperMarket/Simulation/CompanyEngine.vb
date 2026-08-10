Option Strict On
Option Explicit On

Imports SuperMarket.Core
Imports SuperMarket.Entities

Namespace SuperMarket.Simulation

    ''' <summary>
    ''' 公司引擎：每步更新公司营收、成本、利润、现金流、资产负债表，
    ''' 计算 PE/PB/PS/ROE/ROA/毛利率/股息率，处理高管增减持/股权质押/限售解禁。
    ''' </summary>
    Public Class CompanyEngine

        Private ReadOnly _rng As SeededRandom
        Private ReadOnly _config As SimulationConfig
        Private _countries As List(Of Country)
        Private _companies As List(Of Company)
        Private _chains As List(Of IndustryChain)
        Private _eventPropagator As Events.EventPropagator
        Private _macroEngine As MacroEngine

        Public Sub New(rng As SeededRandom, config As SimulationConfig)
            _rng = rng
            _config = config
        End Sub

        Public Sub SetWorld(
            countries As List(Of Country),
            companies As List(Of Company),
            chains As List(Of IndustryChain),
            eventPropagator As Events.EventPropagator,
            macroEngine As MacroEngine
        )
            _countries = countries
            _companies = companies
            _chains = chains
            _eventPropagator = eventPropagator
            _macroEngine = macroEngine
        End Sub

        Public Sub [Step](day As Date)
            If _companies Is Nothing Then Return

            Dim dailyFactor As Double = 1.0 / _config.TradingDaysPerYear

            For Each comp In _companies
                Dim country As Country = If(comp.CountryId < _countries.Count, _countries(comp.CountryId), Nothing)
                Dim chain As IndustryChain = If(comp.IndustryChainId < _chains.Count, _chains(comp.IndustryChainId), Nothing)
                Dim eventImpact As Double = If(_eventPropagator IsNot Nothing, _eventPropagator.GetCompanyImpact(comp.CompanyId), 0.0)

                ' === 营收：受产品价格 × 销量 + 行业增长 + 事件 ===
                Dim industryGrowth As Double = If(chain IsNot Nothing, (chain.Demand / 100.0 - 1.0) * 0.1, 0.0)
                Dim priceEffect As Double = If(chain IsNot Nothing, (chain.ProductPrice / 100.0 - 1.0) * 0.2, 0.0)
                Dim revenueGrowth As Double = (industryGrowth + priceEffect + eventImpact * 0.3) * dailyFactor
                comp.Revenue *= (1.0 + revenueGrowth + _rng.NextGaussian(0, 0.002))
                comp.Revenue = Math.Max(1.0, comp.Revenue)

                ' === 成本：受供应链价格 + 关税 + 汇率 ===
                Dim costRatio As Double = 0.6
                If chain IsNot Nothing Then
                    costRatio = 0.5 + 0.2 * (chain.ProductPrice / chain.BasePrice - 1.0) * If(chain.Layer = ChainLayer.Upstream, -0.5, 0.5)
                End If
                costRatio = Math.Max(0.3, Math.Min(0.9, costRatio))
                ' 关税影响（若有贸易伙伴关系）
                If country IsNot Nothing AndAlso country.Tariffs.Count > 0
                    Dim tariffEffect As Double = 0
                    For Each t In country.Tariffs.Values
                        For Each v In t.Values
                            tariffEffect += v
                        Next
                    Next
                    tariffEffect /= (country.Tariffs.Count * 6)  ' 平均关税率
                    costRatio += tariffEffect * 0.05
                End If
                comp.CostOfGoodsSold = comp.Revenue * costRatio

                ' === 运营费用 ===
                comp.OperatingExpense = comp.Revenue * (0.1 + comp.RDLevel)

                ' === 净利润 ===
                Dim pretax As Double = comp.Revenue - comp.CostOfGoodsSold - comp.OperatingExpense
                Dim taxRate As Double = If(country IsNot Nothing, 0.25, 0.2)
                comp.NetProfit = pretax * (1.0 - taxRate) + eventImpact * comp.Revenue * 0.05
                comp.NetProfit = Math.Max(-comp.Revenue * 0.5, comp.NetProfit)

                ' === 现金流 ===
                comp.OperatingCashFlow = comp.NetProfit + comp.Revenue * 0.1
                comp.FreeCashFlow = comp.OperatingCashFlow * 0.6

                ' === 资产负债表 ===
                comp.TotalAssets *= (1.0 + comp.NetProfit / comp.TotalAssets * 0.5)
                comp.Equity += comp.NetProfit * dailyFactor * 252
                comp.TotalLiabilities = comp.TotalAssets - comp.Equity
                comp.CashAndEquivalents += comp.FreeCashFlow * dailyFactor * 252

                ' === 股权质押比例动态调整 ===
                comp.PledgeRatio += _rng.NextGaussian(0, 0.002)
                comp.PledgeRatio = Math.Max(0, Math.Min(0.5, comp.PledgeRatio))

                ' === 限售解禁处理 ===
                If comp.NextUnlockDate.HasValue AndAlso day >= comp.NextUnlockDate.Value Then
                    ' 解禁发生：FreeFloatRatio 增加，标记已解禁
                    comp.FreeFloatRatio = Math.Min(0.95, comp.FreeFloatRatio + comp.NextUnlockShares / comp.SharesOutstanding)
                    comp.NextUnlockDate = Nothing
                    comp.NextUnlockShares = 0
                End If

                ' === 动量与近期表现 ===
                comp.Momentum = comp.Momentum * 0.95 + eventImpact * 0.05

                ' === 累积事件冲击存档 ===
                comp.EventImpact = eventImpact
            Next
        End Sub

    End Class

End Namespace
