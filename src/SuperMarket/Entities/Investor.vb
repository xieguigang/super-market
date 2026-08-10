Option Strict On
Option Explicit On

Namespace Entities

    ''' <summary>
    ''' 投资者：在市场中交易股票，行为模式受类型与情绪驱动。
    ''' </summary>
    Public Class Investor

        Public Property InvestorId As Integer
        Public Property Name As String
        Public Property Type As Core.InvestorType

        ''' <summary>风险偏好 [0, 1]。</summary>
        Public Property RiskAppetite As Double = 0.5

        ''' <summary>平均持仓周期（天）。</summary>
        Public Property HoldingPeriod As Integer = 30

        ''' <summary>追高倾向 [0, 1]。</summary>
        Public Property ChaseTendency As Double = 0.3

        ''' <summary>做空倾向 [0, 1]。</summary>
        Public Property ShortTendency As Double = 0.1

        ''' <summary>资金规模（标准化）。</summary>
        Public Property CapitalSize As Double = 1000000.0

        ''' <summary>所属国家 ID（巨头的国籍，影响其偏好板块）。</summary>
        Public Property CountryId As Integer = 0

        ''' <summary>当前持仓：股票 ID → 持仓金额。</summary>
        Public Property Holdings As New Dictionary(Of Integer, Double)()

        Public Overrides Function ToString() As String
            Return $"Investor#{InvestorId} {Name} [{Type}] capital={CapitalSize:F0}"
        End Function

    End Class

End Namespace
