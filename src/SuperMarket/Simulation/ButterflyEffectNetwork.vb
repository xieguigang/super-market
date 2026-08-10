Option Strict On
Option Explicit On

Imports SuperMarket.Core
Imports SuperMarket.Entities

Namespace SuperMarket.Simulation

    ''' <summary>
    ''' 蝴蝶效应网络：有向加权图，节点为实体（国家/公司/产业链/投资者），
    ''' 边为关系（供应链/贸易/持股/竞争等）。支持 BFS 传播冲击值。
    ''' </summary>
    Public Class ButterflyEffectNetwork

        ' 邻接表：Key=(源实体类型, 源实体ID)，Value=出边列表(目标实体类型, 目标实体ID, 关系类型, 权重)
        Private ReadOnly _adjacency As New Dictionary(Of (EntityType, Integer), List(Of NetworkEdge))()

        ' 反向邻接表：Key=(目标实体类型, 目标实体ID)，Value=入边列表
        Private ReadOnly _reverseAdjacency As New Dictionary(Of (EntityType, Integer), List(Of NetworkEdge))()

        ''' <summary>所有节点集合。</summary>
        Public ReadOnly Property Nodes As New HashSet(Of (EntityType, Integer))()

        Public Sub AddEdge(rel As Relationship)
            Dim src As (EntityType, Integer) = (rel.SourceType, rel.SourceId)
            Dim dst As (EntityType, Integer) = (rel.TargetType, rel.TargetId)

            Dim edge As New NetworkEdge() With {
                .SourceType = rel.SourceType,
                .SourceId = rel.SourceId,
                .TargetType = rel.TargetType,
                .TargetId = rel.TargetId,
                .RelationType = rel.Type,
                .Weight = rel.Weight
            }

            If Not _adjacency.ContainsKey(src) Then
                _adjacency(src) = New List(Of NetworkEdge)()
            End If
            _adjacency(src).Add(edge)

            If Not _reverseAdjacency.ContainsKey(dst) Then
                _reverseAdjacency(dst) = New List(Of NetworkEdge)()
            End If
            _reverseAdjacency(dst).Add(edge)

            Nodes.Add(src)
            Nodes.Add(dst)
        End Sub

        ''' <summary>从关系列表批量构建网络。</summary>
        Public Sub BuildFromRelationships(relationships As List(Of Relationship))
            For Each rel In relationships
                AddEdge(rel)
            Next
        End Sub

        ''' <summary>获取某节点的所有出边。</summary>
        Public Function GetOutEdges(entityType As EntityType, entityId As Integer) As List(Of NetworkEdge)
            Dim key As (EntityType, Integer) = (entityType, entityId)
            If _adjacency.ContainsKey(key) Then
                Return _adjacency(key)
            End If
            Return New List(Of NetworkEdge)()
        End Function

        ''' <summary>获取某节点的所有入边。</summary>
        Public Function GetInEdges(entityType As EntityType, entityId As Integer) As List(Of NetworkEdge)
            Dim key As (EntityType, Integer) = (entityType, entityId)
            If _reverseAdjacency.ContainsKey(key) Then
                Return _reverseAdjacency(key)
            End If
            Return New List(Of NetworkEdge)()
        End Function

        ''' <summary>
        ''' BFS 传播冲击值。从源节点出发，沿出边传播，每跳 ×decay，最大 maxDepth 跳。
        ''' 返回受影响实体 ID→累积冲击值 的字典（仅 Company 类型，用于股价定价）。
        ''' 同时记录完整传播路径。
        ''' </summary>
        Public Function Propagate(
            sourceType As EntityType,
            sourceId As Integer,
            initialImpact As Double,
            decay As Double,
            maxDepth As Integer,
            affectedCategory As ProductCategory?,
            companiesByChainId As Dictionary(Of Integer, List(Of Integer)),
            companiesByCountryId As Dictionary(Of Integer, List(Of Integer))
        ) As (AffectedCompanies As Dictionary(Of Integer, Double), AllImpacts As Dictionary(Of (EntityType, Integer), Double), Path As List(Of (EntityType, Integer, Double)))
            Dim result As New Dictionary(Of Integer, Double)()
            Dim allImpacts As New Dictionary(Of (EntityType, Integer), Double)()
            Dim pathRecord As New List(Of (EntityType, Integer, Double))()

            ' 队列：(实体类型, 实体ID, 当前冲击值, 当前深度)
            Dim queue As New Queue(Of (EntityType, Integer, Double, Integer))()
            Dim visited As New HashSet(Of (EntityType, Integer))()

            queue.Enqueue((sourceType, sourceId, initialImpact, 0))
            visited.Add((sourceType, sourceId))
            allImpacts((sourceType, sourceId)) = initialImpact
            pathRecord.Add((sourceType, sourceId, initialImpact))

            While queue.Count > 0
                Dim current = queue.Dequeue()
                Dim curType = current.Item1
                Dim curId = current.Item2
                Dim curImpact = current.Item3
                Dim curDepth = current.Item4

                ' 若当前节点是公司，记录冲击
                If curType = EntityType.Company Then
                    If result.ContainsKey(curId) Then
                        result(curId) += curImpact
                    Else
                        result(curId) = curImpact
                    End If
                End If

                ' 若是产业链节点，将冲击传递给该产业链上的所有公司（额外直接传递）
                If curType = EntityType.IndustryChain Then
                    If companiesByChainId IsNot Nothing AndAlso companiesByChainId.ContainsKey(curId) Then
                        For Each compId In companiesByChainId(curId)
                            Dim propagatedImpact = curImpact * 0.8  ' 产业链→公司传递系数
                            If result.ContainsKey(compId) Then
                                result(compId) += propagatedImpact
                            Else
                                result(compId) = propagatedImpact
                            End If
                            ' 同时记入 allImpacts
                            Dim compKey As (EntityType, Integer) = (EntityType.Company, compId)
                            If allImpacts.ContainsKey(compKey) Then
                                allImpacts(compKey) += propagatedImpact
                            Else
                                allImpacts(compKey) = propagatedImpact
                            End If
                            pathRecord.Add((EntityType.Company, compId, propagatedImpact))
                        Next
                    End If
                End If

                ' 若是国家节点，将冲击传递给该国所有上市公司（若未限定类别或匹配）
                If curType = EntityType.Country Then
                    If companiesByCountryId IsNot Nothing AndAlso companiesByCountryId.ContainsKey(curId) Then
                        For Each compId In companiesByCountryId(curId)
                            ' 国家→公司的宏观冲击（较弱）
                            Dim propagatedImpact = curImpact * 0.5
                            If result.ContainsKey(compId) Then
                                result(compId) += propagatedImpact
                            Else
                                result(compId) = propagatedImpact
                            End If
                            Dim compKey As (EntityType, Integer) = (EntityType.Company, compId)
                            If allImpacts.ContainsKey(compKey) Then
                                allImpacts(compKey) += propagatedImpact
                            Else
                                allImpacts(compKey) = propagatedImpact
                            End If
                        Next
                    End If
                End If

                ' 达到最大深度，不再扩展
                If curDepth >= maxDepth Then Continue While

                ' 沿出边传播
                Dim edges = GetOutEdges(curType, curId)
                For Each edge In edges
                    Dim nextKey As (EntityType, Integer) = (edge.TargetType, edge.TargetId)
                    Dim nextImpact = curImpact * decay * edge.Weight

                    ' 冲击过小则剪枝
                    If Math.Abs(nextImpact) < 0.01 Then Continue For

                    If allImpacts.ContainsKey(nextKey) Then
                        allImpacts(nextKey) += nextImpact
                    Else
                        allImpacts(nextKey) = nextImpact
                    End If

                    pathRecord.Add((edge.TargetType, edge.TargetId, nextImpact))

                    ' 仅在未访问过时入队（避免循环）
                    If Not visited.Contains(nextKey) Then
                        visited.Add(nextKey)
                        queue.Enqueue((edge.TargetType, edge.TargetId, nextImpact, curDepth + 1))
                    End If
                Next
            End While

            ' 限制最终公司冲击值范围
            Dim clampedResult As New Dictionary(Of Integer, Double)()
            For Each kv In result
                clampedResult(kv.Key) = Math.Max(-1.0, Math.Min(1.0, kv.Value))
            Next

            Return (clampedResult, allImpacts, pathRecord)
        End Function

    End Class

    ''' <summary>网络边。</summary>
    Public Class NetworkEdge
        Public Property SourceType As EntityType
        Public Property SourceId As Integer
        Public Property TargetType As EntityType
        Public Property TargetId As Integer
        Public Property RelationType As RelationType
        Public Property Weight As Double
    End Class

End Namespace
