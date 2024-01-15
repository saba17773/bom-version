using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Deestone.Services;
using Microsoft.AspNetCore.Http;

namespace Deestone.Controllers
{
    public class DashboardController : Controller
    {
        AuthService authService = new AuthService();

        [HttpGet]
        public IActionResult Index()
        {
            if (authService.IsLogin(HttpContext.Request).result == false)
            {
                return Redirect("/user/login");
            }

            return Redirect("/email");
        }
    }
}