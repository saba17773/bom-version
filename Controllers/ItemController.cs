using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deestone.Services;

namespace Deestone.Controllers
{
    public class ItemController : Controller
    {
        ItemService itemService = new ItemService();

        [HttpPost]
        public JsonResult GetItemGroup()
        {
            try
            {
                var rows = itemService.GetItemGroup();
                return Json(rows);
            }
            catch (Exception)
            {
                return Json(new List<object>());
            }
        }
    }
}
