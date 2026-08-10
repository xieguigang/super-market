Option Strict On
Option Explicit On

Namespace SuperMarket.Output

    ''' <summary>
    ''' 输出模式定义：定义每个 CSV 文件的列头和文件路径规则，以及 JSON 文件结构。
    ''' </summary>
    Public Module OutputSchema

        ' === 量价数据 CSV ===
        Public ReadOnly PriceColumns As String() = {
            "Date", "Open", "High", "Low", "Close", "VWAP",
            "Volume", "Amount", "TurnoverRate", "MarketCap", "PE", "PB", "PS",
            "DividendYield", "Beta", "Volatility", "FairValue", "Mispricing"
        }

        ' === 基本面数据 CSV ===
        Public ReadOnly FundamentalsColumns As String() = {
            "Date", "Revenue", "CostOfGoodsSold", "OperatingExpense", "NetProfit",
            "TotalAssets", "TotalLiabilities", "Equity", "CashAndEquivalents",
            "OperatingCashFlow", "FreeCashFlow", "SharesOutstanding", "FreeFloatRatio",
            "InsiderHoldingRatio", "PledgeRatio", "RDLevel", "Competitiveness",
            "GrossMargin", "NetMargin", "ROE", "ROA"
        }

        ' === 宏观经济 CSV ===
        Public ReadOnly MacroColumns As String() = {
            "Date", "GDP", "GDPGrowth", "CPI", "PPI", "PMI", "NonFarmPayroll",
            "InterestRate", "InterbankRate", "TreasuryYield2Y", "TreasuryYield10Y",
            "M1", "M2", "ExchangeRate", "PoliticalStability", "MarginBalance", "NorthFlow"
        }

        ' === 供应链数据 CSV ===
        Public ReadOnly SupplyChainColumns As String() = {
            "Date", "OrderVolume", "SupplierOrderValue", "InventoryTurnover", "CustomerDemandIndex"
        }

        ' === 高管/质押 CSV ===
        Public ReadOnly InsiderColumns As String() = {
            "Date", "Action", "BuyAmount", "SellAmount", "PledgeRatio", "UnlockDate", "UnlockShares"
        }

        ' === 期权数据 CSV ===
        Public ReadOnly OptionsColumns As String() = {
            "Date", "ImpliedVolatility", "PutCallRatio", "OptionVolume"
        }

        ' === 期货数据 CSV ===
        Public ReadOnly FuturesColumns As String() = {
            "Date", "FuturesPrice", "SpotPrice", "Basis", "BasisPct"
        }

        ' === 大宗商品 CSV ===
        Public ReadOnly CommodityColumns As String() = {
            "Date", "Price", "Volume", "ChangePct"
        }

        ' === 汇率数据 CSV ===
        Public ReadOnly FxColumns As String() = {
            "Date", "Rate", "ChangePct"
        }

        ' === 微观结构 CSV ===
        Public ReadOnly MicrostructureColumns As String() = {
            "Date", "OrderImbalance", "LargeOrderCount", "LargeOrderAmount",
            "BuyRatio", "SellRatio", "Slippage"
        }

        ' === 卫星图像代理 CSV ===
        Public ReadOnly SatelliteColumns As String() = {
            "Date", "VehicleCount", "RevenueProxy"
        }

        ' === 子目录名 ===
        Public Const DirPrice As String = "price"
        Public Const DirFundamentals As String = "fundamentals"
        Public Const DirMacro As String = "macro"
        Public Const DirAlternative As String = "alternative"
        Public Const DirDerivatives As String = "derivatives"
        Public Const DirMicrostructure As String = "microstructure"
        Public Const DirTopology As String = "topology"

        ' === JSON 文件名 ===
        Public Const FileWorldTopology As String = "world_topology.json"
        Public Const FileRelationships As String = "relationships.json"
        Public Const FileIndustryChains As String = "industry_chains.json"
        Public Const FileEventLog As String = "event_log.json"

    End Module

End Namespace
