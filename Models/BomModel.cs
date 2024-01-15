using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Deestone.Models
{
  public class FetchEmailApproveModel
  {
    public int EMAIL_RECID { get; set; }
    public string EMAIL_ORDER { get; set; }
    public string BOM_RECID { get; set; }
    public string ITEMGROUPID { get; set; }
    public string DIMENSION { get; set; }
  }

  public class EmailForApproveModel
  {
    public int ID { get; set; }
    public int BOM_RECID { get; set; }
    public string BOMID { get; set; }
    public string ITEM_GROUP { get; set; }
    public string EMAIL { get; set; }
    public int EMAIL_ORDER { get; set; }
    public int EMAIL_RECID { get; set; }
    public int NOTIFY_TO { get; set; }
    public int IS_FINAL { get; set; }
    public int CAN_APPROVE { get; set; }
  }

  public class ItemRelateBomVersionModel
  {
    public string BOMID { get; set; }
    public string ITEMID { get; set; }
    public string ITEM_NAME { get; set; }
  }

  public class BomVersionDetailModel
  {
    public string BOMID { get; set; }
    public string ITEMID { get; set; }
    public double LINENUM { get; set; }
    public double BOMQTY { get; set; }
    public string UNITID { get; set; }
    public double BOMQTYSERIE { get; set; }
    public int DSG_LOGBOMID { get; set; }
    public string LOG_BOMITEMID { get; set; }
    public double LOG_LINENUM { get; set; }
    public double LOG_BOMQTY { get; set; }
    public string LOG_UNITID { get; set; }
    public double PERSERIES { get; set; }
  }

  public class GetBomSendEmailModel
  {
    public int ID { get; set; }
    public string BOMID { get; set; }
    public string ITEMID { get; set; }
    public string COMPANY_REF { get; set; }
    public int STATUS { get; set; }
  }

  public class GetEmailFromBomModel
  {
    public int IS_FINAL { get; set; }
    public int NOTIFY_TO { get; set; }
    public int ID { get; set; }
    public int BOM_RECID { get; set; }
    public string BOMID { get; set; }
    public string ITEM_GROUP { get; set; }
    public string EMAIL { get; set; }
    public int EMAIL_ORDER { get; set; }
    public int EMAIL_RECID { get; set; }
    public int CAN_APPROVE { get; set; }
    public string COMPANY_REF { get; set; }
    public int AX_STATUS { get; set; }
  }

  public class IsFinalApproveModel
  {
    public int IS_FINAL { get; set; }
  }

  public class GridUpdateModel
  {
    public int pk { get; set; }
    public string name { get; set; }
    public string value { get; set; }
  }

  public class GetEmailApproveByCompanyModel
  {
    public int ID { get; set; }
    public string EMAIL { get; set; }
    public string COMPANY { get; set; }
  }

  public class RemarkModel
  {
    public int ID { get; set; }
    public string REMARK { get; set; }
    public DateTime CREATE_DATE { get; set; }
    public string BOMID { get; set; }
    public string EMAIL { get; set; }
    public int APPROVE { get; set; }
  }

  public class EmailForSendModel
  {
    public string COMPANY { get; set; }
    public string EMAIL { get; set; }
    public string ITEM_GROUP { get; set; }
    public string EMPNAME { get; set; }
    public int BOM_RECID { get; set; }
  }

  public class BomListApproveModel
  {
    public string BOMID { get; set; }
    public string COMPANY { get; set; }
    public string EMAIL { get; set; }
    public string ITEM_GROUP { get; set; }
  }

  public class ListResultApproveModel
  {
    public string BOMID { get; set; }
    public int APPROVE { get; set; }
    public string REASON { get; set; }
    public int APPROVE_TYPE { get; set; }
    public int BOM_RECID { get; set; }
  }

  public class TempBomDetailModel
  {
    public int ID { get; set; }
    public int BOM_RECID { get; set; }
    public string BOMID { get; set; }
    public string COMPANY { get; set; }
    public int EMAIL_RECID { get; set; }
    public string EMAIL { get; set; }
    public string ITEM_GROUP { get; set; }
    public int SEND { get; set; }
    public int CAN_APPROVE { get; set; }
    public int NOTIFY_TO { get; set; }
    public int IS_FINAL { get; set; }
    public int APPROVE_TRANS_ID { get; set; }
  }
}
