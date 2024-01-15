using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Deestone.Models
{
  public class BomDetailViewModel
  {
    public List<BomDetailModel> BOM_DETAIL { get; set; }
    public List<BomApproveRemarkModel> BOM_REMARK { get; set; }
    public string REMARK { get; set; }
  }

  public class BomDetail
  {
    public string BOMID { get; set; }
    public string ITEMID { get; set; }
    public string ITEMNAME { get; set; }
    public string LOG_ITEMNAME { get; set; }
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
    public int AX_STATUS { get; set; }
    public string BOMNAME { get; set; }
  }
}
