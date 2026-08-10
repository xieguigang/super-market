Option Strict On
Option Explicit On

Namespace Entities

    ''' <summary>
    ''' 国家实体：宏观经济属性、关税表、产业链列表、与他国关系。
    ''' </summary>
    Public Class Country

        Public Property CountryId As Integer
        Public Property Name As String
        Public Property CurrencyCode As String

        ''' <summary>货币符号。</summary>
        Public Property CurrencySymbol As String = "$"

        ''' <summary>对国际基准货币的汇率（每单位本币兑换基准货币）。</summary>
        Public Property ExchangeRate As Double = 1.0

        ''' <summary>上一交易日汇率（用于计算变化率）。</summary>
        Public Property PreviousExchangeRate As Double = 1.0

        ''' <summary>央行基准利率（年化%）。</summary>
        Public Property InterestRate As Double = 2.0

        ''' <summary>隔夜拆借利率（%）。</summary>
        Public Property InterbankRate As Double = 2.0

        ''' <summary>GDP 总量（标准化货币单位）。</summary>
        Public Property GDP As Double = 1000000.0

        ''' <summary>GDP 增速（年化%）。</summary>
        Public Property GDPGrowth As Double = 2.5

        ''' <summary>消费者价格指数同比涨幅（%）。</summary>
        Public Property CPI As Double = 2.0

        ''' <summary>生产者价格指数同比涨幅（%）。</summary>
        Public Property PPI As Double = 1.5

        ''' <summary>采购经理指数（%）。</summary>
        Public Property PMI As Double = 50.0

        ''' <summary>非农就业人数（万人）。</summary>
        Public Property NonFarmPayroll As Double = 10000.0

        ''' <summary>M1 货币供应量。</summary>
        Public Property M1 As Double = 500000.0

        ''' <summary>M2 货币供应量。</summary>
        Public Property M2 As Double = 1000000.0

        ''' <summary>10 年期国债收益率（%）。</summary>
        Public Property TreasuryYield10Y As Double = 2.5

        ''' <summary>2 年期国债收益率（%）。</summary>
        Public Property TreasuryYield2Y As Double = 1.5

        ''' <summary>融资融券余额（仅适用于有融资融券机制的市场）。</summary>
        Public Property MarginBalance As Double = 0.0

        ''' <summary>北向/南向资金净流入（仅适用于双向互联互通市场；其余国家为外资净流入）。</summary>
        Public Property NorthFlow As Double = 0.0

        ''' <summary>政治稳定性指数 [0, 1]，1=最稳定。</summary>
        Public Property PoliticalStability As Double = 0.7

        ''' <summary>关税表：Key=贸易伙伴国 ID，Value=该伙伴国各类产品关税率字典。</summary>
        Public Property Tariffs As New Dictionary(Of Integer, Dictionary(Of Core.ProductCategory, Double))()

        ''' <summary>持有的他国国债：Key=发行国 ID，Value=持有金额。</summary>
        Public Property TreasuryHoldings As New Dictionary(Of Integer, Double)()

        ''' <summary>本国产业链 ID 列表。</summary>
        Public Property IndustryChainIds As New List(Of Integer)()

        ''' <summary>本国上市公司 ID 列表。</summary>
        Public Property CompanyIds As New List(Of Integer)()

        ''' <summary>本日累积事件冲击值。</summary>
        Public Property EventImpact As Double = 0.0

        Public Overrides Function ToString() As String
            Return $"Country#{CountryId} {Name} ({CurrencyCode})"
        End Function

    End Class

End Namespace
