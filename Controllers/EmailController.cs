using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deestone.Services;

namespace Deestone.Controllers
{
    public class EmailController : Controller
    {
        AuthService authService = new AuthService();

        [HttpGet]
        public IActionResult Index()
        {
            try
            {
                if (authService.IsLogin(HttpContext.Request).result == false)
                {
                    return Redirect("/user/login");
                }

                return View("Email");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
            
        }
    }
}
