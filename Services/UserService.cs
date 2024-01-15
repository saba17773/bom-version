using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deestone.Models;
using Dapper;
using System.Data.SqlClient;
using Deestone.Services;
using Microsoft.AspNetCore.Http;

namespace Deestone.Services
{
  public class UserService
  {
    JwtService jwtService = new JwtService();

    public ResponseModel Register(string username, string hashedPassword, string empid)
    {
      try
      {
        using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
        {
          var create = conn.Execute(@"INSERT INTO USER_MASTER(
                            USERNAME, PASSWORD, EMPID
                        ) VALUES (
                            @USERNAME, @PASSWORD, @EMPID
                        )
                    ", new { @USERNAME = username, @PASSWORD = hashedPassword, @EMPID = empid });

          if (create == -1)
          {
            throw new Exception("Insert error.");
          }

          return new ResponseModel
          {
            result = true,
            message = "Register successful."
          };
        }
      }
      catch (Exception)
      {
        throw;
      }
    }

    public ResponseModel Create(string email, string company, int order, int emailGroup, int canApprove, int notifyTo, int finalApprove, int itemGroup)
    {
      try
      {
        using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
        {
          var create = conn.Execute(@"INSERT INTO APPROVE_EMAIL(EMAIL, COMPANY, EMAIL_ORDER, EMAIL_GROUP, CAN_APPROVE, NOTIFY_TO, IS_FINAL, ITEM_GROUP)
                        VALUES(@EMAIL, @COMPANY, @EMAIL_ORDER, @EMAIL_GROUP, @CAN_APPROVE, @NOTIFY_TO, @IS_FINAL, @ITEM_GROUP)",
              new { @EMAIL = email, @COMPANY = company, @EMAIL_ORDER = order, @EMAIL_GROUP = emailGroup, @CAN_APPROVE = canApprove, @NOTIFY_TO = notifyTo, @IS_FINAL = finalApprove, @ITEM_GROUP = itemGroup });

          if (create == -1)
          {
            throw new Exception("Error create user email.");
          }
        }

        return new ResponseModel
        {
          result = true,
          message = "Create success."
        };
      }
      catch (Exception)
      {

        throw;
      }
    }

    public ResponseModel CheckDuplicate(string username)
    {
      try
      {
        using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
        {

          var user = conn.Query(@"SELECT USERNAME FROM USER_MASTER WHERE USERNAME = @USERNAME",
              new { @USERNAME = username.Trim() })
              .ToList();

          if (user.Count > 0)
          {
            throw new Exception("Username are used by other.");
          }

          return new ResponseModel
          {
            result = true,
            message = "Username " + username + " is avaliable."
          };
        }
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

    public ResponseModel IsActive(string username, string password)
    {
      try
      {
        using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
        {
          var hashedPassword = jwtService.HashPassword(password);

          var user = conn.Query(@"SELECT ID from USER_MASTER
                        WHERE USERNAME = @USERNAME
                        AND PASSWORD = @PASSWORD
                        AND ACTIVE = 1",
              new { @USERNAME = username, @PASSWORD = hashedPassword })
              .ToList();

          if (user.Count == 0)
          {
            throw new Exception("User not found.");
          }

          return new ResponseModel
          {
            result = true,
            message = "User " + username + " is active."
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

    public List<object> GetUserMaster()
    {
      try
      {
        using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
        {
          var rows = conn.Query(@"SELECT 
                        U.ID,
                        U.USERNAME,
                        U.EMPID,
                        E.EMPNAME,
                        E.EMPLASTNAME,
                        R.NAME AS ROLE_NAME,
                        U.ACTIVE
                        from USER_MASTER U
                        LEFT JOIN USER_ROLE R ON U.ROLE = R.ID
                        LEFT JOIN [HRTRAINING].[dbo].EMPLOYEE E ON E.CODEMPID = U.EMPID").ToList();

          return rows;
        }
      }
      catch (Exception)
      {

        throw;
      }
    }

    public int GetRoleFromUsername(string username)
    {
      try
      {
        using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
        {
          var role = conn.Query(@"SELECT ROLE FROM USER_MASTER WHERE USERNAME = @USERNAME",
              new { @USERNAME = username }).ToList();

          if (role.Count == 0)
          {
            throw new Exception("Role not found on " + username);
          }
          else
          {
            return role[0].ROLE;
          }
        }
      }
      catch (Exception)
      {

        throw;
      }
    }

    public ResponseModel IsAdmin(HttpRequest req)
    {
      try
      {
        string token = req.Cookies[Startup.GetTokenName()];

        var validateToken = jwtService.ValidateToken(token);

        if (validateToken.result == false)
        {
          throw new Exception(validateToken.message);
        }

        if (validateToken.data.role == 1)
        {
          return new ResponseModel
          {
            result = true,
            message = "You're admin.",
            data = new UserDataModel
            {
              username = validateToken.data.username
            }
          };
        }
        else
        {
          return new ResponseModel
          {
            result = false,
            message = "You're not admin."
          }; ;
        }
      }
      catch (System.Exception ex)
      {
        return new ResponseModel
        {
          result = false,
          message = ex.Message
        };
      }
    }

    // end
  }
}
