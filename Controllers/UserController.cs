using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deestone.Models;
using Deestone.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Deestone.Controllers
{
  public class UserController : Controller
  {
    UserService userService = new UserService();
    AuthService authService = new AuthService();
    JwtService jwtService = new JwtService();

    string tokenName = Startup.GetTokenName();

    [HttpGet]
    public IActionResult Index()
    {
      if (authService.IsLogin(HttpContext.Request).result == false)
      {
        return Redirect("/user/login");
      }

      return View("User");
    }

    [HttpGet]
    public IActionResult Register([FromQuery] string message)
    {
      ViewData["message"] = message;

      return View("Register");
    }

    [HttpGet]
    public IActionResult Login()
    {
      return View("Login");
    }

    [HttpGet]
    public IActionResult Logout()
    {
      Response.Cookies.Delete(tokenName);
      return RedirectToAction("Login", "User");
    }

    [HttpPost]
    public ResponseModel Create(string email, string company, int order, int emailGroup, int canApprove, int notifyTo, int finalApprove, int itemGroup)
    {
      try
      {
        var create = userService.Create(email, company, order, emailGroup, canApprove, notifyTo, finalApprove, itemGroup);

        return new ResponseModel
        {
          result = true,
          message = create.message
        };
      }
      catch (Exception ex)
      {
        return new ResponseModel
        {
          result = true,
          message = ex.Message
        };
      }
    }

    // end
  }
}