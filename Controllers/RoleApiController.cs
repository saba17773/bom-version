using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deestone.Services;

namespace Deestone.Controllers
{
    public class RoleApiController : Controller
    {
        RoleService roleService = new RoleService();
        
        [HttpPost]
        public List<object> GetRoleMaster()
        {
            try
            {
                var rows = roleService.GetRoleMaster();
                return rows;
            }
            catch (Exception)
            {
                return new List<object>();
            }
        }
    }
}
