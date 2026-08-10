Option Strict On
Option Explicit On

Imports SuperMarket.Core
Imports SuperMarket.Entities

Namespace SuperMarket.Simulation

    ''' <summary>
    ''' 产业链引擎：每步更新各产业链产能利用率、库存、产品价格（供需均衡）、
    ''' 跨国贸易流（比较优势+关税+汇率）、供应链成本传导。
    ''' 输出产品价格供宏观和公司引擎使用。
    ''' </summary>
    Public Class IndustryChainEngine

        Private ReadOnly _rng As SeededRandom
        Private ReadOnly _config As SimulationConfig
        Private _countries As List(Of Country)
        Private _chains As List(Of IndustryChain)
        Private _products As List(Of Product)
        Private _eventPropagator As Events.EventPropagator

        ' 本步产品价格变化记录（供 CompanyEngine 使用）：ProductId → 价格变化率
        Public Property ProductPriceChanges As New Dictionary(Of Integer, Double)()

        Public Sub New(rng As SeededRandom, config As SimulationConfig)
            _rng = rng
            _config = config
        End Sub

        Public Sub SetWorld(
            countries As List(Of Country),
            chains As List(Of IndustryChain),
            products As List(Of Product),
            eventPropagator As Events.EventPropagator
        )
            _countries = countries
            _chains = chains
            _products = products
            _eventPropagator = eventPropagator
        End Sub

        Public Sub [Step](day As Date)
            ProductPriceChanges.Clear()
            If _chains Is Nothing Then Return

            For Each chain In _chains
                ' 事件冲击
                Dim chainImpact As Double = 0.0
                If _eventPropagator IsNot Nothing Then
                    ' 产业链本身的事件冲击（通过传播器累积的国家冲击间接传导）
                    Dim countryImpact = _eventPropagator.GetCountryImpact(chain.CountryId)
                    chainImpact = countryImpact * 0.6
                End If

                ' === 产能利用率：受需求驱动 + 随机 ===
                Dim demandFactor As Double = chain.Demand / 100.0
                chain.UtilizationRate += (demandFactor - chain.UtilizationRate) * 0.05
                chain.UtilizationRate += _rng.NextGaussian(0, 0.01)
                chain.UtilizationRate = Math.Max(0.2, Math.Min(0.98, chain.UtilizationRate))

                ' === 供给 = 产能 × 利用率 ===
                chain.Supply = chain.ProductionCapacity * chain.UtilizationRate

                ' === 需求：受全球情绪 + 事件 ===
                chain.Demand += _rng.NextGaussian(0, 2.0)
                chain.Demand *= (1.0 + chainImpact * 0.1)
                chain.Demand = Math.Max(10.0, chain.Demand)

                ' === 库存调整 ===
                Dim excess As Double = chain.Supply - chain.Demand
                chain.Inventory += excess * 0.1
                chain.Inventory = Math.Max(0, chain.Inventory)

                ' === 产品价格：供需均衡 ===
                Dim supplyDemandRatio As Double = If(chain.Demand > 0, chain.Supply / chain.Demand, 1.0)
                Dim priceChange As Double = (1.0 - supplyDemandRatio) * 0.05 + chainImpact * 0.1
                priceChange += _rng.NextGaussian(0, 0.01)
                chain.ProductPrice *= (1.0 + priceChange)
                chain.ProductPrice = Math.Max(1.0, chain.ProductPrice)

                ' === 供应链成本传导：上游涨价 → 下游成本上升 ===
                For Each upId In chain.UpstreamChainIds
                    If upId < _chains.Count Then
                        Dim upstream = _chains(upId)
                        ' 上游价格变化传导到下游（系数 0.3）
                        Dim upstreamPriceChange = upstream.ProductPrice / upstream.BasePrice - 1.0
                        chain.ProductPrice *= (1.0 + upstreamPriceChange * 0.3 * 0.05)
                    End If
                Next
            Next

            ' === 更新产品价格（产品与产业链一一对应） ===
            For i = 0 To _products.Count - 1
                If i < _chains.Count Then
                    Dim oldPrice = _products(i).CurrentPrice
                    _products(i).CurrentPrice = _chains(i).ProductPrice
                    If oldPrice > 0 Then
                        ProductPriceChanges(_products(i).ProductId) = (_products(i).CurrentPrice / oldPrice - 1.0)
                    End If
                End If
            Next
        End Sub

    End Class

End Namespace
