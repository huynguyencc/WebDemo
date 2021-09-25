using System;
using System.Collections.Generic;
using System.Text;

namespace PowerBIAdmin.Service.DataTable
{
    public class SalesInvoiceLine
    {
        public string StoreID { get; set; }
        public string SalesInvoiceHeadNo { get; set; }
        public int LineNo { get; set; }
        public int Type { get; set; }
        public string No { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountInclVAT { get; set; }
        public decimal DiscountAmount { get; set; }
        public string CampaignCode { get; set; }
        public string CashRegSettlementNo { get; set; }
        public string SerialNo { get; set; }
        public string VariantCode { get; set; }
        public string ServiceNo { get; set; }
        public decimal VATPercent { get; set; }
        public string ExternalOrderID { get; set; }
        public string ShipmentPosID { get; set; }
        public string ShipmentNo { get; set; }
        public int ShipmentLineNo { get; set; }
        public string TransactionNo { get; set; }
        public string SerialNo2 { get; set; }
        public string AppliesToDocNo { get; set; }
        public decimal DiscountPercent { get; set; }
        public bool IsUndo { get; set; }
        public string ReferenceDocumentNo { get; set; }
        public decimal OriginalUnitPrice { get; set; }
        public string Comment { get; set; }
        public int SortOrder { get; set; }
        public string UnitCode { get; set; }
        public int TimeSheetId { get; set; }
        public int GLStatus { get; set; }
        public string SalesPersonNo { get; set; }
        public int FixedPriceLineNo { get; set; }
        public string Tag { get; set; }
        public int ItemType { get; set; }
        public decimal CalculatedPrice { get; set; }
        public decimal CurrencyAmount { get; set; }
    }
}
