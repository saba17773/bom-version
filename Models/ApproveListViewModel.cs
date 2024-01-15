using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Deestone.Models
{
  public class ApproveListViewModel
  {
    public List<TempBomDetailModel> BOMLISTAPPROVE { get; set; }
    public List<BomApproveRemarkModel> BOMREMARK { get; set; }
    public string EMAIL { get; set; }
    public string NONCE { get; set; }
    public List<BomRecIdModel> BOM_RECID { get; set; }
  }
}
