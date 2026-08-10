Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text
Imports System.Text.Json

Namespace SuperMarket.Output

    ''' <summary>
    ''' JSON 写入器：使用 System.Text.Json 写入结构化 JSON 文件。
    ''' 支持世界拓扑、产业链结构、事件流、关系图。
    ''' </summary>
    Public Class JsonWriter

        ''' <summary>将任意对象序列化为 JSON 文件（缩进格式）。</summary>
        Public Shared Sub WriteToFile(filePath As String, data As Object)
            Dim dir = Path.GetDirectoryName(filePath)
            If Not String.IsNullOrEmpty(dir) AndAlso Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            Dim options As New JsonSerializerOptions() With {
                .WriteIndented = True,
                .Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }

            Dim json As String = JsonSerializer.Serialize(data, options)
            File.WriteAllText(filePath, json, Encoding.UTF8)
        End Sub

        ''' <summary>将字符串列表写入 JSON 数组文件。</summary>
        Public Shared Sub WriteStringList(filePath As String, items As IEnumerable(Of String))
            Dim sb As New StringBuilder()
            sb.AppendLine("[")
            Dim list = items.ToList()
            For i = 0 To list.Count - 1
                sb.Append("  ")
                sb.Append(JsonSerializer.Serialize(list(i)))
                If i < list.Count - 1 Then sb.Append(",")
                sb.AppendLine()
            Next
            sb.AppendLine("]")
            Dim dir = Path.GetDirectoryName(filePath)
            If Not String.IsNullOrEmpty(dir) AndAlso Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8)
        End Sub

    End Class

End Namespace
