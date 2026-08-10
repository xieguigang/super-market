Option Strict On
Option Explicit On

Imports SuperMarket.Core
Imports SuperMarket.Entities
Imports SuperMarket.Simulation

Namespace SuperMarket.Events

    ''' <summary>
    ''' 事件传播器：在蝴蝶效应网络上传播事件冲击，将结果应用到实体。
    ''' 维护当前时间步所有事件对公司累积冲击的汇总。
    ''' </summary>
    Public Class EventPropagator

        Private ReadOnly _network As ButterflyEffectNetwork
        Private ReadOnly _config As SimulationConfig
        Private _companiesByChainId As Dictionary(Of Integer, List(Of Integer))
        Private _companiesByCountryId As Dictionary(Of Integer, List(Of Integer))

        ' 当前时间步公司累积冲击：CompanyId → 累积冲击值
        Public Property CurrentCompanyImpacts As New Dictionary(Of Integer, Double)()

        ' 当前时间步国家累积冲击：CountryId → 累积冲击值
        Public Property CurrentCountryImpacts As New Dictionary(Of Integer, Double)()

        ' 当前时间步所有已处理事件（含传播路径）
        Public Property ProcessedEvents As New List(Of MarketEvent)()

        Public Sub New(network As ButterflyEffectNetwork, config As SimulationConfig)
            _network = network
            _config = config
        End Sub

        ''' <summary>设置公司查找索引（产业链ID→公司列表，国家ID→公司列表）。</summary>
        Public Sub SetIndices(
            companiesByChainId As Dictionary(Of Integer, List(Of Integer)),
            companiesByCountryId As Dictionary(Of Integer, List(Of Integer))
        )
            _companiesByChainId = companiesByChainId
            _companiesByCountryId = companiesByCountryId
        End Sub

        ''' <summary>每个时间步开始时清空累积冲击。</summary>
        Public Sub ResetForNewStep()
            CurrentCompanyImpacts.Clear()
            CurrentCountryImpacts.Clear()
            ProcessedEvents.Clear()
        End Sub

        ''' <summary>
        ''' 传播单个事件，将冲击累积到公司和国家的当前冲击字典中。
        ''' </summary>
        Public Sub Propagate(ev As MarketEvent)
            Dim result = _network.Propagate(
                ev.SourceEntityType,
                ev.SourceEntityId,
                ev.InitialImpact,
                ev.ImpactDecay,
                ev.MaxPropagationDepth,
                ev.AffectedCategory,
                _companiesByChainId,
                _companiesByCountryId
            )

            ' 填充事件的传播路径与受影响实体
            ev.AffectedEntities = result.AffectedCompanies
            ev.PropagationPath = result.Path.Select(Function(p) (p.Item1, p.Item2, p.Item3)).ToList()

            ' 累积公司冲击
            For Each kv In result.AffectedCompanies
                If CurrentCompanyImpacts.ContainsKey(kv.Key) Then
                    CurrentCompanyImpacts(kv.Key) += kv.Value
                Else
                    CurrentCompanyImpacts(kv.Key) = kv.Value
                End If
            Next

            ' 累积国家冲击（从 allImpacts 中提取 Country 类型）
            For Each kv In result.AllImpacts
                If kv.Key.Item1 = EntityType.Country Then
                    If CurrentCountryImpacts.ContainsKey(kv.Key.Item2) Then
                        CurrentCountryImpacts(kv.Key.Item2) += kv.Value
                    Else
                        CurrentCountryImpacts(kv.Key.Item2) = kv.Value
                    End If
                End If
            Next

            ProcessedEvents.Add(ev)
        End Sub

        ''' <summary>获取某公司当前时间步的累积冲击值。</summary>
        Public Function GetCompanyImpact(companyId As Integer) As Double
            If CurrentCompanyImpacts.ContainsKey(companyId) Then
                Return Math.Max(-1.0, Math.Min(1.0, CurrentCompanyImpacts(companyId)))
            End If
            Return 0.0
        End Function

        ''' <summary>获取某国家当前时间步的累积冲击值。</summary>
        Public Function GetCountryImpact(countryId As Integer) As Double
            If CurrentCountryImpacts.ContainsKey(countryId) Then
                Return Math.Max(-1.0, Math.Min(1.0, CurrentCountryImpacts(countryId)))
            End If
            Return 0.0
        End Function

    End Class

End Namespace
