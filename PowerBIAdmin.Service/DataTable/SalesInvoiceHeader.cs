using System;
using System.Collections.Generic;
using System.Text;

namespace PowerBIAdmin.Service.DataTable
{
    public class SalesInvoiceHeader
    {
        public string StoreID { get; set; }
        public string No { get; set; }
        public string CustomerNo { get; set; }
        public DateTime OrderDate { get; set; }
        public string SalesOrderPosID { get; set; }
        public string OrderNo { get; set; }
        public DateTime PostingDate { get; set; }
        public string Terminal { get; set; }
        public string SalesPerson { get; set; }
        public bool IsSyncronized { get; set; }
        public DateTime? SyncTime { get; set; }
        public bool ReceivedReceipt { get; set; }
        public DateTime SyncReceipt { get; set; }
        public string PostingTime { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerAddress2 { get; set; }
        public string CustomerPostNo { get; set; }
        public string CustomerCity { get; set; }
        public string ReferenceNo { get; set; }
        public string ReferenceName { get; set; }
        public string InvoiceCustomerNo { get; set; }
        public string DeliveryAddressCode { get; set; }
        public string ContactNo { get; set; }
        public string AgreementNo { get; set; }
        public string MembershipCardNo { get; set; }
        public long ExportTransactionId { get; set; }
        public DateTime DueDate { get; set; }
        public bool ExportToReportingDatabase { get; set; }
        public int SaleType { get; set; }
        public bool IsSyncedToMaster { get; set; }

        public bool IsExportedToPO { get; set; }
        public bool ExportedToMainCard { get; set; }
        public bool ExportedToFinancing { get; set; }
        public bool TransferredToFactoring { get; set; }
        public string KID { get; set; }
        public bool Exported { get; set; }
        public bool ByggDokStatus { get; set; }
        public string DeliveryCode { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string DeliveryTime { get; set; }
        public string TrackingNo { get; set; }

        public string Signature { get; set; }
        public int SignatureVersion { get; set; }
        public DateTime Created { get; set; }
        public DateTime ModifiedOn { get; set; }

        public string CurrencyCode { get; set; }
    }
}
