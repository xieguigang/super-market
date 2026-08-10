Option Strict On
Option Explicit On

Namespace Entities

    ''' <summary>
    ''' 关系：蝴蝶效应网络中的有向加权边。
    ''' </summary>
    Public Class Relationship

        Public Property RelationId As Integer
        Public Property SourceType As Core.EntityType
        Public Property SourceId As Integer
        Public Property TargetType As Core.EntityType
        Public Property TargetId As Integer
        Public Property Type As Core.RelationType

        ''' <summary>关系强度权重 [0, 1]。</summary>
        Public Property Weight As Double = 0.5

        ''' <summary>人类可读描述。</summary>
        Public Property Description As String = ""

        Public Overrides Function ToString() As String
            Return $"{SourceType}#{SourceId} --[{Type},w={Weight:F2}]--> {TargetType}#{TargetId}"
        End Function

    End Class

End Namespace
