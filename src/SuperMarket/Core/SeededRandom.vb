Option Strict On
Option Explicit On

Imports System.Threading

Namespace Core

    ''' <summary>
    ''' 种子化随机数辅助类。封装 System.Random，提供高斯采样与多种辅助方法。
    ''' 同一种子保证完全可复现的模拟序列。
    ''' </summary>
    Public Class SeededRandom

        Private ReadOnly _rng As Random

        Public ReadOnly Property Seed As Integer

        Public Sub New(seed As Integer)
            _Seed = seed
            _rng = New Random(seed)
        End Sub

        ''' <summary>[0, 1) 双精度浮点。</summary>
        Public Function NextDouble() As Double
            Return _rng.NextDouble()
        End Function

        ''' <summary>[min, max) 双精度浮点。</summary>
        Public Function NextDouble(min As Double, max As Double) As Double
            Return min + (max - min) * _rng.NextDouble()
        End Function

        ''' <summary>[min, max] 整数闭区间。</summary>
        Public Function [Next](min As Integer, max As Integer) As Integer
            Return _rng.[Next](min, max + 1)
        End Function

        ''' <summary>非负整数。</summary>
        Public Function [Next](max As Integer) As Integer
            Return _rng.[Next](max)
        End Function

        ''' <summary>布尔值，p 为 true 的概率。</summary>
        Public Function NextBoolean(p As Double) As Boolean
            Return _rng.NextDouble() < p
        End Function

        ''' <summary>
        ''' Box-Muller 正态分布采样，均值 mean，标准差 stdDev。
        ''' </summary>
        Public Function NextGaussian(mean As Double, stdDev As Double) As Double
            Dim u1 As Double = _rng.NextDouble()
            Dim u2 As Double = _rng.NextDouble()
            ' 避免 log(0)
            If u1 < Double.Epsilon Then u1 = Double.Epsilon
            Dim r As Double = Math.Sqrt(-2.0 * Math.Log(u1))
            Dim theta As Double = 2.0 * Math.PI * u2
            Dim z0 As Double = r * Math.Cos(theta)
            Return mean + stdDev * z0
        End Function

        ''' <summary>从给定序列中按均匀分布选取一个元素。</summary>
        Public Function Choice(Of T)(items As IReadOnlyList(Of T)) As T
            If items Is Nothing OrElse items.Count = 0 Then
                Throw New ArgumentException("items 不能为空", NameOf(items))
            End If
            Return items(_rng.Next(items.Count))
        End Function

        ''' <summary>按权重数组选取索引（轮盘赌）。</summary>
        Public Function WeightedIndex(weights As IReadOnlyList(Of Double)) As Integer
            If weights Is Nothing OrElse weights.Count = 0 Then
                Throw New ArgumentException("weights 不能为空", NameOf(weights))
            End If
            Dim total As Double = 0.0
            For i = 0 To weights.Count - 1
                If weights(i) < 0 Then
                    Throw New ArgumentException($"权重不能为负: index={i}")
                End If
                total += weights(i)
            Next
            If total <= 0 Then
                Return _rng.Next(weights.Count)
            End If
            Dim r As Double = _rng.NextDouble() * total
            Dim cumulative As Double = 0.0
            For i = 0 To weights.Count - 1
                cumulative += weights(i)
                If r <= cumulative Then Return i
            Next
            Return weights.Count - 1
        End Function

        ''' <summary>打乱列表（Fisher-Yates，原地）。</summary>
        Public Sub Shuffle(Of T)(items As IList(Of T))
            If items Is Nothing Then Return
            For i = items.Count - 1 To 1 Step -1
                Dim j As Integer = _rng.Next(i + 1)
                Dim tmp = items(i)
                items(i) = items(j)
                items(j) = tmp
            Next
        End Sub

    End Class

End Namespace
