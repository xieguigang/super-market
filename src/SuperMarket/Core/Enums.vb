Option Strict On
Option Explicit On

Namespace SuperMarket.Core

    ''' <summary>
    ''' 产品大类。决定产业链分类与关税类目。
    ''' </summary>
    Public Enum ProductCategory
        IndustrialRawMaterial  ' 工业原材料
        Agricultural           ' 农产品
        InformationTechnology  ' 信息技术产品
        HighTech               ' 高科技产品
        ConsumerGoods          ' 消费品
        Energy                 ' 能源
    End Enum

    ''' <summary>
    ''' 产业链层级。
    ''' </summary>
    Public Enum ChainLayer
        Upstream      ' 上游：原材料/开采
        Midstream     ' 中游：制造/加工
        Downstream    ' 下游：消费/服务
    End Enum

    ''' <summary>
    ''' 事件类型。用于驱动蝴蝶效应网络。
    ''' </summary>
    Public Enum EventType
        None
        TariffChange              ' 关税变化
        PolicyChange              ' 市场监管政策变化
        RDBreakthrough            ' 公司产品研发成功
        InvestorChase             ' 投资者追高
        WhaleShort                ' 巨头做空
        WarFriction               ' 国家间战争摩擦
        CurrencyDevaluation       ' 货币贬值
        InterestRateChange        ' 银行利率变化
        SupplyChainDisruption     ' 供应链中断
        MarketCrash               ' 市场崩盘
        SectorRotation            ' 板块轮动
        EarningsSurprise          ' 财报超预期
        InsiderTrading            ' 高管增减持
        PledgeUnlock              ' 限售股解禁
        CommodityShock            ' 大宗商品冲击
        Pandemic                  ' 流行病/黑天鹅
    End Enum

    ''' <summary>
    ''' 投资者类型。
    ''' </summary>
    Public Enum InvestorType
        Retail          ' 散户
        Institutional   ' 机构
        Whale           ' 巨头
    End Enum

    ''' <summary>
    ''' 时间分辨率。
    ''' </summary>
    Public Enum Resolution
        Daily
        Minute
    End Enum

    ''' <summary>
    ''' 情绪等级（用于新闻/社交文本情绪打分）。
    ''' </summary>
    Public Enum SentimentLevel
        VeryNegative
        Negative
        Neutral
        Positive
        VeryPositive
    End Enum

    ''' <summary>
    ''' 实体类型（用于关系图节点标识）。
    ''' </summary>
    Public Enum EntityType
        Country
        Company
        IndustryChain
        Product
        Investor
    End Enum

    ''' <summary>
    ''' 关系类型（用于蝴蝶效应网络的边）。
    ''' </summary>
    Public Enum RelationType
        SupplyChain        ' 供应链上下游
        Trade              ' 贸易往来
        CrossHolding       ' 交叉持股
        TreasuryHolding    ' 国债持有
        Competition        ' 竞争关系
        Ownership          ' 所有权（公司归属国家）
        Portfolio          ' 投资组合（投资者持股票）
    End Enum

End Namespace
