Option Strict On
Option Explicit On

Namespace Entities

    ''' <summary>
    ''' 股票：量价数据载体，由多因子定价引擎驱动。
    ''' </summary>
    Public Class Stock

        Public Property StockId As Integer
        Public Property Ticker As String
        Public Property CompanyId As Integer
        Public Property CountryId As Integer

        ' === 前一日收盘价 ===
        Public Property PreviousClose As Double = 50.0

        ' === 本日 OHLC ===
        Public Property [Open] As Double = 50.0
        Public Property High As Double = 50.0
        Public Property Low As Double = 50.0
        Public Property Close As Double = 50.0

        ' === VWAP ===
        Public Property VWAP As Double = 50.0

        ' === 成交量与换手 ===
        Public Property Volume As Double = 1000.0        ' 成交股数（万股）
        Public Property Amount As Double = 50000.0       ' 成交金额（万元）
        Public Property TurnoverRate As Double = 0.02    ' 换手率

        ' === 市值与估值 ===
        Public Property MarketCap As Double = 5000.0     ' 总市值
        Public Property PE As Double = 20.0
        Public Property PB As Double = 2.0
        Public Property PS As Double = 1.5
        Public Property DividendYield As Double = 0.02

        ' === 风险特征 ===
        Public Property Beta As Double = 1.0
        Public Property Volatility As Double = 0.25       ' 年化波动率
        Public Property DailyVolatility As Double = 0.015 ' 日波动率

        ' === 内在价值与合理价 ===
        Public Property FairValue As Double = 50.0        ' 基本面估算的合理价
        Public Property Mispricing As Double = 0.0        ' 错价程度 = Close/FairValue - 1

        ''' <summary>重置当日 OHLC 为前收，准备新的一天。</summary>
        Public Sub ResetForNewDay()
            PreviousClose = Close
            [Open] = Close
            High = Close
            Low = Close
            VWAP = Close
            Volume = 0
            Amount = 0
            TurnoverRate = 0
        End Sub

        Public Overrides Function ToString() As String
            Return $"Stock#{StockId} {Ticker} close={Close:F2} vol={Volatility:F3}"
        End Function

    End Class

End Namespace
