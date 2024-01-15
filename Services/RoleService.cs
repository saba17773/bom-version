using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace Deestone.Services
{
    public class RoleService
    {
        public List<object> GetRoleMaster()
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query(@"SELECT * FROM USER_ROLE").ToList();
                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
