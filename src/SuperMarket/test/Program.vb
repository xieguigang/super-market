Option Strict On
Option Explicit On

Imports SuperMarket
Imports SuperMarket.Core

Namespace SuperMarket

    ''' <summary>
    ''' CLI 入口点：解析命令行参数，创建配置，启动模拟。
    ''' </summary>
    Public Module Program

        Public Function Main(args As String()) As Integer
            Try
                Dim config = SimulationConfig.FromArgs(args)
                If config Is Nothing Then
                    Return 0  ' --help 已输出
                End If

                Dim simulator As New MarketSimulator(config)
                simulator.Run()

                Return 0
            Catch ex As Exception
                Console.Error.WriteLine($"错误: {ex.Message}")
                Console.Error.WriteLine(ex.StackTrace)
                Return 1
            End Try
        End Function

    End Module

End Namespace
