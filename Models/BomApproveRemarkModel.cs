using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Deestone.Models
{
    public class BomApproveRemarkModel
    {
        public int BOM_RECID { get; set; }
        public string BOMID { get; set; }
        public string EMAIL { get; set; }
        public string EMPNAME { get; set; }
        public string REMARK { get; set; }
        public int APPROVE { get; set; }
        public int AX_STATUS { get; set; }
    }
}
