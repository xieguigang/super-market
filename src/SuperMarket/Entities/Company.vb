Option Strict On
Option Explicit On

Namespace Entities

    ''' <summary>
    ''' 上市公司实体：经营、财务、股权结构、研发能力。
    ''' </summary>
    Public Class Company

        Public Property CompanyId As Integer
        Public Property Name As String
        Public Property Ticker As String

        ''' <summary>所属国家 ID。</summary>
        Public Property CountryId As Integer

        ''' <summary>所属产业链 ID。</summary>
        Public Property IndustryChainId As Integer

        ''' <summary>主要产品类别。</summary>
        Public Property ProductCategory As Core.ProductCategory

        ' === 利润表 ===
        Public Property Revenue As Double = 1000.0
        Public Property CostOfGoodsSold As Double = 700.0
        Public Property OperatingExpense As Double = 100.0
        Public Property NetProfit As Double = 150.0

        ' === 资产负债表 ===
        Public Property TotalAssets As Double = 5000.0
        Public Property TotalLiabilities As Double = 2000.0
        Public Property Equity As Double = 3000.0
        Public Property CashAndEquivalents As Double = 500.0

        ' === 现金流量表 ===
        Public Property OperatingCashFlow As Double = 200.0
        Public Property FreeCashFlow As Double = 100.0

        ' === 股本结构 ===
        Public Property SharesOutstanding As Double = 100.0      ' 单位：百万股
        Public Property FreeFloatRatio As Double = 0.6           ' 流通比例
        Public Property InsiderHoldingRatio As Double = 0.25     ' 高管内部持股比例
        Public Property PledgeRatio As Double = 0.0              ' 股权质押比例

        ' === 限售股解禁 ===
        Public Property NextUnlockDate As Date? = Nothing
        Public Property NextUnlockShares As Double = 0.0

        ' === 研发与竞争力 ===
        Public Property RDLevel As Double = 0.3                  ' 研发投入比例
        Public Property Competitiveness As Double = 0.5          ' 竞争力 [0,1]
        Public Property BrandValue As Double = 0.0               ' 品牌价值

        ' === 产业链角色 ===
        Public Property ChainLayer As Core.ChainLayer = Core.ChainLayer.Midstream

        ' === 累积事件冲击 ===
        Public Property EventImpact As Double = 0.0

        ' === 动量与近期表现 ===
        Public Property RecentReturn As Double = 0.0              ' 近期收益率
        Public Property Momentum As Double = 0.0                 ' 动量因子

        Public ReadOnly Property GrossMargin As Double
            Get
                If Revenue = 0 Then Return 0
                Return (Revenue - CostOfGoodsSold) / Revenue
            End Get
        End Property

        Public ReadOnly Property NetMargin As Double
            Get
                If Revenue = 0 Then Return 0
                Return NetProfit / Revenue
            End Get
        End Property

        Public ReadOnly Property ROE As Double
            Get
                If Equity = 0 Then Return 0
                Return NetProfit / Equity
            End Get
        End Property

        Public ReadOnly Property ROA As Double
            Get
                If TotalAssets = 0 Then Return 0
                Return NetProfit / TotalAssets
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return $"Company#{CompanyId} {Ticker} {Name} cat={ProductCategory}"
        End Function

    End Class

End Namespace
