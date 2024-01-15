using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Deestone.Models
{
  public class BomListsModel
  {
    public int ID { get; set; }
    public string BOMID { get; set; }
    public string BOMNAME { get; set; }
    public string COMPANY_REF { get; set; }
    public string STATUS { get; set; }
    public DateTime CREATE_DATE { get; set; }
    public DateTime UPDATE_DATE { get; set; }
    public DateTime COMPLETE_DATE { get; set; }
    public int AX_STATUS { get; set; }
  }
}
