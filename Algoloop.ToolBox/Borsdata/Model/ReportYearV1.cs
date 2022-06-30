

using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Borsdata.Api.Dal.Model
{
    public partial class ReportYearV1
    {
        [JsonProperty(PropertyName = "year")]
        public int Year { get; set; }

        [JsonProperty(PropertyName = "period")]
        public int Period { get; set; }

        [JsonProperty(PropertyName = "revenues")]  
        public double? Revenues { get; set; }

        [JsonProperty(PropertyName = "gross_Income")] 
        public double? GrossIncome { get; set; }

        [JsonProperty(PropertyName = "operating_Income")] 
        public double? OperatingIncome { get; set; }

        [JsonProperty(PropertyName = "profit_Before_Tax")] 
        public double? ProfitBeforeTax { get; set; }

        [JsonProperty(PropertyName = "profit_To_Equity_Holders")] 
        public double? ProfitToEquityHolders { get; set; }

        [JsonProperty(PropertyName = "earnings_Per_Share")]
        public double? EarningsPerShare { get; set; }

        [JsonProperty(PropertyName = "number_Of_Shares")]
        public double? NumberOfShares { get; set; }

        [JsonProperty(PropertyName = "dividend")]
        public double? Dividend { get; set; }

        [JsonProperty(PropertyName = "intangible_Assets")]
        public double? IntangibleAssets { get; set; }

        [JsonProperty(PropertyName = "tangible_Assets")]
        public double? TangibleAssets { get; set; }

        [JsonProperty(PropertyName = "financial_Assets")]
        public double? FinancialAssets { get; set; }

        [JsonProperty(PropertyName = "non_Current_Assets")]
        public double? NonCurrentAssets { get; set; }

        [JsonProperty(PropertyName = "cash_And_Equivalents")]
        public double? CashAndEquivalents { get; set; }

        [JsonProperty(PropertyName = "current_Assets")]
        public double? CurrentAssets { get; set; }

        [JsonProperty(PropertyName = "total_Assets")]
        public double? TotalAssets { get; set; }

        [JsonProperty(PropertyName = "total_Equity")]
        public double? TotalEquity { get; set; }

        [JsonProperty(PropertyName = "non_Current_Liabilities")]
        public double? NonCurrentLiabilities { get; set; }

        [JsonProperty(PropertyName = "current_Liabilities")]
        public double? CurrentLiabilities { get; set; }

        [JsonProperty(PropertyName = "total_Liabilities_And_Equity")]
        public double? TotalLiabilitiesAndEquity { get; set; }

        [JsonProperty(PropertyName = "net_Debt")]
        public double? NetDebt { get; set; }

        [JsonProperty(PropertyName = "cash_Flow_From_Operating_Activities")]
        public double? CashFlowFromOperatingActivities { get; set; }

        [JsonProperty(PropertyName = "cash_Flow_From_Investing_Activities")]
        public double? CashFlowFromInvestingActivities { get; set; }

        [JsonProperty(PropertyName = "cash_Flow_From_Financing_Activities")]
        public double? CashFlowFromFinancingActivities { get; set; }

        [JsonProperty(PropertyName = "cash_Flow_For_The_Year")]
        public double? CashFlowForTheYear { get; set; }

        [JsonProperty(PropertyName = "free_Cash_Flow")]
        public double? FreeCashFlow { get; set; }

        [JsonProperty(PropertyName = "stock_Price_Average")]
        public double? StockPriceAverage { get; set; }

        [JsonProperty(PropertyName = "stock_Price_High")]
        public double? StockPriceHigh { get; set; }

        [JsonProperty(PropertyName = "stock_Price_Low")]
        public double? StockPriceLow { get; set; }

        [JsonProperty(PropertyName = "currency")]
         public string Currency { get; set; } 

        [JsonProperty(PropertyName = "report_Start_Date")]
        public DateTime? ReportStartDate { get; set; }

        [JsonProperty(PropertyName = "report_End_Date")]
        public DateTime? ReportEndDate { get; set; }

        [JsonProperty(PropertyName = "broken_Fiscal_Year")]
        public bool Broken_Fiscal_Year { get; set; }

        [JsonProperty(PropertyName = "report_Date")]
        public DateTime? ReportDate { get; set; }
    }
}
