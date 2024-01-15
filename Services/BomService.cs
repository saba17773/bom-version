using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Deestone.Models;
using Microsoft.AspNetCore.Http;

namespace Deestone.Services
{
    public class BomService
    {
        public ResponseModel GetBomFromAx()
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        var getBomVersion = conn.Execute(@"INSERT INTO BOM(
	                            BOMID,
                              BOMNAME,
	                            COMPANY_REF,
	                            CREATE_DATE,
	                            STATUS,
                              AX_STATUS,
                              REMARK
                            )
                            SELECT
                            BV.BOMID,
                            BV.NAME,
                            BV.DSG_REFCOMPANYID,
                            GETDATE() AS CREATE_DATE,
                            1 AS BOM_STATUS,
                            BV.DSG_PROJECTAPPROVEBOM,
                            BRS.BOMREASON
                            FROM [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV
                            LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.DSG_BOMREASON BRS ON BRS.BOMID = BV.BOMID AND BRS.FLAG = 0
                            WHERE BV.DSG_PROJECTAPPROVEBOM IN (1, 4)
                            AND BV.DSG_WaitMail = 0
                            AND BV.DATAAREAID = 'dv'
                            AND BV.DSG_REFCOMPANYID IS NOT NULL
                            AND BV.DSG_REFCOMPANYID <> ''
                            AND BV.BOMID NOT IN (
                                SELECT BOMID 
                                FROM BOM
                                WHERE BOMID = BV.BOMID
                                AND COMPANY_REF = BV.DSG_REFCOMPANYID
                                AND STATUS = 1
                            )
                            GROUP BY 
                            BV.BOMID,
                            BV.NAME,
                            BV.DSG_REFCOMPANYID,
                            BV.DSG_PROJECTAPPROVEBOM,
                            BRS.BOMREASON",
                            null,
                            transaction);

                        if (getBomVersion == -1)
                        {
                            transaction.Rollback();
                            throw new Exception("Get bom version failed.");
                        }

                        var getBomLine = conn.Execute(@"INSERT INTO BOMLINE(
	                            BOM_RECID,
	                            ITEMID,
	                            BOMQTY,
	                            UNITID,
	                            CREATE_DATE,
	                            STATUS
                            )
                            SELECT 
                            B.ID,
                            BL.ITEMID,
                            BL.BOMQTY,
                            BL.UNITID,
                            GETDATE() AS CREATE_DATE,
                            B.AX_STATUS
                            FROM [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOM BL
                            LEFT JOIN BOM B ON BL.BOMID = B.BOMID
                            WHERE BL.BOMID IN (
                                SELECT BV.BOMID
                                FROM [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV
                                WHERE BV.DSG_PROJECTAPPROVEBOM IN (1, 4)
                                AND BV.DSG_WaitMail = 0
                                AND BV.DSG_REFCOMPANYID IS NOT NULL
                                AND BV.DSG_REFCOMPANYID <> ''
                                AND BV.DATAAREAID = 'dv'
                                AND BV.BOMID = BL.BOMID
                            )
                            AND BL.DATAAREAID = 'dv'",
                            null,
                            transaction);

                        if (getBomLine == -1)
                        {
                            transaction.Rollback();
                            throw new Exception("Get bom line failed.");
                        }

                        transaction.Commit();
                    }
                }

                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    conn.Open();

