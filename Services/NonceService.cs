using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Dapper;
using Deestone.Models;

namespace Deestone.Services
{
    public class NonceService
    {
        public string CreateNonce()
        {
            try
            {
                string nonce = Guid.NewGuid().ToString("N");

                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var insert = conn.Execute(@"INSERT INTO NONCE(NONCE) VALUES(@NONCE)",
                        new { @NONCE = nonce });

                    if (insert == -1)
                    {
                        throw new Exception("Error create nonce.");
                    }

                    return nonce;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ResponseModel UpdateNonce(string nonce, int value)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var updateNonce = conn.Execute(@"UPDATE NONCE SET USED = @VALUE WHERE NONCE = @NONCE",
                        new { @VALUE = value, @NONCE = nonce });

                    if (updateNonce == -1)
                    {
                        throw new Exception("Update nonce failed.");
                    }

                    return new ResponseModel
                    {
                        result = true,
                        message = "Update nonce success."
                    };
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ResponseModel ValidateNonce(string nonce)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query(@"SELECT NONCE FROM NONCE WHERE NONCE = @NONCE AND USED = 0",
                        new { @NONCE = nonce }).ToList();

                    if (rows.Count > 0)
                    {
                        return new ResponseModel
                        {
                            result = true,
                            message = "Nonce valid."
                        };
                    }
                    else
                    {
                        return new ResponseModel
                        {
                            result = false,
                            message = "Nonce invalid."
                        };
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        // end class
    }
}
