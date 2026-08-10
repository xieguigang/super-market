Option Strict On
Option Explicit On

Namespace Entities

    ''' <summary>
    ''' 产业链节点：表示某一国家某一产品类别的产业链环节（上游/中游/下游）。
    ''' </summary>
    Public Class IndustryChain

        Public Property ChainId As Integer
        Public Property Name As String
        Public Property CountryId As Integer
        Public Property Category As Core.ProductCategory
        Public Property Layer As Core.ChainLayer

        ''' <summary>产能上限（标准化单位）。</summary>
        Public Property ProductionCapacity As Double = 1000.0

        ''' <summary>当前产能利用率 [0, 1]。</summary>
        Public Property UtilizationRate As Double = 0.75

        ''' <summary>当前库存量。</summary>
        Public Property Inventory As Double = 200.0

        ''' <summary>本环节产品当前价格。</summary>
        Public Property ProductPrice As Double = 100.0

        ''' <summary>市场需求指数。</summary>
        Public Property Demand As Double = 100.0

        ''' <summary>市场供给指数。</summary>
        Public Property Supply As Double = 100.0

        ''' <summary>本环节所属公司 ID 列表。</summary>
        Public Property CompanyIds As New List(Of Integer)()

        ''' <summary>上游产业链节点 ID 列表。</summary>
        Public Property UpstreamChainIds As New List(Of Integer)()

        ''' <summary>下游产业链节点 ID 列表。</summary>
        Public Property DownstreamChainIds As New List(Of Integer)()

        Public Overrides Function ToString() As String
            Return $"Chain#{ChainId} {Name} [{Layer}] cat={Category}"
        End Function

    End Class

End Namespace