                    var updateWaitingMailAx = conn.Execute(@"UPDATE 
                        [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION
                        SET DSG_WaitMail = 1
                        WHERE DSG_PROJECTAPPROVEBOM IN (1, 4)
                        AND DSG_WaitMail = 0
                        AND DSG_REFCOMPANYID IS NOT NULL
                        AND DSG_REFCOMPANYID <> ''
                        AND DATAAREAID = 'dv'");

                    if (updateWaitingMailAx == -1)
                    {
                        throw new Exception("Update waiting mail failed.");
                    }

                    var UpdateFlag = conn.Execute(@"
                        UPDATE [frey\live].[DSL_AX40_SP1_LIVE].dbo.DSG_BOMREASON
                        SET FLAG = 1
                        WHERE BOMID IN (
                          SELECT BOMID 
                          FROM BOM
                          WHERE STATUS = 1
                          AND UPDATE_DATE IS NULL
                        )
                      ");

                    if (UpdateFlag == -1)
                    {
                        throw new Exception("Update flag failed.");
                    }
                }

                return new ResponseModel
                {
                    result = true,
                    message = "Get bom success."
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

        public List<GetEmailFromBomModel> GetEmailFromBom(string bomId, string company, int emailRecId = 0, bool sendAll = false)
        {
            try
            {
                string sql = "";

                if (emailRecId == 0)
                {
                    if (sendAll == false)
                    {
                        sql = @"SELECT TOP 1 
                            AE.IS_FINAL, 
                            AE.NOTIFY_TO, 
                            AE.CAN_APPROVE,
                            AT.ID, 
                            AT.BOM_RECID, 
                            B.BOMID, 
                            B.COMPANY_REF,
                            IT.ITEMGROUPID AS ITEM_GROUP, 
                            AE.EMAIL, 
                            AE.EMAIL_ORDER, 
                            AE.ID AS EMAIL_RECID,
                            B.AX_STATUS
                            FROM APPROVE_TRANS AT
                            LEFT JOIN APPROVE_EMAIL AE ON AT.EMAIL_RECID = AE.ID
                            LEFT JOIN BOM B ON B.ID = AT.BOM_RECID
                            LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV ON BV.BOMID = B.BOMID  AND BV.DATAAREAID = 'dv'
                            LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE IT ON IT.ITEMID = BV.ITEMID AND IT.DATAAREAID = 'dv'
                            LEFT JOIN ITEM_GROUP IG ON IG.NAME = IT.ITEMGROUPID
                            WHERE IG.ID = AE.ITEM_GROUP
                            AND IG.NAME = IT.ITEMGROUPID
                            AND AT.SEND_DATE IS NULL
                            AND B.BOMID = @BOMID
                            AND B.COMPANY_REF = @COMPANY
                            AND AT.APPROVE_DATE IS NULL
                            AND AT.REJECT_DATE IS NULL
                            GROUP BY 
                            AT.ID, 
                            AT.BOM_RECID, 
                            B.AX_STATUS, 
                            B.COMPANY_REF, 
                            B.BOMID, 
                            IT.ITEMGROUPID, 
                            AE.EMAIL, 
                            AE.ID, 
                            AE.EMAIL_ORDER, 
                            AE.NOTIFY_TO, 
                            AE.CAN_APPROVE, 
                            AE.IS_FINAL
                            ORDER BY AE.EMAIL_ORDER ASC";

                        using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                        {
                            conn.Open();

                            var rows = conn.Query<GetEmailFromBomModel>(sql, new { @BOMID = bomId, @COMPANY = company }).ToList();
                            return rows;
                        }
                    }
                    else
                    {
                        sql = @"SELECT
                            AE.IS_FINAL, 
                            AE.NOTIFY_TO, 
                            AE.CAN_APPROVE,
                            AT.ID, 
                            AT.BOM_RECID, 
                            B.BOMID, 
                            B.COMPANY_REF,
                            IT.ITEMGROUPID AS ITEM_GROUP, 
                            AE.EMAIL, 
                            AE.EMAIL_ORDER, 
                            AE.ID AS EMAIL_RECID,
                            B.AX_STATUS
                            FROM APPROVE_TRANS AT
                            LEFT JOIN APPROVE_EMAIL AE ON AT.EMAIL_RECID = AE.ID
                            LEFT JOIN BOM B ON B.ID = AT.BOM_RECID
                            LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV ON BV.BOMID = B.BOMID  AND BV.DATAAREAID = 'dv'
                            LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE IT ON IT.ITEMID = BV.ITEMID AND IT.DATAAREAID = 'dv'
                            LEFT JOIN ITEM_GROUP IG ON IG.NAME = IT.ITEMGROUPID
                            WHERE IG.ID = AE.ITEM_GROUP
                            AND IG.NAME = IT.ITEMGROUPID
                            AND B.BOMID = @BOMID
                            AND B.COMPANY_REF = @COMPANY
                            GROUP BY 
                            AT.ID, 
                            AT.BOM_RECID, 
                            B.AX_STATUS, 
                            B.COMPANY_REF, 
                            B.BOMID, 
                            IT.ITEMGROUPID, 
                            AE.EMAIL, 
                            AE.ID, 
                            AE.EMAIL_ORDER, 
                            AE.NOTIFY_TO, 
                            AE.CAN_APPROVE, 
                            AE.IS_FINAL
                            ORDER BY AE.EMAIL_ORDER ASC";

                        using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                        {
                            conn.Open();

                            var rows = conn.Query<GetEmailFromBomModel>(sql, new { @BOMID = bomId, @COMPANY = company }).ToList();
                            return rows;
                        }
                    }

                }
                else
                {
                    sql = @"SELECT TOP 1 
                        AE.IS_FINAL, 
                        AE.NOTIFY_TO, 
                        AE.CAN_APPROVE,
                        AT.ID, 
                        AT.BOM_RECID, 
                        B.BOMID, 
                        B.COMPANY_REF,
                        IT.ITEMGROUPID AS ITEM_GROUP, 
                        AE.EMAIL, 
                        AE.EMAIL_ORDER, 
                        AE.ID AS EMAIL_RECID,
                        B.AX_STATUS
                        FROM APPROVE_TRANS AT
                        LEFT JOIN APPROVE_EMAIL AE ON AT.EMAIL_RECID = AE.ID
                        LEFT JOIN BOM B ON B.ID = AT.BOM_RECID
                        LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV ON BV.BOMID = B.BOMID  AND BV.DATAAREAID = 'dv'
                        LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE IT ON IT.ITEMID = BV.ITEMID AND IT.DATAAREAID = 'dv'
                        LEFT JOIN ITEM_GROUP IG ON IG.NAME = IT.ITEMGROUPID
                        WHERE IG.ID = AE.ITEM_GROUP
                        AND IG.NAME = IT.ITEMGROUPID
                        AND B.BOMID = @BOMID
                        AND B.COMPANY_REF = @COMPANY
                        AND AT.APPROVE_DATE IS NULL
                        AND AT.REJECT_DATE IS NULL
                        AND AE.ID = @NOTIFY_TO
                        AND AE.EMAIL_GROUP = 3
                        AND AE.COMPANY = @COMPANY
                        GROUP BY 
                        AT.ID, 
                        AT.BOM_RECID, 
                        B.AX_STATUS, 
                        B.COMPANY_REF, 
                        B.BOMID, 
                        IT.ITEMGROUPID, 
                        AE.EMAIL, 
                        AE.ID, 
                        AE.EMAIL_ORDER, 
                        AE.NOTIFY_TO, 
                        AE.CAN_APPROVE, 
                        AE.IS_FINAL
                        ORDER BY AE.EMAIL_ORDER ASC";

                    using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                    {
                        conn.Open();

                        var rows = conn.Query<GetEmailFromBomModel>(sql, new { @BOMID = bomId, @COMPANY = company, @NOTIFY_TO = emailRecId }).ToList();
                        return rows;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<GetBomSendEmailModel> GetBomSendEmail(int bomRecId = 0)
        {
            try
            {
                string sql = "";

                if (bomRecId != 0)
                {
                    sql = @"SELECT 
                        B.ID,
                        B.BOMID,
                        B.COMPANY_REF,
                        B.STATUS
                        FROM BOM B
                        WHERE B.ID = @ID";

                    using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                    {
                        conn.Open();
                        var rows = conn.Query<GetBomSendEmailModel>(sql, new { @ID = bomRecId }).ToList();
                        return rows;
                    }
                }
                else
                {
                    sql = @"SELECT 
                        B.ID,
                        B.BOMID,
                        B.COMPANY_REF,
                        B.STATUS
                        FROM BOM B
                        WHERE B.STATUS IN (1, 4)
                        AND B.COMPLETE_DATE IS NULL";

                    using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                    {
                        var rows = conn.Query<GetBomSendEmailModel>(sql).ToList();
                        return rows;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public List<ItemRelateBomVersionModel> GetItemRelateBomVersion(string bomId)
        {
            try
            {
                var sql = @"SELECT BV.BOMID, BV.ITEMID, IT.DSGTHAIITEMDESCRIPTION AS ITEM_NAME 
          FROM [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV
          LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE IT ON IT.ITEMID = BV.ITEMID
          WHERE BV.BOMID = @BOMID
          AND BV.DATAAREAID = 'dv'
          GROUP BY BV.BOMID, BV.ITEMID, IT.DSGTHAIITEMDESCRIPTION";

                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    conn.Open();
                    var rows = conn.Query<ItemRelateBomVersionModel>(sql, new { @BOMID = bomId }).ToList();
                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<BomDetail> GetBomVersionDetail(string bomId)
        {
            var sql = @"SELECT
                BV.BOMID,
                BV.NAME AS BOMNAME,
                BOM.ITEMID,
                INTB.ITEMNAME,
                BOM.LINENUM,
                BOM.BOMQTY,
                BOM.UNITID,
                BOM.BOMQTYSERIE,
                LT.DSG_LOGBOMID,
                DSG_LOGBOMLINE.BOMITEMID AS LOG_BOMITEMID,
                DSG_LOGBOMLINE.LINENUM AS LOG_LINENUM,
                DSG_LOGBOMLINE.BOMQTY AS LOG_BOMQTY,
                DSG_LOGBOMLINE.UNITID AS LOG_UNITID,
                DSG_LOGBOMLINE.PERSERIES,
                BB.AX_STATUS
                FROM [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV
                LEFT JOIN BOM BB ON BB.BOMID = BV.BOMID AND BV.DATAAREAID = 'dv'
                LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOM ON
                BOM.DATAAREAID    = BV.DATAAREAID AND
                BOM.BOMID        = BV.BOMID
                LEFT JOIN
                (
                SELECT TOP 1
                MAX(DSG_LOGBOMTABLE.DSG_LOGBOMID) AS DSG_LOGBOMID,
                --0 AS DSG_LOGBOMID,
                DSG_LOGBOMTABLE.BOMID,
                DSG_LOGBOMTABLE.DATAAREAID
                FROM [frey\live].[DSL_AX40_SP1_LIVE].dbo.DSG_LOGBOMTABLE
                GROUP BY DSG_LOGBOMTABLE.BOMID,
                DSG_LOGBOMTABLE.DATAAREAID
                ) LT ON
                LT.BOMID        = BV.BOMID AND
                LT.DATAAREAID    = BV.DATAAREAID
                LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.DSG_LOGBOMLINE ON
                DSG_LOGBOMLINE.DATAAREAID        = LT.DATAAREAID AND
                DSG_LOGBOMLINE.DSG_LOGBOMID        = LT.DSG_LOGBOMID AND
                DSG_LOGBOMLINE.LINENUM            = BOM.LINENUM
                LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE INTB ON BOM.ITEMID = INTB.ITEMID
                WHERE BV.DATAAREAID = 'DV'
                AND BV.BOMID = @BOMID
                GROUP BY BV.BOMID,
                    BOM.ITEMID,
                    BOM.LINENUM,
                    BOM.BOMQTY,
                    BOM.UNITID,
                    BOM.BOMQTYSERIE,
                    LT.DSG_LOGBOMID,
                    DSG_LOGBOMLINE.BOMITEMID,
                    DSG_LOGBOMLINE.LINENUM,
                    DSG_LOGBOMLINE.BOMQTY,
                    DSG_LOGBOMLINE.UNITID,
                    DSG_LOGBOMLINE.PERSERIES,
                    BB.AX_STATUS,
                    BV.NAME,
                    INTB.ITEMNAME
                ORDER BY
                    BOM.LINENUM,
                    DSG_LOGBOMLINE.LINENUM";

            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    conn.Open();
                    var rows = conn.Query<BomDetail>(sql, new { @BOMID = bomId }).ToList();
                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<BomDetail> GetBomVersionDetail_v2(string bomId)
        {
            var sql = @"SELECT
        BV.BOMID,
        BV.NAME AS BOMNAME,
        BOM.ITEMID,
        INTB.DSGTHAIITEMDESCRIPTION AS ITEMNAME,
        INTB2.DSGTHAIITEMDESCRIPTION AS LOG_ITEMNAME,
        BOM.LINENUM,
        BOM.BOMQTY,
        BOM.UNITID,
        BOM.BOMQTYSERIE,
        LT.DSG_LOGBOMID,
        DSG_LOGBOMLINE.BOMITEMID AS LOG_BOMITEMID,
        DSG_LOGBOMLINE.LINENUM AS LOG_LINENUM,
        (
          CASE 
            WHEN DSG_LOGBOMLINE.BOMQTY = BOM.BOMQTY THEN (
              SELECT 	
                TOP 1
                LBL.BOMQTY
              FROM [frey\live].[DSL_AX40_SP1_LIVE].dbo.DSG_LOGBOMTABLE LBT
              LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.DSG_LOGBOMLINE LBL
                ON LBL.DSG_LOGBOMID = LBT.DSG_LOGBOMID
                AND LBL.BOMITEMID = DSG_LOGBOMLINE.BOMITEMID
              WHERE 
                LBT.BOMID = BV.BOMID AND
                LBT.TRANSDATE = LT.TRANSDATE
              ORDER BY LBT.DSG_LOGBOMID ASC
            )
            ELSE DSG_LOGBOMLINE.BOMQTY
          END
        ) AS LOG_BOMQTY,
        DSG_LOGBOMLINE.UNITID AS LOG_UNITID,
        DSG_LOGBOMLINE.PERSERIES,
        BB.AX_STATUS,
        LT.TRANSDATE
        FROM [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV
        LEFT JOIN BOM BB ON BB.BOMID = BV.BOMID AND BV.DATAAREAID = 'dv'
        LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOM ON
        BOM.DATAAREAID    = BV.DATAAREAID AND
        BOM.BOMID        = BV.BOMID
        LEFT JOIN
        (
        SELECT TOP 1
        MAX(DSG_LOGBOMTABLE.DSG_LOGBOMID) AS DSG_LOGBOMID,
        --0 AS DSG_LOGBOMID,
        DSG_LOGBOMTABLE.BOMID,
        DSG_LOGBOMTABLE.DATAAREAID,
        DSG_LOGBOMTABLE.TRANSDATE
        FROM [frey\live].[DSL_AX40_SP1_LIVE].dbo.DSG_LOGBOMTABLE
        GROUP BY DSG_LOGBOMTABLE.BOMID,
        DSG_LOGBOMTABLE.DATAAREAID,
        DSG_LOGBOMTABLE.TRANSDATE
        ) LT ON
        LT.BOMID        = BV.BOMID AND
        LT.DATAAREAID    = BV.DATAAREAID
        LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.DSG_LOGBOMLINE ON
        DSG_LOGBOMLINE.DATAAREAID        = LT.DATAAREAID AND
        DSG_LOGBOMLINE.DSG_LOGBOMID        = LT.DSG_LOGBOMID AND
        DSG_LOGBOMLINE.LINENUM            = BOM.LINENUM
        LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE INTB ON BOM.ITEMID = INTB.ITEMID
        LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE INTB2 ON DSG_LOGBOMLINE.BOMITEMID = INTB2.ITEMID
        WHERE BV.DATAAREAID = 'DV'
        AND BV.BOMID = @BOMID
        GROUP BY BV.BOMID,
            BOM.ITEMID,
            BOM.LINENUM,
            BOM.BOMQTY,
            BOM.UNITID,
            BOM.BOMQTYSERIE,
            LT.DSG_LOGBOMID,
            DSG_LOGBOMLINE.BOMITEMID,
            DSG_LOGBOMLINE.LINENUM,
            DSG_LOGBOMLINE.BOMQTY,
            DSG_LOGBOMLINE.UNITID,
            DSG_LOGBOMLINE.PERSERIES,
            BB.AX_STATUS,
            BV.NAME,
            INTB.DSGTHAIITEMDESCRIPTION,
            INTB2.DSGTHAIITEMDESCRIPTION,
            LT.TRANSDATE
        ORDER BY
            BOM.LINENUM,
            DSG_LOGBOMLINE.LINENUM";

            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    conn.Open();
                    var rows = conn.Query<BomDetail>(sql, new { @BOMID = bomId }).ToList();
                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<BomDetail> GetBomVersionDetail_v3(string bomId)
        {
            // string sql = @"
            //   SELECT 
            //   BL.BOMID,
            //   BL.NAME AS BOMNAME,
            //   BLL.ITEMID,
            //   INTB.DSGTHAIITEMDESCRIPTION AS ITEMNAME,
            //   INTB2.DSGTHAIITEMDESCRIPTION AS LOG_ITEMNAME,
            //   BLL.LINENUM,
            //   BLL.BOMQTY,
            //   BLL.UNITID,
            //   BLL.BOMQTYSERIE,
            //   null AS DSG_LOGBOMID,
            //   AX_BOMLINE.ITEMID AS LOG_BOMITEMID,
            //   AX_BOMLINE.LINENUM AS LOG_LINENUM,
            //   AX_BOMLINE.BOMQTY AS LOG_BOMQTY,
            //   AX_BOMLINE.UNITID AS LOG_UNITID,
            //   AX_BOMLINE.BOMQTYSERIE AS PERSERIES,
            //   BB.STATUS AS AX_STATUS,
            //   null AS TRANSDATE
            //   FROM BOM_LOG BL
            //   LEFT JOIN BOMLINE_LOG BLL 
            //     ON BLL.BOMID = BL.BOMID 
            //     AND BLL.DATAAREAID = 'dv'
            //     AND BL.DATAAREAID = 'dv'
            //   LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV 
            //     ON BV.BOMID = BL.BOMID 
            //     AND BL.DATAAREAID = 'dv'
            //     AND BV.DATAAREAID = 'dv'
            //   LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE INTB 
            //     ON BLL.ITEMID = INTB.ITEMID 
            //     AND BLL.DATAAREAID = 'dv'
            //     AND BL.DATAAREAID = 'dv'
            //   LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOM AX_BOMLINE 
            //     ON AX_BOMLINE.DATAAREAID = 'dv' 
            //     AND BL.DATAAREAID = 'dv'
            //     AND AX_BOMLINE.BOMID = BL.BOMID
            //     AND AX_BOMLINE.LINENUM = BLL.LINENUM
            //   LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE INTB2 
            //     ON INTB2.ITEMID = AX_BOMLINE.ITEMID 
            //     AND AX_BOMLINE.DATAAREAID = 'dv'
            //     AND BL.DATAAREAID = 'dv'
            //   LEFT JOIN BOM BB 
            //     ON BB.BOMID = BV.BOMID 
            //     AND BV.DATAAREAID = 'dv'
            //     AND BL.DATAAREAID = 'dv'
            //   WHERE BL.BOMID = @BOMID 
            //   AND BL.DATAAREAID = 'dv'
            //   GROUP BY
            //   BL.BOMID,
            //   BL.NAME,
            //   BLL.ITEMID,
            //   INTB.DSGTHAIITEMDESCRIPTION,
            //   BLL.LINENUM,
            //   BLL.BOMQTY,
            //   BLL.UNITID,
            //   BLL.BOMQTYSERIE,
            //   AX_BOMLINE.BOMID,
            //   AX_BOMLINE.UNITID,
            //   AX_BOMLINE.BOMQTY,
            //   AX_BOMLINE.BOMQTYSERIE,
            //   BB.STATUS,
            //   AX_BOMLINE.LINENUM,
            //   AX_BOMLINE.ITEMID,
            //   INTB2.DSGTHAIITEMDESCRIPTION
            //   ORDER BY
            //   BLL.LINENUM
            //   ASC
            // ";

            string sql = @"
        SELECT 
        BV.BOMID,
        BV.NAME AS BOMNAME,
        B.ITEMID,
        INTB.DSGTHAIITEMDESCRIPTION AS ITEMNAME,
        INTB2.DSGTHAIITEMDESCRIPTION as LOG_ITEMNAME,
        B.LINENUM,
        B.BOMQTY,
        B.UNITID,
        B.BOMQTYSERIE,
        0 AS DSG_LOGBOMID,
        BLL.ITEMID AS LOG_BOMITEMID,
        BLL.LINENUM AS LOG_LINENUM,
        BLL.BOMQTY AS LOG_BOMQTY,
        BLL.UNITID AS LOG_UNITID,
        BLL.BOMQTYSERIE AS PERSERIES,
        BB.STATUS AS AX_STATUS,
        null AS TRANSDATE
        FROM [FREY\LIVE].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV
        LEFT JOIN [FREY\LIVE].[DSL_AX40_SP1_LIVE].dbo.BOM B 
          ON B.BOMID = BV.BOMID 
          AND B.DATAAREAID = 'dv'
        LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE INTB 
          ON INTB.ITEMID = B.ITEMID
        LEFT JOIN BOM_LOG BL 
          ON BL.BOMID = BV.BOMID 
          AND BL.DATAAREAID = 'dv'
          AND BL.IS_CURRENT = 1
        LEFT JOIN BOMLINE_LOG BLL
          ON BLL.BOMID = BV.BOMID 
          AND BLL.DATAAREAID = 'dv'
          AND BL.IS_CURRENT = 1
          AND BLL.LINENUM = B.LINENUM
        LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE INTB2 
          ON BLL.ITEMID = INTB2.ITEMID
        LEFT JOIN BOM BB 
          ON BB.BOMID = BV.BOMID 
          AND BV.DATAAREAID = 'dv'
          AND BB.AX_STATUS IN (1, 4)
        WHERE BV.BOMID = @BOMID
        AND BV.DATAAREAID = 'dv'
        AND BB.AX_STATUS IS NOT NULL
        GROUP BY
        BV.BOMID,
        BV.NAME,
        B.ITEMID,
        INTB.DSGTHAIITEMDESCRIPTION,
        B.LINENUM,
        B.BOMQTY,
        B.UNITID,
        B.BOMQTYSERIE,
        BL.BOMID,
        BLL.ITEMID ,
        BLL.LINENUM,
        BLL.BOMQTY,
        BLL.UNITID,
        BLL.BOMQTYSERIE,
        INTB2.DSGTHAIITEMDESCRIPTION,
        BB.STATUS
        ORDER BY
        BLL.LINENUM
        ASC
      ";

            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<BomDetail>(sql, new { @BOMID = bomId }).ToList();
                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<BomDetailModel> GetBomVersionDetail_v4(string bomId)
        {
            try
            {
                string sql = @"
          SELECT 
          BV.BOMID,
          BV.NAME AS BOMNAME,
          B.ITEMID,
          IVT.DSGTHAIITEMDESCRIPTION AS ITEMNAME,
          B.UNITID,
          B.BOMQTY,
          B.BOMQTYSERIE,
          BLL.ITEMID AS OLD_ITEMID,
          IVT_OLD.DSGTHAIITEMDESCRIPTION AS OLD_ITEMNAME,
          BLL.UNITID AS OLD_UNITID,
          BLL.BOMQTY AS OLD_BOMQTY,
          BLL.BOMQTYSERIE AS OLD_BOMQTYSERIE,
          BV.DSG_PROJECTAPPROVEBOM AS AX_STATUS
          FROM [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOM B
          LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV
            ON B.BOMID = BV.BOMID
            AND B.DATAAREAID = 'dv'
          LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE IVT 
            ON IVT.ITEMID = B.ITEMID
          LEFT JOIN BOMLINE_LOG BLL 
            ON BLL.LINENUM = B.LINENUM
            AND BLL.BOMID = B.BOMID
            AND BLL.IS_CURRENT = 1
            AND BLL.DATAAREAID = 'dv'
          LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE IVT_OLD
            ON IVT_OLD.ITEMID = BLL.ITEMID
          WHERE B.BOMID = @BOMID
          AND B.DATAAREAID = 'dv'
          GROUP BY 
          BV.BOMID,
          BV.NAME,
          B.ITEMID,
          IVT.DSGTHAIITEMDESCRIPTION,
          B.BOMQTY,
          B.BOMQTYSERIE,
          B.LINENUM,
          BLL.LINENUM,
          BLL.BOMQTY,
          BLL.ITEMID,
          IVT_OLD.DSGTHAIITEMDESCRIPTION,
          BLL.BOMQTYSERIE,
          BV.DSG_PROJECTAPPROVEBOM,
          B.UNITID,
          BLL.UNITID
          ORDER BY 
          B.LINENUM
          ASC
        ";
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<BomDetailModel>(sql, new { @BOMID = bomId }).ToList();
                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ResponseModel UpdateEmailStatus(int approveTransId, int bomRecId, bool isNotify = false, bool isReject = false)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        if (isNotify == true)
                        {
                            var updateSendDate = conn.Execute(@"UPDATE APPROVE_TRANS
                                SET SEND_NOTIFY_DATE = GETDATE(),
                                SEND_DATE = GETDATE()
                                WHERE ID = @ID",
                                new { @ID = approveTransId },
                                transaction);

                            if (updateSendDate == -1)
                            {
                                transaction.Rollback();
                                throw new Exception("Update approve transaction failed.");
                            }
                        }
                        else
                        {
                            var updateSendDate = conn.Execute(@"UPDATE APPROVE_TRANS
                                SET SEND_DATE = GETDATE()
                                WHERE ID = @ID",
                                new { @ID = approveTransId },
                                transaction);

                            if (updateSendDate == -1)
                            {
                                transaction.Rollback();
                                throw new Exception("Update approve transaction failed.");
                            }
                        }

                        if (isReject == true)
                        {
                            var updateBomStatus = conn.Execute(@"UPDATE BOM 
                            SET STATUS = 3,
                            UPDATE_DATE = GETDATE()
                            WHERE ID = @BOMRECID",
                           new { @BOMRECID = bomRecId },
                           transaction);

                            if (updateBomStatus == -1)
                            {
                                transaction.Rollback();
                                throw new Exception("Update bom status failed.");
                            }
                        }
                        else
                        {
                            var updateBomStatus = conn.Execute(@"UPDATE BOM 
                            SET STATUS = 4,
                            UPDATE_DATE = GETDATE()
                            WHERE ID = @BOMRECID",
                           new { @BOMRECID = bomRecId },
                           transaction);

                            if (updateBomStatus == -1)
                            {
                                transaction.Rollback();
                                throw new Exception("Update bom status failed.");
                            }
                        }

                        transaction.Commit();

                        return new ResponseModel
                        {
                            result = true,
                            message = "Update success."
                        };
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }

        }

        public ResponseModel ComplateBom(int bomRecId, int status)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var update = conn.Execute(@"UPDATE BOM
                        SET STATUS = @STATUS,
                        COMPLETE_DATE = GETDATE()
                        WHERE ID = @BOMRECID",
                        new { @BOMRECID = bomRecId, @STATUS = status });

                    if (update == -1)
                    {
                        throw new Exception("Update complete bom failed.");
                    }

                    return new ResponseModel
                    {
                        result = true,
                        message = "Update success."
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

        public ResponseModel GenerateApproveFlow()
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        var emailApprove = new List<FetchEmailApproveModel>();

                        // emailApprove = conn.Query<FetchEmailApproveModel>(@"SELECT 
                        //                 B.ID AS BOM_RECID,
                        //                 AE.ID AS EMAIL_RECID,
                        //                 AE.EMAIL_ORDER
                        //                 FROM BOM B 
                        //                 LEFT JOIN APPROVE_EMAIL AE ON AE.COMPANY = B.COMPANY_REF AND AE.ACTIVE = 1
                        //                 LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV ON BV.BOMID = B.BOMID  AND BV.DATAAREAID = 'dv'
                        //                 LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE IT ON IT.ITEMID = BV.ITEMID AND IT.DATAAREAID = 'dv'
                        //                 LEFT JOIN ITEM_GROUP IG ON IG.NAME = IT.ITEMGROUPID AND IG.ID = AE.ITEM_GROUP
                        //                 LEFT JOIN APPROVE_STATUS AST ON AST.ID = B.STATUS
                        //                 WHERE B.STATUS IN (1, 4)
                        //                 AND BV.DATAAREAID = 'dv'
                        //                 AND IT.DATAAREAID = 'dv'
                        //                 AND AE.ACTIVE = 1
                        //                 AND IG.NAME IS NOT NULL
                        //                 AND AE.EMAIL IS NOT NULL
                        //                 AND IG.NAME = IT.ITEMGROUPID
                        //                 AND AE.ID NOT IN (
                        //                   SELECT TOP 1 AT.EMAIL_RECID
                        //                     FROM APPROVE_TRANS AT
                        //                     WHERE AT.EMAIL_RECID = AE.ID
                        //                     AND B.ID = AT.BOM_RECID
                        //                 )
                        //                 GROUP BY 
                        //                 B.ID,
                        //                 AE.ID,
                        //                 AE.EMAIL_ORDER
                        //                 ORDER BY AE.EMAIL_ORDER ASC", null, transaction).ToList();

                        // string oldQuery = @"
                        // SELECT 
                        //   B.ID AS BOM_RECID,
                        //   AE.ID AS EMAIL_RECID,
                        //   AE.EMAIL_ORDER,
                        //   IT.ITEMGROUPID,
                        //   IT.DIMENSION
                        //   FROM BOM B 
                        //   LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV ON BV.BOMID = B.BOMID  AND BV.DATAAREAID = 'dv'
                        //   LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE IT ON IT.ITEMID = BV.ITEMID AND IT.DATAAREAID = 'dv'
                        //   LEFT JOIN APPROVE_EMAIL AE 
                        //   ON AE.COMPANY = B.COMPANY_REF 
                        //   AND AE.ACTIVE = 1 
                        //   AND AE.DIMENSION =
                        //   CASE
                        //     WHEN IT.ITEMGROUPID = 'SM' AND IT.DIMENSION IN ('S202', 'S203') THEN 1
                        //     ELSE 0
                        //   END
                        //   LEFT JOIN ITEM_GROUP IG ON IG.NAME = IT.ITEMGROUPID AND IG.ID = AE.ITEM_GROUP
                        //   LEFT JOIN APPROVE_STATUS AST ON AST.ID = B.STATUS
                        //   WHERE B.STATUS IN (1, 4) AND 
                        //   BV.DATAAREAID = 'dv'
                        //   AND IT.DATAAREAID = 'dv'
                        //   AND AE.ACTIVE = 1
                        //   AND IG.NAME IS NOT NULL
                        //   AND AE.EMAIL IS NOT NULL
                        //   AND IG.NAME = IT.ITEMGROUPID
                        //   AND AE.ID NOT IN (
                        //       SELECT TOP 1 AT.EMAIL_RECID
                        //       FROM APPROVE_TRANS AT
                        //       WHERE AT.EMAIL_RECID = AE.ID
                        //       AND B.ID = AT.BOM_RECID
                        //   )
                        //   GROUP BY 
                        //   B.ID,
                        //   AE.ID,
                        //   AE.EMAIL_ORDER,
                        //   IT.ITEMGROUPID,
                        //   IT.DIMENSION
                        //   ORDER BY AE.EMAIL_ORDER ASC";

                        string queryItemQ = @"SELECT 
              B.ID AS BOM_RECID,
              AE.ID AS EMAIL_RECID,
              AE.EMAIL_ORDER,
              IT.ITEMGROUPID,
              IT.DIMENSION
              FROM BOM B 
              LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV ON BV.BOMID = B.BOMID  AND BV.DATAAREAID = 'dv'
              LEFT JOIN [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE IT ON IT.ITEMID = BV.ITEMID AND IT.DATAAREAID = 'dv'
              LEFT JOIN APPROVE_EMAIL AE 
              ON AE.COMPANY = B.COMPANY_REF 
              AND AE.ACTIVE = 1 
              AND AE.DIMENSION =
              CASE
              WHEN IT.ITEMGROUPID = 'SM' AND IT.DIMENSION IN ('MF220104', 'MF220105') THEN 1
              ELSE 0
              END
              LEFT JOIN ITEM_GROUP IG ON IG.NAME = (
                CASE
                  WHEN IT.ITEMGROUPID = 'SQ' THEN (
                    SELECT TOP 1 ITEMGROUPID 
                    FROM [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE
                    WHERE ITEMID = REPLACE(IT.ITEMID, 'Q', 'I')
                    AND DATAAREAID = 'dv'
                  )
                  ELSE IT.ITEMGROUPID
                END 
              ) 
              AND IG.ID = AE.ITEM_GROUP
              LEFT JOIN APPROVE_STATUS AST ON AST.ID = B.STATUS
              WHERE B.STATUS IN (1, 4) AND 
              BV.DATAAREAID = 'dv'
              AND IT.DATAAREAID = 'dv'
              AND AE.ACTIVE = 1
              AND IG.NAME IS NOT NULL
              AND AE.EMAIL IS NOT NULL
              AND IG.NAME = (
                CASE
                  WHEN IT.ITEMGROUPID = 'SQ' THEN (
                    SELECT TOP 1 ITEMGROUPID 
                    FROM [frey\live].[DSL_AX40_SP1_LIVE].dbo.INVENTTABLE
                    WHERE ITEMID = REPLACE(IT.ITEMID, 'Q', 'I')
                    AND DATAAREAID = 'dv'
                  )
                  ELSE IT.ITEMGROUPID
                END 
              ) 
              AND AE.ID NOT IN (
                SELECT TOP 1 AT.EMAIL_RECID
                FROM APPROVE_TRANS AT
                WHERE AT.EMAIL_RECID = AE.ID
                AND B.ID = AT.BOM_RECID
              )
              GROUP BY 
              B.ID,
              AE.ID,
              AE.EMAIL_ORDER,
              IT.ITEMGROUPID,
              IT.DIMENSION
              ORDER BY B.ID, AE.EMAIL_ORDER ASC";

                        emailApprove = conn.Query<FetchEmailApproveModel>(queryItemQ, null, transaction).ToList();

                        if (emailApprove.Count == 0)
                        {
                            transaction.Rollback();
                            throw new Exception("No email to add.");
                        }

                        foreach (var email in emailApprove)
                        {

                            var checkDuplicate = conn.QueryFirstOrDefault<ApproveTransEmailRecModel>(@"
                SELECT EMAIL_RECID from APPROVE_TRANS
                WHERE BOM_RECID = @BOM_RECID
                AND EMAIL_RECID = @EMAIL_RECID
                AND EMAIL_ORDER = @EMAIL_ORDER
              ", new
                            {
                                @BOM_RECID = email.BOM_RECID,
                                @EMAIL_RECID = email.EMAIL_RECID,
                                @EMAIL_ORDER = email.EMAIL_ORDER
                            }, transaction);

                            if (checkDuplicate != null)
                            {
                                var deleteExistsEmailRecId = conn.Execute(@"
                    DELETE FROM APPROVE_TRANS
                    WHERE BOM_RECID = @BOM_RECID
                    AND EMAIL_RECID = @EMAIL_RECID
                    AND EMAIL_ORDER = @EMAIL_ORDER
                ", new
                                {
                                    @BOM_RECID = email.BOM_RECID,
                                    @EMAIL_RECID = email.EMAIL_RECID,
                                    @EMAIL_ORDER = email.EMAIL_ORDER
                                }, transaction);
                            }

                            var addEmail = conn.Execute(@"INSERT INTO APPROVE_TRANS(
                                    EMAIL_RECID, BOM_RECID, EMAIL_ORDER, CREATE_DATE) 
                                    VALUES(@EMAIL_RECID, @BOM_RECID, @EMAIL_ORDER, @CREATE_DATE)",
                                new { email.EMAIL_RECID, email.BOM_RECID, email.EMAIL_ORDER, CREATE_DATE = DateTime.Now },
                                transaction);

                            if (addEmail == -1)
                            {
                                transaction.Rollback();
                                break;
                                throw new Exception("Error add email.");
                            }
                        }

                        transaction.Commit();

                        return new ResponseModel
                        {
                            result = true,
                            message = "Generate success."
                        };
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ResponseModel ApproveBom(int bomRecId, int emailRecId, int transId, string bomId)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        var isFinalApprove = conn.Query(@"SELECT 
                            AE.EMAIL,
                            AE.COMPANY,
                            AE.EMAIL_ORDER,
                            AE.EMAIL_GROUP,
                            AE.CAN_APPROVE,
                            AE.NOTIFY_TO,
                            AE.IS_FINAL,
                            AE.ITEM_GROUP
                            FROM APPROVE_EMAIL AE 
                            WHERE AE.ID = @ID",
                            new { @ID = emailRecId },
                            transaction).ToList();

                        if (isFinalApprove.Count > 0 && isFinalApprove[0].IS_FINAL == 1)
                        {
                            var updateBomCompleteDate = conn.Execute(@"UPDATE BOM
                                SET COMPLETE_DATE = GETDATE(), STATUS = 2
                                WHERE ID = @ID",
                                new { @ID = bomRecId },
                                transaction);

                            if (updateBomCompleteDate == -1)
                            {
                                transaction.Rollback();
                                throw new Exception("Update bom complete failed.");
                            }
                        }

                        var updateEmailTrans = conn.Execute(@"UPDATE APPROVE_TRANS
                            SET APPROVE_DATE = GETDATE()
                            WHERE ID = @ID",
                            new { @ID = transId },
                            transaction);

                        if (updateEmailTrans == -1)
                        {
                            transaction.Rollback();
                            throw new Exception("Update email trans status failed.");
                        }

                        transaction.Commit();
                    }
                }

                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var axStatus = CheckAxStatusOfBom(bomRecId);
                    var status = 2;

                    if (axStatus == 1)
                    {
                        status = 2; // approve
                    }
                    else if (axStatus == 4)
                    {
                        status = 5; // cancel
                    }

                    var isFinal = conn.Query(@"SELECT 
                        AE.EMAIL,
                        AE.COMPANY,
                        AE.EMAIL_ORDER,
                        AE.EMAIL_GROUP,
                        AE.CAN_APPROVE,
                        AE.NOTIFY_TO,
                        AE.IS_FINAL,
                        AE.ITEM_GROUP
                        FROM APPROVE_EMAIL AE 
                        WHERE AE.ID = @ID",
                        new { @ID = emailRecId }).ToList();

                    if (isFinal.Count > 0 && isFinal[0].IS_FINAL == 1)
                    {
                        if (axStatus == 4) // cancel
                        {
                            var updateBom = conn.Execute(@"UPDATE [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION 
                            SET DSG_WaitMail = 0,
                            APPROVED = 0,
                            DSG_ProjectApproveBOM = @STATUS
                            WHERE BOMID = @ID",
                             new { @ID = bomId, @STATUS = status, @EMAIL_RECID = emailRecId });

                            if (updateBom == -1)
                            {
                                throw new Exception("Update bom failed.");
                            }
                        }
                        else // approve
                        {
                            var updateBom = conn.Execute(@"UPDATE [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION 
                            SET DSG_WaitMail = 0,
                            DSG_ProjectApproveBOM = @STATUS
                            WHERE BOMID = @ID",
                             new { @ID = bomId, @STATUS = status, @EMAIL_RECID = emailRecId });

                            if (updateBom == -1)
                            {
                                throw new Exception("Update bom failed.");
                            }


                        }
                    }
                }

                UpdateLog(bomId);

                return new ResponseModel
                {
                    result = true,
                    message = "Approve success."
                };
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ResponseModel RejectBom(int bomRecId, int emailRecId, int transId, string bomId)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        var isFinalApprove = conn.Query(@"SELECT 
                            AE.EMAIL,
                            AE.COMPANY,
                            AE.EMAIL_ORDER,
                            AE.EMAIL_GROUP,
                            AE.CAN_APPROVE,
                            AE.NOTIFY_TO,
                            AE.IS_FINAL,
                            AE.ITEM_GROUP
                            FROM APPROVE_EMAIL AE 
                            WHERE AE.ID = @ID",
                            new { @ID = emailRecId },
                            transaction).ToList();

                        if (isFinalApprove.Count > 0 && isFinalApprove[0].IS_FINAL == 1)
                        {
                            var updateBomCompleteDate = conn.Execute(@"UPDATE BOM
                                SET COMPLETE_DATE = GETDATE(), STATUS = 3
                                WHERE ID = @ID",
                                new { @ID = bomRecId },
                                transaction);

                            if (updateBomCompleteDate == -1)
                            {
                                transaction.Rollback();
                                throw new Exception("Update bom complete failed.");
                            }
                        }

                        var updateEmailTrans = conn.Execute(@"UPDATE APPROVE_TRANS
                            SET REJECT_DATE = GETDATE()
                            WHERE ID = @ID",
                            new { @ID = transId },
                            transaction);

                        if (updateEmailTrans == -1)
                        {
                            transaction.Rollback();
                            throw new Exception("Update email trans status failed.");
                        }

                        transaction.Commit();
                    }
                }

                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var updateBom = conn.Execute(@"UPDATE [frey\live].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION 
                        SET DSG_WaitMail = 0,
                        DSG_ProjectApproveBOM = 3
                        WHERE BOMID = @ID",
                        new { @ID = bomId });

                    if (updateBom == -1)
                    {
                        throw new Exception("Update bom failed.");
                    }
                }

                return new ResponseModel
                {
                    result = true,
                    message = "Reject success."
                };
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<object> GetListEmailSetup()
        {
            try
            {
                var sql = @"SELECT 
                    AE.ID,
                    AE.EMAIL,
                    AE.COMPANY,
                    AE.EMAIL_ORDER,
                    EG.NAME AS APPROVE_LEVEL,
                    AE.CAN_APPROVE,
                    AE2.EMAIL AS NOTIFY_TO,
                    AE.IS_FINAL,
                    AE.ACTIVE,
                    IG.NAME AS ITEM_GROUP,
                    AE.DIMENSION
                    FROM APPROVE_EMAIL AE 
                    LEFT JOIN EMAIL_GROUP EG ON AE.EMAIL_GROUP = EG.ID
                    LEFT JOIN APPROVE_EMAIL AE2 ON AE.NOTIFY_TO = AE2.ID
                    LEFT JOIN ITEM_GROUP IG ON AE.ITEM_GROUP = IG.ID
                    ORDER BY AE.ACTIVE DESC";

                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query(sql).ToList();
                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool IsFinalApprove(int emailRecId)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<IsFinalApproveModel>(@"SELECT IS_FINAL FROM APPROVE_EMAIL WHERE ID = @ID", new { @ID = emailRecId }).ToList();

                    if (rows[0].IS_FINAL == 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ResponseModel UpdateEmail(int id, string _field, string _value)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var result = conn.Execute(@"UPDATE APPROVE_EMAIL SET " + _field + " = @VALUE WHERE ID = @ID",
                        new { @VALUE = _value, @ID = id });

                    if (result == -1)
                    {
                        throw new Exception("Update error.");
                    }
                }

                return new ResponseModel
                {
                    result = true,
                    message = "Update success."
                };
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<NotifyToModel> GetNotifyToEmail()
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<NotifyToModel>(@"SELECT 
                        E.ID, 
                        E.EMAIL, 
                        E.COMPANY,
                        I.NAME AS ITEM_GROUP
                        FROM APPROVE_EMAIL E
                        LEFT JOIN ITEM_GROUP I ON I.ID = E.ITEM_GROUP
                        WHERE E.ACTIVE = 1
                        AND E.EMAIL_GROUP = 3
                        GROUP BY 
                        E.ID, 
                        E.EMAIL, 
                        E.COMPANY,
                        I.NAME
                        ORDER BY E.COMPANY, E.ID ASC",
                        null)
                        .ToList();
                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ResponseModel SaveRemark(string remark, int createBy, int bomRecId, bool approve)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    int isApprove;

                    if (approve == true)
                    {
                        isApprove = 1;
                    }
                    else
                    {
                        isApprove = 0;
                    }

                    var addRemark = conn.Execute(@"INSERT INTO REMARK(REMARK, CREATE_DATE, CREATE_BY, BOM_RECID, APPROVE) 
                        VALUES(@REMARK, @CREATE_DATE, @CREATE_BY, @BOM_RECID, @APPROVE)",
                        new { @REMARK = remark, @CREATE_DATE = DateTime.Now, @CREATE_BY = createBy, @BOM_RECID = bomRecId, @APPROVE = isApprove });

                    if (addRemark == -1)
                    {
                        throw new Exception("Add remark error.");
                    }

                    return new ResponseModel
                    {
                        result = true,
                        message = "Add remark success."
                    };
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<RemarkModel> GetRemarkByBom(int bomRecId)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<RemarkModel>(@"SELECT 
                        RM.ID,
                        RM.REMARK,
                        RM.CREATE_DATE,
                        B.BOMID,
                        AE.EMAIL,   
                        RM.APPROVE
                        FROM REMARK RM
                        LEFT JOIN APPROVE_EMAIL AE ON RM.CREATE_BY = AE.ID
                        LEFT JOIN BOM B ON B.ID = RM.BOM_RECID
                        WHERE RM.BOM_RECID = @ID
                        ORDER BY RM.CREATE_DATE ASC",
                        new { @ID = bomRecId }).ToList();

                    return rows;
                }
            }
            catch (Exception)
            {
                return new List<RemarkModel>();
            }
        }

        public int CheckAxStatusOfBom(int bomRecId)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query(@"SELECT AX_STATUS FROM BOM WHERE ID = @ID",
                        new { @ID = bomRecId })
                        .ToList();

                    if (rows.Count > 0)
                    {
                        return rows[0].AX_STATUS;
                    }
                    else
                    {
                        throw new Exception("BOM not found");
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<object> GetEmailGroup()
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query(@"SELECT * FROM EMAIL_GROUP").ToList();
                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ResponseModel GenerateTempSendEmail()
        {
            try
            {
                string sql = @"INSERT INTO TEMP_SENDMAIL(
                        EMAIL_RECID,
                        BOM_RECID,
                        CAN_APPROVE,
                        IS_FINAL,
                        NOTIFY_TO,
	                    BOMID,
	                    COMPANY,
	                    EMAIL,
	                    ITEM_GROUP
                    )
                    SELECT 
                    AE.ID AS EMAIL_RECID,
                    B.ID AS BOM_RECID,
                    AE.CAN_APPROVE,
                    AE.IS_FINAL,
                    AE.NOTIFY_TO,
                    B.BOMID,
                    B.COMPANY_REF AS COMPANY,
                    AE.EMAIL,
                    IT.NAME AS ITEM_GROUP
                    FROM BOM B 
                    LEFT JOIN APPROVE_TRANS AT 
	                    ON AT.BOM_RECID = B.ID
                    LEFT JOIN APPROVE_EMAIL AE 
	                    ON AE.ID = AT.EMAIL_RECID 
	                    AND AE.CAN_APPROVE = 1
	                    AND AE.EMAIL_GROUP <> 3
                    LEFT JOIN ITEM_GROUP IT 
	                    ON IT.ID = AE.ITEM_GROUP
                    WHERE B.STATUS IN (1, 4)
                    AND B.COMPLETE_DATE IS NULL
                    AND AE.EMAIL IS NOT NULL
                    AND AT.EMAIL_RECID = (
	                    SELECT TOP 1 AT3.EMAIL_RECID 
	                    FROM APPROVE_TRANS AT3
	                    WHERE AT3.BOM_RECID = B.ID
	                    AND APPROVE_DATE IS NULL
	                    AND REJECT_DATE IS NULL
                    ) 
                    AND AT.EMAIL_RECID NOT IN (
	                    SELECT 
	                    TM.EMAIL_RECID
	                    FROM TEMP_SENDMAIL TM
	                    WHERE TM.BOM_RECID = AT.BOM_RECID
	                    AND TM.EMAIL_RECID = AT.EMAIL_RECID
	                    AND TM.EMAIL = AE.EMAIL
	                    AND TM.COMPANY = B.COMPANY_REF
                    )
                    GROUP BY
                      AE.ID,
                      B.ID,
                      AE.CAN_APPROVE,
                      AE.IS_FINAL,
                      AE.NOTIFY_TO,
                      B.BOMID,
                      B.COMPANY_REF,
                      AE.EMAIL,
                      IT.NAME";

                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var insert = conn.Execute(sql);

                    if (insert == -1)
                    {
                        throw new Exception("Generate temp email error.");
                    }

                    return new ResponseModel
                    {
                        result = true,
                        message = "Generate success."
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

        public List<EmailForSendModel> GetEmailForSend()
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var email = conn.Query<EmailForSendModel>(@"SELECT 
                        TSM.COMPANY,
                        TSM.EMAIL,
                        E.EMPNAME,
                        TSM.ITEM_GROUP
                        FROM TEMP_SENDMAIL TSM
                        LEFT JOIN [HRTRAINING].[dbo].TEMPLOY1 T ON T.EMAIL = TSM.EMAIL
                        LEFT JOIN [HRTRAINING].[dbo].EMPLOYEE E ON E.CODEMPID = T.CODEMPID
                        WHERE TSM.SEND = 0
                        GROUP BY 
                        TSM.COMPANY,
                        E.EMPNAME,
                        TSM.EMAIL,
                        TSM.ITEM_GROUP").ToList();

                    return email;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<BomRecIdModel> GetBomRecIdFromEmailSendApprove(string email)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<BomRecIdModel>(@"
            SELECT BOM_RECID FROM TEMP_SENDMAIL
            WHERE EMAIL = @EMAIL
            AND SEND = 0",
                      new { @EMAIL = email }).ToList();

                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<TempBomDetailModel> GetBomListApproveDetail(string bomid)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<TempBomDetailModel>(@"SELECT 
                        TSM.ID,
                        AT.ID AS APPROVE_TRANS_ID,
                        TSM.BOM_RECID,
                        TSM.BOMID,
                        TSM.COMPANY,
                        TSM.EMAIL_RECID,
                        TSM.EMAIL,
                        TSM.ITEM_GROUP,
                        TSM.SEND,
                        TSM.CAN_APPROVE,
                        TSM.NOTIFY_TO,
                        IS_FINAL
                        FROM TEMP_SENDMAIL TSM
                        LEFT JOIN APPROVE_TRANS AT ON AT.BOM_RECID = TSM.BOM_RECID AND AT.EMAIL_RECID = TSM.EMAIL_RECID
                        WHERE TSM.SEND = 0
                        AND TSM.BOMID = @BOMID
                        GROUP BY
                        TSM.ID,
                        TSM.BOM_RECID,
                        TSM.BOMID,
                        TSM.COMPANY,
                        TSM.EMAIL_RECID,
                        TSM.EMAIL,
                        TSM.ITEM_GROUP,
                        TSM.SEND,
                        TSM.CAN_APPROVE,
                        TSM.NOTIFY_TO,
                        IS_FINAL,
                        AT.ID",
                        new { @BOMID = bomid }).ToList();

                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<TempBomDetailModel> GetBomListApprove(string email)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<TempBomDetailModel>(@"SELECT 
                        TSM.ID,
                        AT.ID AS APPROVE_TRANS_ID,
                        TSM.BOM_RECID,
                        TSM.BOMID,
                        TSM.COMPANY,
                        TSM.EMAIL_RECID,
                        TSM.EMAIL,
                        TSM.ITEM_GROUP,
                        TSM.SEND,
                        TSM.CAN_APPROVE,
                        TSM.NOTIFY_TO,
                        IS_FINAL
                        FROM TEMP_SENDMAIL TSM
                        LEFT JOIN APPROVE_TRANS AT ON AT.BOM_RECID = TSM.BOM_RECID AND AT.EMAIL_RECID = TSM.EMAIL_RECID
                        WHERE TSM.SEND = 0 AND TSM.EMAIL = @EMAIL
                        GROUP BY
                        TSM.ID,
                        TSM.BOM_RECID,
                        TSM.BOMID,
                        TSM.COMPANY,
                        TSM.EMAIL_RECID,
                        TSM.EMAIL,
                        TSM.ITEM_GROUP,
                        TSM.SEND,
                        TSM.CAN_APPROVE,
                        TSM.NOTIFY_TO,
                        IS_FINAL,
                        AT.ID
                        ",
                        new { @EMAIL = email }).ToList();

                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ResponseModel UpdateTemp(int id)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var updateTemp = conn.Execute(@"UPDATE TEMP_SENDMAIL SET SEND = 1 WHERE ID = @ID",
                        new { @ID = id });

                    if (updateTemp == -1)
                    {
                        throw new Exception("Update temp failed.");
                    }

                    return new ResponseModel
                    {
                        result = true,
                        message = "Update success."
                    };

                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<NotifyEmailModel> GetNotifyEmail(int bomRecId, bool notify)
        {
            try
            {
                string sql = "";

                if (notify == false)
                {
                    sql = @"SELECT 
                        E.EMPNAME,
                        AE.EMAIL
                        FROM APPROVE_TRANS AT
                        LEFT JOIN BOM B ON B.ID = AT.BOM_RECID
                        LEFT JOIN APPROVE_EMAIL AE ON AE.ID = AT.EMAIL_RECID
                        LEFT JOIN [HRTRAINING].[dbo].TEMPLOY1 T ON T.EMAIL = AE.EMAIL
                        LEFT JOIN [HRTRAINING].[dbo].EMPLOYEE E ON E.CODEMPID = T.CODEMPID AND E.STATUS = 3
                        WHERE B.ID = @BOM_RECID AND AE.IS_FINAL = 0
                        GROUP BY 
                        E.EMPNAME,
                        AE.EMAIL";
                }
                else
                {
                    sql = @"SELECT 
                        E.EMPNAME,
                        AE.EMAIL
                        FROM APPROVE_TRANS AT
                        LEFT JOIN BOM B ON B.ID = AT.BOM_RECID
                        LEFT JOIN APPROVE_EMAIL AE ON AE.ID = AT.EMAIL_RECID
                        LEFT JOIN [HRTRAINING].[dbo].TEMPLOY1 T ON T.EMAIL = AE.EMAIL
                        LEFT JOIN [HRTRAINING].[dbo].EMPLOYEE E ON E.CODEMPID = T.CODEMPID AND E.STATUS = 3
                        WHERE B.ID = @BOM_RECID AND AE.EMAIL_GROUP = 3
                        GROUP BY
                        E.EMPNAME,
                        AE.EMAIL";
                }

                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<NotifyEmailModel>(sql, new { @BOM_RECID = bomRecId }).ToList();
                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<BomApproveRemarkModel> GetBomRemark()
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<BomApproveRemarkModel>(@"SELECT 
                        TSM.BOM_RECID,
                        TSM.BOMID,
                        TSM.EMAIL,
                        E.EMPNAME + ' ' + E.EMPLASTNAME AS EMPNAME,
                        R.REMARK
                        FROM TEMP_SENDMAIL TSM
                        LEFT JOIN APPROVE_TRANS AT ON AT.BOM_RECID = TSM.BOM_RECID AND AT.EMAIL_RECID = TSM.EMAIL_RECID
                        LEFT JOIN REMARK R ON R.BOM_RECID = TSM.BOM_RECID
                        LEFT JOIN [HRTRAINING].[dbo].TEMPLOY1 T ON T.EMAIL = TSM.EMAIL
                        LEFT JOIN [HRTRAINING].[dbo].EMPLOYEE E ON E.CODEMPID = T.CODEMPID
                        WHERE 
                        TSM.SEND = 1 
                        GROUP BY
                        R.REMARK,
                        TSM.BOMID,
                        TSM.EMAIL,
                        TSM.BOM_RECID,
                        E.EMPNAME + ' ' + E.EMPLASTNAME").ToList();

                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<BomApproveRemarkModel> GetBomRemarkByBomId(int bomRecId)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<BomApproveRemarkModel>(@"SELECT 
                        R.REMARK,
                        AE.EMAIL,
                        R.APPROVE,
                        B.BOMID,
                        B.ID AS BOM_RECID,
                        B.AX_STATUS,
                        CASE 
	                        WHEN E.EMPNAME IS NULL OR E.EMPNAME = '' THEN AE.EMAIL
	                        ELSE E.EMPNAME + ' ' + E.EMPLASTNAME
                        END AS EMPNAME
                        FROM REMARK R
                        LEFT JOIN APPROVE_EMAIL AE ON AE.ID = R.CREATE_BY
                        LEFT JOIN BOM B ON B.ID = R.BOM_RECID
                        LEFT JOIN [HRTRAINING].[dbo].TEMPLOY1 T ON T.EMAIL = AE.EMAIL
                        LEFT JOIN [HRTRAINING].[dbo].EMPLOYEE E ON E.CODEMPID = T.CODEMPID
                        WHERE R.BOM_RECID = B.ID
                        AND E.STATUS <> 9
                        AND B.ID = @BOM_RECID",
                        new { @BOM_RECID = bomRecId }).ToList();

                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public string GetBomName(int bomRecId)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query(@"SELECT BOMNAME FROM BOM WHERE ID = @ID",
                        new { @ID = bomRecId }).ToList();

                    return rows[0].BOMNAME;
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        public string GetBomRemarkFromAx(string bomid)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.QueryFirstOrDefault(@"
            SELECT REMARK 
            FROM BOM 
            WHERE BOMID = @BOMID
            ORDER BY ID DESC",
                        new { @BOMID = bomid });

                    return rows.REMARK;
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        public List<BomListsModel> GetBomAll()
        {
            try
            {
                string sql = @"SELECT 
                    B.ID,
                    B.BOMID,
                    B.BOMNAME,
                    B.COMPANY_REF,
                    S.NAME AS STATUS,
                    B.CREATE_DATE,
                    B.UPDATE_DATE,
                    B.COMPLETE_DATE,
                    B.AX_STATUS
                    FROM BOM B 
                    LEFT JOIN APPROVE_STATUS S ON S.ID = B.STATUS
                    ORDER BY B.ID DESC";

                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<BomListsModel>(sql).ToList();
                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<ItemGroupModel> GetItemGroup()
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<ItemGroupModel>(@"SELECT ID, NAME FROM ITEM_GROUP").ToList();
                    return rows;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<BomApproveTransModel> GetBomApproveTrans(int bomRecId)
        {
            try
            {
                string sql = @"SELECT 
          B.ID,
          B.BOMID,
          AE.EMAIL,
          AT.EMAIL_ORDER,
          AT.CREATE_DATE,
          AT.SEND_DATE,
          AT.SEND_NOTIFY_DATE,
          AT.APPROVE_DATE,
          AT.REJECT_DATE
          FROM BOM B
          LEFT JOIN APPROVE_TRANS AT ON AT.BOM_RECID = B.ID
          LEFT JOIN APPROVE_EMAIL AE ON AE.ID = AT.EMAIL_RECID
          WHERE B.ID = @ID";

                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    var rows = conn.Query<BomApproveTransModel>(sql, new { @ID = bomRecId }).ToList();
                    return rows;
                }
            }
            catch (System.Exception)
            {

                throw;
            }
        }

        public void UpdateLog(string bomid)
        {
            try
            {
                using (var conn = new SqlConnection(Startup.ConnectionStrings("BOM")))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        var updateBomLog = conn.Execute(@"
              update BOM_LOG
              set IS_CURRENT = 0 
              WHERE BOMID = @BOMID
              AND IS_CURRENT = 1
            ", new { @BOMID = bomid }, transaction);

                        if (updateBomLog == -1)
                        {
                            transaction.Rollback();
                            throw new Exception("Update Bom Log Failed.");
                        }

                        var insertBomLog = conn.Execute(@"
              INSERT INTO BOM_LOG(
                TODATE,
                FROMDATE,
                ITEMID,
                BOMID,
                NAME,
                ACTIVE,
                APPROVED,
                APPROVEDBY,
                CONSTRUCTION,
                FROMQTY,
                DSG_STANDARD,
                DSG_ACTIVEPREV,
                MODIFIEDDATE,
                MODIFIEDTIME,
                MODIFIEDBY,
                DATAAREAID,
                RECVERSION,
                RECID,
                DSG_BOMCALCVERSION,
                DSG_OBSOLETEBOM,
                DSG_REFCOMPANYID,
                DSG_PROJECTAPPROVEBOM,
                DSG_WAITMAIL,
                IS_CURRENT
              )
              SELECT 
                BV.TODATE,
                BV.FROMDATE,
                BV.ITEMID,
                BV.BOMID,
                BV.NAME,
                BV.ACTIVE,
                BV.APPROVED,
                BV.APPROVEDBY,
                BV.CONSTRUCTION,
                BV.FROMQTY,
                BV.DSG_STANDARD,
                BV.DSG_ACTIVEPREV,
                BV.MODIFIEDDATE,
                BV.MODIFIEDTIME,
                BV.MODIFIEDBY,
                BV.DATAAREAID,
                BV.RECVERSION,
                BV.RECID,
                BV.DSG_BOMCALCVERSION,
                BV.DSG_OBSOLETEBOM,
                BV.DSG_REFCOMPANYID,
                BV.DSG_PROJECTAPPROVEBOM,
                BV.DSG_WAITMAIL, 
                1 AS IS_CURRENT 
              FROM [FREY\LIVE].[DSL_AX40_SP1_LIVE].dbo.BOMVERSION BV
              WHERE BV.BOMID = @BOMID
              AND BV.DATAAREAID = 'dv'
            ", new { @BOMID = bomid }, transaction);

                        if (insertBomLog == -1)
                        {
                            transaction.Rollback();
                            throw new Exception("Insert Bom Log Failed.");
                        }

                        var updateBomLineLog = conn.Execute(@"
              update BOMLINE_LOG
              set IS_CURRENT = 0 
              WHERE BOMID = @BOMID
              AND IS_CURRENT = 1
            ", new { @BOMID = bomid }, transaction);

                        if (updateBomLineLog == -1)
                        {
                            transaction.Rollback();
                            throw new Exception("Update Bom Line Log Failed.");
                        }

                        string sqlInsertBomLineLog = $@"
                INSERT INTO BOMLINE_LOG
                SELECT 
                LINENUM,
                BOMTYPE,
                BOMCONSUMP,
                ITEMID,
                BOMQTY,
                CALCULATION,
                DIM1,
                DIM2,
                DIM3,
                DIM4,
                DIM5,
                ROUNDUP,
                ROUNDUPQTY,
                POSITION,
                OPRNUM,
                FROMDATE,
                TODATE,
                VENDID,
                UNITID,
                BOMID,
                CONFIGGROUPID,
                FORMULA,
                BOMQTYSERIE,
                ITEMBOMID,
                ITEMROUTEID,
                INVENTDIMID,
                SCRAPVAR,
                SCRAPCONST,
                PRODFLUSHINGPRINCIP,
                ENDSCHEDCONSUMP,
                DATAAREAID,
                RECVERSION,
                RECID,
                DSG_PERCENTYIELD,
                --DSG_STATUSSENDTOMES
                --MODIFIEDDATE
                1 AS IS_CURRENT FROM [FREY\LIVE].[DSL_AX40_SP1_LIVE].dbo.BOM B
                WHERE B.BOMID = @BOMID
                AND B.DATAAREAID = 'dv'
            ";

                        var insertBomLineLog = conn.Execute(sqlInsertBomLineLog, new { @BOMID = bomid }, transaction);

                        if (insertBomLineLog == -1)
                        {
                            transaction.Rollback();
                            throw new Exception("Insert Bom Line Log Failed.");
                        }

                        transaction.Commit();
                    }
                }
            }
            catch (System.Exception)
            {

                throw;
            }
        }

        // end
    }
}
