using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Deestone.Models;

namespace Deestone.Services
{
    public class TableService
    {
        public ResponseModel UpdateTable(string connString, string table, int pk, string name, string value)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings(connString)))
                {
                    var result = conn.Execute(@"UPDATE "+ table +
                        @" SET " + name + " = " + @"@VALUE WHERE ID = @ID",
                        new { @VALUE = value, @ID = pk });

                    if (result == -1)
                    {
                        throw new Exception("Update "+ table + " error.");
                    }
                }

                return new ResponseModel
                {
                    result = true,
                    message = "Update success."
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
    }
}
