Option Strict On
Option Explicit On

Namespace Entities

    ''' <summary>
    ''' 产品：产业链中流转的标准化商品。全球供需决定其国际价格。
    ''' </summary>
    Public Class Product

        Public Property ProductId As Integer
        Public Property Name As String
        Public Property Category As Core.ProductCategory

        ''' <summary>基准价格（模拟初值）。</summary>
        Public Property BasePrice As Double = 100.0

        ''' <summary>当前价格（随供需波动）。</summary>
        Public Property CurrentPrice As Double = 100.0

        ''' <summary>需求弹性。</summary>
        Public Property DemandElasticity As Double = 0.5

        ''' <summary>供给弹性。</summary>
        Public Property SupplyElasticity As Double = 0.3

        ''' <summary>主要生产国 ID 列表。</summary>
        Public Property ProducerCountryIds As New List(Of Integer)()

        ''' <summary>全球总需求。</summary>
        Public Property GlobalDemand As Double = 10000.0

        ''' <summary>全球总供给。</summary>
        Public Property GlobalSupply As Double = 10000.0

        Public Overrides Function ToString() As String
            Return $"Product#{ProductId} {Name} cat={Category} price={CurrentPrice:F2}"
        End Function

    End Class

End Namespace
