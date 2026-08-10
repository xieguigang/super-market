Option Strict On
Option Explicit On

Imports SuperMarket.Core
Imports SuperMarket.Entities

Namespace SuperMarket.Events

    ''' <summary>
    ''' 事件生成器：按配置的频率与概率分布，每时间步生成 0-N 个事件。
    ''' 不同事件类型有不同的触发条件（如战争摩擦仅在政治不稳定国家间触发）。
    ''' 支持季节性事件（如财报季）。
    ''' </summary>
    Public Class EventGenerator

        Private ReadOnly _rng As SeededRandom
        Private ReadOnly _config As SimulationConfig
        Private _eventSeq As Integer = 0

        ' 运行时引用
        Private _countries As List(Of Country)
        Private _companies As List(Of Company)
        Private _investors As List(Of Investor)
        Private _chains As List(Of IndustryChain)

        Public Sub New(rng As SeededRandom, config As SimulationConfig)
            _rng = rng
            _config = config
        End Sub

        Public Sub SetWorld(
            countries As List(Of Country),
            companies As List(Of Company),
            investors As List(Of Investor),
            chains As List(Of IndustryChain)
        )
            _countries = countries
            _companies = companies
            _investors = investors
            _chains = chains
        End Sub

        ''' <summary>
        ''' 为给定日期生成事件列表。使用 Poisson 过程模拟事件到达。
        ''' </summary>
        Public Function Generate(day As Date) As List(Of MarketEvent)
            Dim events As New List(Of MarketEvent)()

            ' Poisson 采样事件数量
            Dim lambda As Double = _config.EventFrequencyPerDay
            Dim numEvents As Integer = SamplePoisson(lambda)

            ' 财报季：每季度末月份（3/6/9/12）的月中前后增加财报事件
            Dim isEarningsSeason As Boolean = (day.Month Mod 3 = 0) AndAlso day.Day >= 1 AndAlso day.Day <= 15
            If isEarningsSeason Then
                numEvents += _rng.[Next](0, 3)
            End If

            If _countries Is Nothing OrElse _companies Is Nothing Then Return events

            Dim weights = _config.EventWeights
            Dim typeList As New List(Of EventType)(weights.Keys)
            Dim weightList As New List(Of Double)()
            For Each t In typeList
                weightList.Add(weights(t))
            Next

            For i = 0 To numEvents - 1
                Dim typeIdx = _rng.WeightedIndex(weightList)
                Dim etype = typeList(typeIdx)
                Dim ev = TryCreateEvent(day, etype)
                If ev IsNot Nothing Then events.Add(ev)
            Next

            Return events
        End Function

        Private Function TryCreateEvent(day As Date, etype As EventType) As MarketEvent
            ' 根据事件类型选择合适的源实体
            Dim sourceType As EntityType
            Dim sourceId As Integer

            Select Case etype
                Case EventType.TariffChange, EventType.PolicyChange, EventType.InterestRateChange,
                     EventType.CurrencyDevaluation, EventType.WarFriction
                    ' 国家层面事件
                    sourceType = EntityType.Country
                    sourceId = PickCountry(etype)

                Case EventType.RDBreakthrough, EventType.EarningsSurprise, EventType.InsiderTrading,
                     EventType.PledgeUnlock
                    ' 公司层面事件
                    sourceType = EntityType.Company
                    sourceId = PickCompany()

                Case EventType.InvestorChase, EventType.WhaleShort
                    ' 投资者行为事件
                    sourceType = EntityType.Investor
                    sourceId = PickInvestor(etype)

                Case EventType.SupplyChainDisruption, EventType.CommodityShock
                    ' 产业链层面事件
                    sourceType = EntityType.IndustryChain
                    sourceId = PickChain()

                Case EventType.MarketCrash, EventType.SectorRotation, EventType.Pandemic
                    ' 全球/市场层面事件：以最大经济体为源
                    sourceType = EntityType.Country
                    sourceId = If(_countries.Count > 0, _countries(0).CountryId, 0)

                Case Else
                    sourceType = EntityType.Country
                    sourceId = If(_countries.Count > 0, _countries(0).CountryId, 0)
            End Select

            If sourceId < 0 Then Return Nothing

            Dim ev = EventFactory.Create(_eventSeq, day, etype, sourceType, sourceId, _rng, _config)
            _eventSeq += 1

            ' 特殊处理：战争摩擦需要两个国家，描述补充
            If etype = EventType.WarFriction Then
                Dim target = PickCountry(etype, sourceId)
                If target >= 0 Then
                    ev.Description &= $"（{_countries(sourceId).Name} vs {_countries(target).Name}）"
                End If
            End If

            Return ev
        End Function

        Private Function PickCountry(etype As EventType, excludeId As Integer) As Integer
            If _countries Is Nothing OrElse _countries.Count = 0 Then Return -1
            Dim candidates = _countries.Where(Function(c) c.CountryId <> excludeId).ToList()
            If candidates.Count = 0 Then Return -1
            ' 战争摩擦优先选政治不稳定国家
            If etype = EventType.WarFriction Then
                candidates = candidates.Where(Function(c) c.PoliticalStability < 0.7).ToList()
                If candidates.Count = 0 Then candidates = _countries.Where(Function(c) c.CountryId <> excludeId).ToList()
            End If
            Return _rng.Choice(candidates).CountryId
        End Function

        Private Function PickCountry(etype As EventType) As Integer
            Return PickCountry(etype, -1)
        End Function

        Private Function PickCompany() As Integer
            If _companies Is Nothing OrElse _companies.Count = 0 Then Return -1
            Return _rng.Choice(_companies).CompanyId
        End Function

        Private Function PickInvestor(etype As EventType) As Integer
            If _investors Is Nothing OrElse _investors.Count = 0 Then Return -1
            ' 追高优先散户，做空优先巨头
            If etype = EventType.WhaleShort Then
                Dim whales = _investors.Where(Function(i) i.Type = InvestorType.Whale).ToList()
                If whales.Count > 0 Then Return _rng.Choice(whales).InvestorId
            ElseIf etype = EventType.InvestorChase Then
                Dim chasers = _investors.Where(Function(i) i.ChaseTendency > 0.4).ToList()
                If chasers.Count > 0 Then Return _rng.Choice(chasers).InvestorId
            End If
            Return _rng.Choice(_investors).InvestorId
        End Function

        Private Function PickChain() As Integer
            If _chains Is Nothing OrElse _chains.Count = 0 Then Return -1
            Return _rng.Choice(_chains).ChainId
        End Function

        ''' <summary>Poisson 采样（Knuth 算法）。</summary>
        Private Function SamplePoisson(lambda As Double) As Integer
            If lambda <= 0 Then Return 0
            Dim L As Double = Math.Exp(-lambda)
            Dim k As Integer = 0
            Dim p As Double = 1.0
            Do
                k += 1
                p *= _rng.NextDouble()
            Loop While p > L
            Return k - 1
        End Function

    End Class

End Namespace
