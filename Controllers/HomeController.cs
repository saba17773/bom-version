using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Deestone.Models;
using Microsoft.Extensions.Configuration;

namespace Deestone.Controllers
{
  public class HomeController : Controller
  {
    private IConfiguration _configuration;

    public HomeController(IConfiguration configuration)
    {
      _configuration = configuration;
    }

    // [HttpGet]
    // public IActionResult Test()
    // {
    //   return Ok("URL : " + _configuration["CONFIG:BASE_URL"]);
    // }

    [HttpGet]
    public IActionResult Error(string message = "", string linkBack = "")
    {
      ViewData["Message"] = message;

      if (linkBack == "")
      {
        ViewData["LinkBack"] = "/";
      }
      else
      {
        ViewData["LinkBack"] = linkBack;
      }

      return View("Error");
    }
  }
}
