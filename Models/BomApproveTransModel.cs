using System;

namespace Deestone.Models
{
  public class BomApproveTransModel
  {
    public int ID { get; set; }
    public string BOMID { get; set; }
    public string EMAIL { get; set; }
    public int EMAIL_ORDER { get; set; }
    public DateTime CREATE_DATE { get; set; }
    public DateTime SEND_DATE { get; set; }
    public DateTime SEND_NOTIFY_DATE { get; set; }
    public DateTime APPROVE_DATE { get; set; }
    public DateTime REJECT_DATE { get; set; }
  }
}