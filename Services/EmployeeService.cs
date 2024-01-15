using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Deestone.Models;

namespace Deestone.Services
{
    public class EmployeeService
    {
        public List<EmployeeModel> GetEmployee()
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("HR")))
                {
                    var rows = conn.Query<EmployeeModel>(@"SELECT
                        E.CODEMPID,
                        E.EMPNAME,
                        E.EMPLASTNAME,
                        E.COMPANYNAME,
                        E.POSITIONCODE,
                        E.DIVISIONCODE,
                        E.DEPARTMENTCODE,
                        E.DIVISIONNAME,
                        E.DEPARTMENTNAME,
                        E.POSITIONNAME,
                        T.EMAIL
                        FROM EMPLOYEE E
                        LEFT JOIN TEMPLOY1 T ON T.CODEMPID = E.CODEMPID
                        WHERE E.STATUS = 3").ToList();

                    return rows;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ResponseModel CheckEmpId(string empId)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("HR")))
                {
                    var user = conn.Query(@"SELECT TOP 1 CODEMPID 
                        FROM EMPLOYEE
                        WHERE CODEMPID = @EMPID
                        AND STATUS = 3",
                        new { @EMPID = empId })
                        .ToList();

                    if (user.Count == 0)
                    {
                        throw new Exception("User not found.");
                    }

                    return new ResponseModel
                    {
                        result = true,
                        message = "User " + empId + " is exists."
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
