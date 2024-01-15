using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Deestone.Models;
using Deestone.Services;
using Microsoft.AspNetCore.Http;

namespace Deestone.Controllers
{
    public class UserApiController : Controller
    {
        UserService userService = new UserService();
        JwtService jwtService = new JwtService();
        EmployeeService empService = new EmployeeService();
        TableService tableService = new TableService();
        AuthService authService = new AuthService();

        string tokenName = Startup.GetTokenName();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(UserLogin userLogin)
        {
            try
            {
                var user = userService.IsActive(userLogin.username, userLogin.password);

                if (user.result == false)
                {
                    throw new Exception(user.message);
                }

                var roleId = userService.GetRoleFromUsername(userLogin.username);

                string token = jwtService.CreateToken(new UserDataModel {
                    username = userLogin.username,
                    role = roleId
                });

                Response.Cookies.Append(tokenName, token, new CookieOptions()
                {
                    Expires = DateTime.Now.AddHours(12),
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict
                });

                return Redirect("/");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string username, string password, string empid)
        {
            try
            {
                string hashedPassword = jwtService.HashPassword(password);

                if (username.Length < 8)
                {
                    throw new Exception("Username ต้องไม่ต่ำกว่า 8 ตัวอักษร.");
                }

                var emp = empService.CheckEmpId(empid);

                if (emp.result == false)
                {
                    throw new Exception(emp.message);
                }

                var user = userService.CheckDuplicate(username);

                if (user.result == false)
                {
                    throw new Exception(user.message);
                }

                ResponseModel register = userService.Register(username, hashedPassword, empid);

                if (register.result == true)
                {
                    return Redirect("/user/register?message=" + register.message);
                }
                else
                {
                    throw new Exception(register.message);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public JsonResult GetUserMaster()
        {
            try
            {
                var users = userService.GetUserMaster();

                return Json(new
                {
                    draw = 1,
                    recordsTotal = users.Count,
                    recordsFiltered = users.Count,
                    data = users
                });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        public ResponseModel UpdateUserMaster(int pk, string name, string value)
        {
            try
            {
                var update = tableService.UpdateTable("BOM", "USER_MASTER", pk, name, value);

                return new ResponseModel
                {
                    result = true,
                    message = update.message
                };

            }
            catch (Exception ex)
            {
                return new ResponseModel
                {
                    result = false,
                    message = ex.Message
                };

            }
        }

        [NonAction]
        public int GetRole()
        {
            try
            {
                authService.IsLogin(Request);

                var userData = jwtService.ValidateToken(Request.Cookies[Startup.GetTokenName()]);

                return 1;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        // end
    }
}
