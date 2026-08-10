Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text

Namespace SuperMarket.Output

    ''' <summary>
    ''' CSV 写入器：使用 StringBuilder 批量构建行，StreamWriter 写入。
    ''' 支持追加模式和新建模式，自动处理逗号转义。
    ''' </summary>
    Public Class CsvWriter

        Private ReadOnly _filePath As String
        Private ReadOnly _columns As String()
        Private _sb As New StringBuilder()
        Private _headerWritten As Boolean = False

        Public Sub New(filePath As String, columns As String())
            _filePath = filePath
            _columns = columns
        End Sub

        ''' <summary>确保目录存在并写入表头（若文件不存在）。</summary>
        Public Sub EnsureHeader()
            Dim dir = Path.GetDirectoryName(_filePath)
            If Not String.IsNullOrEmpty(dir) AndAlso Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            If Not File.Exists(_filePath) Then
                _headerWritten = False
            Else
                _headerWritten = True
            End If
        End Sub

        ''' <summary>添加一行数据。values 长度须与 columns 一致。</summary>
        Public Sub AddRow(values As String())
            If values Is Nothing Then Return
            For i = 0 To values.Length - 1
                If i > 0 Then _sb.Append(","c)
                _sb.Append(EscapeCsvField(If(values(i), "")))
            Next
            _sb.AppendLine()
        End Sub

        ''' <summary>添加一行数据（对象数组，自动转换为字符串）。</summary>
        Public Sub AddRow(values As Object())
            If values Is Nothing Then Return
            Dim strValues(values.Length - 1) As String
            For i = 0 To values.Length - 1
                strValues(i) = If(values(i), "").ToString()
            Next
            AddRow(strValues)
        End Sub

        ''' <summary>将缓冲区写入文件（追加模式）。</summary>
        Public Sub Flush()
            Using sw As New StreamWriter(_filePath, _headerWritten, Encoding.UTF8)
                If Not _headerWritten Then
                    sw.WriteLine(String.Join(","c, _columns))
                    _headerWritten = True
                End If
                sw.Write(_sb.ToString())
            End Using
            _sb.Clear()
        End Sub

        ''' <summary>CSV 字段转义：含逗号、引号、换行则用双引号包裹，内部引号翻倍。</summary>
        Private Function EscapeCsvField(field As String) As String
            If field Is Nothing Then Return ""
            If field.Contains(",") OrElse field.Contains("""") OrElse field.Contains(vbCr) OrElse field.Contains(vbLf) Then
                Return """" & field.Replace("""", """""") & """"
            End If
            Return field
        End Function

    End Class

End Namespace
