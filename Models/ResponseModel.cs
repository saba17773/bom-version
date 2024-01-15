using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Deestone.Models
{
  public class ResponseModel
  {
    public bool result { get; set; }
    public string message { get; set; }
    public UserDataModel data { get; set; }
  }
}
