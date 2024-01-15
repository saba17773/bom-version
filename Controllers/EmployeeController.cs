using Deestone.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deestone.Services;

namespace Deestone.Controllers
{
    public class EmployeeController : Controller
    {
        EmployeeService employeeService = new EmployeeService();

        [HttpPost]
        public JsonResult GetEmployee()
        {
            try
            {
                var rows = employeeService.GetEmployee();
                return Json(new
                {
                    draw = 1,
                    recordsTotal = rows.Count,
                    recordsFiltered = 10,
                    data = rows
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}
