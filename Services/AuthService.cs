using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deestone.Models;

namespace Deestone.Services
{
    public class AuthService
    {
        JwtService jwtService = new JwtService();

        public ResponseModel IsLogin(HttpRequest req)
        {
            try
            {
                if (req.Cookies.ContainsKey(Startup.GetTokenName()) == false)
                {
                    throw new Exception("กรุณาเข้าสู่ระบบ");
                }
                else
                {
                    var auth = jwtService.ValidateToken(req.Cookies[Startup.GetTokenName()]);

                    if (auth.result == false)
                    {
                        throw new Exception(auth.message);
                    }

                    return new ResponseModel
                    {
                        result = true,
                        message = "You are logged in."
                    };
                }
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
    }
}
