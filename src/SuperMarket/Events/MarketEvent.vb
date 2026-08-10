Option Strict On
Option Explicit On

Imports SuperMarket.Core

Namespace SuperMarket.Events

    ''' <summary>
    ''' 市场事件基类。事件是蝴蝶效应网络中的冲击源，沿关系图传播影响。
    ''' </summary>
    Public Class MarketEvent

        Public Property EventId As Integer
        Public Property Timestamp As Date
        Public Property EventType As EventType = EventType.None
        Public Property SourceEntityType As EntityType = EntityType.Country
        Public Property SourceEntityId As Integer
        Public Property Description As String = ""

        ''' <summary>初始冲击值 [-1, 1]，正值利好，负值利空。</summary>
        Public Property InitialImpact As Double = 0.0

        ''' <summary>每跳衰减系数。</summary>
        Public Property ImpactDecay As Double = 0.6

        ''' <summary>最大传播深度（跳数）。</summary>
        Public Property MaxPropagationDepth As Integer = 3

        ''' <summary>影响的产品类别（Nothing 表示全类别）。</summary>
        Public Property AffectedCategory As ProductCategory? = Nothing

        ''' <summary>传播路径记录：(实体类型, 实体ID, 累积冲击值)。</summary>
        Public Property PropagationPath As New List(Of (EntityType As EntityType, EntityId As Integer, Impact As Double))()

        ''' <summary>受影响实体及其最终冲击值（传播后填充）。</summary>
        Public Property AffectedEntities As New Dictionary(Of Integer, Double)()

        Public Overrides Function ToString() As String
            Return $"#{EventId} [{EventType}] src={SourceEntityType}#{SourceEntityId} impact={InitialImpact:F3} {Description}"
        End Function

    End Class

    ''' <summary>
    ''' 事件工厂：根据 EventType 创建带预设参数的事件。
    ''' 不同事件类型有不同的初始冲击范围、衰减系数、传播深度。
    ''' </summary>
    Public Module EventFactory

        ''' <summary>
        ''' 创建指定类型的事件，预设合理参数。
        ''' </summary>
        Public Function Create(
            eventId As Integer,
            timestamp As Date,
            type As EventType,
            sourceType As EntityType,
            sourceId As Integer,
            rng As SeededRandom,
            config As SimulationConfig
        ) As MarketEvent
            Dim ev As New MarketEvent() With {
                .EventId = eventId,
                .Timestamp = timestamp,
                .EventType = type,
                .SourceEntityType = sourceType,
                .SourceEntityId = sourceId,
                .ImpactDecay = config.EventImpactDecay,
                .MaxPropagationDepth = config.EventMaxDepth
            }

            Select Case type
                Case EventType.TariffChange
                    ev.InitialImpact = rng.NextGaussian(-0.15, 0.2)
                    ev.Description = "关税政策调整"
                    ev.MaxPropagationDepth = Math.Max(3, config.EventMaxDepth)

                Case EventType.PolicyChange
                    ev.InitialImpact = rng.NextGaussian(0.0, 0.15)
                    ev.Description = "市场监管政策变化"

                Case EventType.RDBreakthrough
                    ev.InitialImpact = rng.NextDouble(0.15, 0.5)
                    ev.Description = "研发突破，新产品上市"
                    ev.MaxPropagationDepth = 2

                Case EventType.InvestorChase
                    ev.InitialImpact = rng.NextDouble(0.05, 0.2)
                    ev.Description = "投资者追高买入"
                    ev.MaxPropagationDepth = 1

                Case EventType.WhaleShort
                    ev.InitialImpact = rng.NextDouble(-0.4, -0.1)
                    ev.Description = "巨头做空打压"
                    ev.MaxPropagationDepth = 2

                Case EventType.WarFriction
                    ev.InitialImpact = rng.NextDouble(-0.6, -0.2)
                    ev.Description = "国家间战争摩擦升级"
                    ev.MaxPropagationDepth = config.EventMaxDepth + 1

                Case EventType.CurrencyDevaluation
                    ev.InitialImpact = rng.NextDouble(-0.3, -0.1)
                    ev.Description = "货币大幅贬值"
                    ev.AffectedCategory = ProductCategory.IndustrialRawMaterial

                Case EventType.InterestRateChange
                    ev.InitialImpact = rng.NextGaussian(-0.1, 0.15)
                    ev.Description = "央行调整基准利率"

                Case EventType.SupplyChainDisruption
                    ev.InitialImpact = rng.NextDouble(-0.4, -0.1)
                    ev.Description = "供应链中断"
                    ev.MaxPropagationDepth = config.EventMaxDepth + 1

                Case EventType.MarketCrash
                    ev.InitialImpact = rng.NextDouble(-0.8, -0.4)
                    ev.Description = "市场崩盘"
                    ev.MaxPropagationDepth = config.EventMaxDepth + 2

                Case EventType.SectorRotation
                    ev.InitialImpact = rng.NextGaussian(0.0, 0.1)
                    ev.Description = "板块轮动"

                Case EventType.EarningsSurprise
                    ev.InitialImpact = rng.NextGaussian(0.0, 0.2)
                    ev.Description = "财报超预期"
                    ev.MaxPropagationDepth = 1

                Case EventType.InsiderTrading
                    ev.InitialImpact = rng.NextGaussian(0.0, 0.1)
                    ev.Description = "高管增减持"
                    ev.MaxPropagationDepth = 1

                Case EventType.PledgeUnlock
                    ev.InitialImpact = rng.NextDouble(-0.15, -0.05)
                    ev.Description = "限售股解禁"
                    ev.MaxPropagationDepth = 1

                Case EventType.CommodityShock
                    ev.InitialImpact = rng.NextGaussian(0.0, 0.25)
                    ev.Description = "大宗商品价格冲击"
                    ev.AffectedCategory = ProductCategory.Energy

                Case EventType.Pandemic
                    ev.InitialImpact = rng.NextDouble(-0.7, -0.3)
                    ev.Description = "黑天鹅事件/流行病"
                    ev.MaxPropagationDepth = config.EventMaxDepth + 2

                Case Else
                    ev.InitialImpact = rng.NextGaussian(0.0, 0.1)
                    ev.Description = "未知事件"
            End Select

            ' 限制冲击值范围 [-1, 1]
            ev.InitialImpact = Math.Max(-1.0, Math.Min(1.0, ev.InitialImpact))

            Return ev
        End Function

    End Module

End Namespace
