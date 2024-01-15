using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deestone.Services;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Deestone.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Deestone.Controllers
{
    public class BomApiController : Controller
    {
        BomService bomService = new BomService();
        EmailService emailService = new EmailService();
        DataService dataService = new DataService();
        NonceService nonceService = new NonceService();

        private IConfiguration _configuration;

        public BomApiController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public JsonResult GenApproveFlow()
        {
            try
            {
                var generateApproveFlow = bomService.GenerateApproveFlow();

                if (generateApproveFlow.result == false)
                {
                    throw new Exception(generateApproveFlow.message);
                }

                return Json(generateApproveFlow);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
            
        }

        [HttpGet]
        public JsonResult GetBomFromAx()
        {
            try
            {
                var getBomFromAx = bomService.GetBomFromAx();

                if (getBomFromAx.result == false)
                {
                    throw new Exception(getBomFromAx.message);
                }

                //var generateApproveFlow = bomService.GenerateApproveFlow();

                //if (generateApproveFlow.result == false)
                //{
                //    throw new Exception(generateApproveFlow.message);
                //}

                //var generateEmail = GenerateTempSendMail();

                //if (generateEmail.result == false)
                //{
                //    throw new Exception(generateEmail.message);
                //}

                //var sendEmailApproveResult = SendEmailApprove_v2();

                //if (sendEmailApproveResult.result == false)
                //{
                //    throw new Exception(sendEmailApproveResult.message);
                //}

                return Json(new ResponseModel
                {
                    result = true,
                    message = "Get data success."
                });
            }
            catch (Exception ex)
            {
                return Json(new ResponseModel
                {
                    result = false,
                    message = ex.Message
                });
            }
        }

        [NonAction]
        public ResponseModel SendEmailApprove(
            int bomRecId = 0,
            bool resultBomStatus = false,
            bool resultBom = false,
            int notifyTo = 0,
            bool sendAll = false)
        {
            // bomReId != 0 คือ send email final approve
            // bomRecId == 0 หรือว่าง คือ ส่ง email approve ปรกติ

            // resultBom คือ complete bom นั้นแล้ว ไม่ว่าจะ approve หรือ reject
            try
            {
                var resultSendEmail = new List<string>();
                var emailLists = new List<GetEmailFromBomModel>();
                var emailToSendEmail = new List<GetEmailFromBomModel>();
                var bom = new List<GetBomSendEmailModel>();

                if (bomRecId != 0)
                {
                    bom = bomService.GetBomSendEmail(bomRecId);
                }
                else
                {
                    bom = bomService.GetBomSendEmail();
                }

                if (bom.Count == 0)
                {
                    return new ResponseModel
                    {
                        result = true,
                        message = "No bom to send an email."
                    };
                }

                foreach (var bomRow in bom)
                {
                    if (sendAll == true)
                    {
                        emailToSendEmail = bomService.GetEmailFromBom(bomRow.BOMID, bomRow.COMPANY_REF, notifyTo, true);
                    }
                    else
                    {
                        emailToSendEmail = bomService.GetEmailFromBom(bomRow.BOMID, bomRow.COMPANY_REF, notifyTo);
                    }


                    if (emailToSendEmail.Count > 0)
                    {
                        foreach (var email in emailToSendEmail)
                        {
                            emailLists.Add(email);
                        }
                    }
                }

                foreach (var email in emailLists)
                {
                    var emailBody = @"";

                    emailBody += @"<div 
                        style='font-size: 16px; 
                        font-family: Arial, Helvetica, sans-serif; 
                        font-weight: bold; 
                        padding: 10px;
                        margin-bottom: 10px;'>" + GetSubjectEmail(email.AX_STATUS, email.BOMID) + @"</div>";

                    emailBody += @"<div 
                        style='font-size: 16px; 
                        font-family: Arial, Helvetica, sans-serif; 
                        font-weight: bold; 
                        padding: 10px;'>รายละเอียดการเปลี่ยนแปลง</div>";

                    var bomVersionDetail = bomService.GetBomVersionDetail(email.BOMID);

                    foreach (var item in bomVersionDetail)
                    {
                        emailBody += @"<table
                          width='100%'
                          cellspacing='0'
                          cellpadding='5'
                          border='1'
                          style='font-size: 14px; font-family: Arial, Helvetica, sans-serif; margin-bottom: 30px; max-width: 500px;'
                        >
                          <thead style='background: #333333; color: #ffffff;'>
                            <th>DETAIL</th>
                            <th>OLD</th>
                            <th>NEW</th>
                          </thead>
                          <tbody>";

                        emailBody += @"<tr>
                              <td style='width: 30%;'>ITEM</td>
                              <td style='text-align: center;'>" + item.LOG_BOMITEMID + @"</td>
                              <td style='text-align: center;'>" + item.ITEMID + @"</td>
                            </tr>
                            <tr>
                              <td style='width: 30%;'>BOM QTY</td>
                              <td style='text-align: center;'>" + item.LOG_BOMQTY + @"</td>
                              <td style='text-align: center;'>" + item.BOMQTY + @"</td>
                            </tr>
                            <tr>
                              <td style='width: 30%;'>UNIT</td>
                              <td style='text-align: center;'>" + item.LOG_UNITID + @"</td>
                              <td style='text-align: center;'>" + item.UNITID + @"</td>
                            </tr>
                            <tr>
                              <td style='width: 30%;'>BOM QTY SERIES</td>
                              <td style='text-align: center;'>" + item.BOMQTYSERIE + @"</td>
                              <td style='text-align: center;'>" + item.PERSERIES + @"</td>
                            </tr>";

                        emailBody += @"</tbody></table>";
                    }

                    emailBody += @"<div 
                        style='font-size: 16px; 
                        font-family: Arial, Helvetica, sans-serif; 
                        font-weight: bold; 
                        padding: 10px;'>ไอเทมที่จะได้รับผลกระทบ</div>";

                    var itemRelate = bomService.GetItemRelateBomVersion(email.BOMID);

                    emailBody += @"<table
                      width='100%'
                      cellspacing='0'
                      cellpadding='5'
                      border='1'
                      style='font-size: 14px; font-family: Arial, Helvetica, sans-serif;
                      max-width: 500px;'
                    >
                      <thead style='background: #333333; color: #ffffff;'>
                        <th>ITEM ID</th>
                        <th>ITEM NAME</th>
                      </thead>
                      <tbody>";


                    foreach (var item in itemRelate)
                    {
                        emailBody += @"<tr>
                          <td style='text-align: center;'>" + item.ITEMID + @"</td>
                          <td> " + item.ITEM_NAME + @"</td>
                        </tr>";
                    }

                    emailBody += @"</tbody></table>";

                    var remarks = bomService.GetRemarkByBom(email.BOM_RECID);

                    if (remarks.Count > 0)
                    {
                        emailBody += @"<div style='text-align: left; 
                              font-size: 16px; 
                              margin: 30px 0px; 
                              max-width: 500px;
                              font-family: Arial, Helvetica, sans-serif; clear: both;'>
                              <div 
                                style='font-size: 16px; 
                                font-family: Arial, Helvetica, sans-serif; 
                                font-weight: bold; 
                                padding: 10px;'>หมายเหตุเพิ่มเติม</div>";

                        foreach (var item in remarks)
                        {
                            string approveText = "";

                            if (item.APPROVE == 1)
                            {
                                approveText = "<span style='background: green; color: white; font-weight: bold;'>อนุมัติ</span>";
                            }
                            else
                            {
                                approveText = "<span style='background: red; color: white; font-weight: bold;'>ไม่อนุมัติ</span>";
                            }

                            emailBody += @"<div style='font-size: 14px; 
                                                font-family: Arial, Helvetica, sans-serif; 
                                                padding: 10px; 
                                                border: 1px #cccccc solid; 
                                                margin: 10px 0px; clear: both;'>
                                <p>" + approveText + @" โดย <i>" + item.EMAIL + @"</i></p>
                                <p>" + item.REMARK + @"</p>
                              </div>";

                        }

                        emailBody += "</div>";

                    }

                    if (resultBomStatus == true)
                    {
                        if (resultBom == true)
                        {
                            emailBody += @"<div style='text-align: left; color: green; margin: 50px 0px;'>
                                    <h2>BOM VERSION " + email.BOMID + @" อนุมัติ.</h2>
                                </div>";
                        }
                        else
                        {
                            emailBody += @"<div style='text-align: left; color: red; margin: 50px 0px;'>
                                    <h2>BOM VERSION " + email.BOMID + @" ไม่อนุมัติ.</h2>
                                </div>";
                        }
                    }

                    // generate nonce
                    string nonce = nonceService.CreateNonce();

                    if (email.CAN_APPROVE == 1 && sendAll == false)
                    {
                        emailBody += @"<div style='text-align: left; margin: 50px 0px;'>
                              <a style='padding: 10px; 
                                background: green; 
                                color: white; 
                                font-weight: bold; 
                                border: none; 
                                text-decoration: none;
                                margin-right: 10px;' 
                                target='_blank' 
                                href='" + _configuration["CONFIG:BASE_URL"] + "/bom/submitresult?bom=" + email.BOM_RECID + @"&email=" + email.EMAIL_RECID + @"&trans=" + email.ID + @"&id=" + email.BOMID + @"&company=" + email.COMPANY_REF + @"&nonce=" + nonce + @"&approve=1'>อนุมัติ</a>
                              <a style='padding: 10px; 
                                background: red; 
                                color: white; 
                                font-weight: bold; 
                                text-decoration: none;
                                border: none;' 
                                target='_blank' 
                                href='" + _configuration["CONFIG:BASE_URL"] + "/bom/submitresult?bom=" + email.BOM_RECID + @"&email=" + email.EMAIL_RECID + @"&trans=" + email.ID + @"&id=" + email.BOMID + @"&company=" + email.COMPANY_REF + @"&nonce=" + nonce + @"&approve=0'>ไม่อนุมัติ</a>
                            </div>";
                    }

                    var sendEmail = emailService.SendEmail(
                        GetSubjectEmail(email.AX_STATUS, email.BOMID),
                        emailBody,
                        new List<string>() { email.EMAIL });

                    resultSendEmail.Add(sendEmail.result + " " + email.BOMID + " " + email.EMAIL + " " + sendEmail.message);

                    if (sendEmail.result == true)
                    {
                        if (notifyTo != 0)
                        {
                            var updateEmailStatus = bomService.UpdateEmailStatus(email.ID, email.BOM_RECID, true);
                        }
                        else
                        {
                            var updateEmailStatus = bomService.UpdateEmailStatus(email.ID, email.BOM_RECID);
                        }

                    }
                    else
                    {
                        throw new Exception(sendEmail.message);
                    }
                }

                return new ResponseModel
                {
                    result = true,
                    message = "Send email success.",
                    data = new UserDataModel { }
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
        public ResponseModel ApproveBOM(int bom, int email, int trans, string id, string company, string nonce, string remark, List<ListResultApproveModel> approveBomList)
        {
            try
            {
                var validateNonce = nonceService.ValidateNonce(nonce);

                if (validateNonce.result == false)
                {
                    throw new Exception("Nonce expired.");
                }

                Console.WriteLine("Approve bom " + bom + " email " + email + " transid " + trans + " bomid " + id);
                var approve = bomService.ApproveBom(bom, email, trans, id);

                if (approve.result == false)
                {
                    throw new Exception(approve.message);
                }

                var addRemark = bomService.SaveRemark(remark, email, bom, true);

                if (addRemark.result == false)
                {
                    throw new Exception(addRemark.message);
                }

                var updateNonce = nonceService.UpdateNonce(nonce, 1);

                if (updateNonce.result == false)
                {
                    throw new Exception(updateNonce.message);
                }

                return new ResponseModel
                {
                    result = true,
                    message = "Approve Bom success."
                };

                //return Redirect("/bom/result?result=1&bom=" + id);
            }
            catch (Exception ex)
            {
                return new ResponseModel
                {
                    result = false,
                    message = ex.Message
                };

                //return Ok("Error : " + ex.Message);
            }
        }

        [NonAction]
        public ResponseModel RejectBOM(int bom, int email, int trans, string id, string company, string nonce, string remark, List<ListResultApproveModel> rejectBomList)
        {
            try
            {
                if (nonceService.ValidateNonce(nonce).result == false)
                {
                    throw new Exception("Nonce expired.");
                }

                Console.WriteLine("Reject bom " + bom + " email " + email + " transid " + trans + " bomid " + id);
                var reject = bomService.RejectBom(bom, email, trans, id);

                if (reject.result == false)
                {
                    throw new Exception(reject.message);
                }

                var addRemark = bomService.SaveRemark(remark, email, bom, false);

                if (addRemark.result == false)
                {
                    throw new Exception(addRemark.message);
                }

                var updateNonce = nonceService.UpdateNonce(nonce, 1);

                if (updateNonce.result == false)
                {
                    throw new Exception(updateNonce.message);
                }

                //return Redirect("/bom/result?result=2&bom=" + id);

                return new ResponseModel
                {
                    result = true,
                    message = "Reject Bom success."
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

        [HttpPost]
        public IActionResult GetListEmailSetup()
        {
            try
            {
                var emailLists = bomService.GetListEmailSetup();

                return Json(new
                {
                    draw = 1,
                    recordsTotal = emailLists.Count,
                    recordsFiltered = emailLists.Count,
                    data = emailLists
                });
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        [NonAction]
        public string GetSubjectEmail(int axStatus, string bomid)
        {
            try
            {
                if (axStatus == 1)
                {
                    return "คำร้องขออนุมัติ BOM VERSION : " + bomid;
                }
                else if (axStatus == 4)
                {
                    return "คำร้องขอยกเลิก BOM VERSION : " + bomid;
                }
                else
                {
                    return "คำร้องขออนุมัติ BOM VERSION : " + bomid;
                }
            }
            catch (Exception)
            {
                return "คำร้องขออนุมัติ BOM Version : " + bomid;
            }
        }

        [HttpPost]
        public IActionResult UpdateEmail(GridUpdateModel _update)
        {
            try
            {
                var updateEmail = bomService.UpdateEmail(_update.pk, _update.name, _update.value);

                if (updateEmail.result == true)
                {
                    return Ok(new ResponseModel
                    {
                        result = updateEmail.result,
                        message = updateEmail.message,
                        data = new UserDataModel { }
                    });
                }
                else
                {
                    throw new Exception("Update error.");
                }
            }
            catch (Exception ex)
            {

                return Ok(new ResponseModel
                {
                    result = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public List<GetEmailApproveByCompanyModel> GetApproveEmail()
        {
            return new List<GetEmailApproveByCompanyModel>();
        }

        [HttpPost]
        public JsonResult GetEmailGroup()
        {
            try
            {
                var rows = bomService.GetEmailGroup();
                return Json(rows);
            }
            catch (Exception)
            {
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public ResponseModel SendEmailApprove_v2()
        {
            try
            {
                var date = DateTime.Now;

                if (date.Hour >= 8 && date.Hour <= 18)
                {
                    var emails = bomService.GetEmailForSend();

                    if (emails.Count == 0)
                    {
                        throw new Exception("No email to send.");
                    }

                    string nonce = nonceService.CreateNonce();

                    string emailBody = "";

                    foreach (var item in emails)
                    {
                        string tempEmail = item.EMAIL;
                        // worachart_s@deestone.com
                        // if (item.EMAIL == "snit@deestone.com")
                        // {
                        //   tempEmail = "worachart_s@deestone.com";
                        // }

                        emailBody = "";
                        emailBody += @"เรียนคุณ " + item.EMPNAME + "<br><br>";
                        emailBody += @"มีคำร้องขออนุมัติ BOM VERSION คลิกที่ลิงค์ด้านล่าง เพื่อดูรายละเอียด <br><br>";
                        emailBody += @"ลิงค์ภายใน : <a href='" + _configuration["CONFIG:BASE_URL"] + "/bom/approvelist?email=" + item.EMAIL + "&nonce=" + nonce + "'>คลิกที่นี่</a> <br>";
                        emailBody += @"ลิงค์ภายนอก : <a href='" + _configuration["CONFIG:EXTERNAL_URL"] + "/bom/approvelist?email=" + item.EMAIL + "&nonce=" + nonce + "'>คลิกที่นี่</a>";
                        var sendEmail = emailService.SendEmail(
                          "แจ้งเตือน คำขออนุมัติ BOM VERSION",
                          emailBody,
                          new List<string>() { tempEmail });
                    }
                }

                return new ResponseModel
                {
                    result = true,
                    message = "Send email success."
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

        [HttpGet("/test_email")]
        public ResponseModel SendEmailTest()
        {
            try
            {
                string nonce = "xxx";//nonceService.CreateNonce();

                string emailBody = "";

                List<EmailForSendModel> emails = new List<EmailForSendModel>();

                emails.Add(new EmailForSendModel { EMAIL = "wattana_r@deestone.com", EMPNAME = "Wattana" });

                foreach (var item in emails)
                {
                    emailBody = "";
                    emailBody += @"เรียนคุณ " + item.EMPNAME + "<br><br>";
                    emailBody += @"มีคำร้องขออนุมัติ BOM VERSION คลิกที่ลิงค์ด้านล่าง เพื่อดูรายละเอียด <br><br>";
                    emailBody += @"ลิงค์ภายใน : <a href='" + _configuration["CONFIG:BASE_URL"] + "/bom/approvelist?email=" + item.EMAIL + "&nonce=" + nonce + "'>คลิกที่นี่</a> <br>";
                    emailBody += @"ลิงค์ภายนอก : <a href='" + _configuration["CONFIG:EXTERNAL_URL"] + "/bom/approvelist?email=" + item.EMAIL + "&nonce=" + nonce + "'>คลิกที่นี่</a>";
                    var sendEmail = emailService.SendEmail(
                      "แจ้งเตือน คำขออนุมัติ BOM VERSION",
                      emailBody,
                      new List<string>() { item.EMAIL });
                }

                return new ResponseModel
                {
                    result = true,
                    message = "Send email success."
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
        public List<TempBomDetailModel> GetBomListApprove(string email)
        {
            try
            {
                var emailLists = bomService.GetBomListApprove(email);
                return emailLists;
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitApprove(IFormCollection data, string email, string nonce)
        {
            try
            {
                var listsApprove = new List<ListResultApproveModel>();
                var listsReason = new List<ListResultApproveModel>();

                foreach (var item in data)
                {
                    if (item.Key.Split("_")[0] == "approve")
                    {
                        if (Convert.ToInt32(item.Value[0]) == 0)
                        {
                            throw new Exception("กรุณาเลือกข้อมูลให้ครบถ้วน.");
                        }
                    }
                }

                foreach (var item in data)
                {
                    if (item.Key.Split("_")[0] == "approve")
                    {
                        if (Convert.ToInt32(item.Value[0]) == 0)
                        {
                            throw new Exception("กรุณาเลือกข้อมูลให้ครบถ้วน.");
                        }

                        listsApprove.Add(new ListResultApproveModel
                        {
                            BOMID = item.Key.Split("_")[1],
                            BOM_RECID = Convert.ToInt32(item.Key.Split("_")[2]),
                            REASON = "",
                            APPROVE = Convert.ToInt32(item.Value[0]),
                            APPROVE_TYPE = 1
                        });

                    }

                    if (item.Key.Split("_")[0] == "reason")
                    {
                        listsReason.Add(new ListResultApproveModel
                        {
                            BOMID = item.Key.Split("_")[1],
                            BOM_RECID = 0,
                            REASON = item.Value[0],
                            APPROVE = 0,
                            APPROVE_TYPE = 2
                        });
                    }
                }

                Console.WriteLine("BOM to approve is " + listsApprove.Count);

                int emailRecId = 0;
                int bomRecId = 0;
                string bomId = "";
                string company = "";

                foreach (var item in listsApprove)
                {
                    Console.WriteLine("BOMID " + item.BOMID + " Approve = " + item.APPROVE);
                    if (item.APPROVE_TYPE == 1 && item.APPROVE != 0)
                    {
                        Console.WriteLine("Getting BOM list approve : " + item.BOMID);
                        var bomDetail = bomService.GetBomListApproveDetail(item.BOMID);

                        emailRecId = bomDetail[0].EMAIL_RECID;
                        bomRecId = bomDetail[0].BOM_RECID;
                        bomId = bomDetail[0].BOMID;
                        company = bomDetail[0].COMPANY;

                        if (bomDetail.Count == 0)
                        {
                            throw new Exception("Bom " + item.BOMID + " approve detail not found.");
                        }

                        if (bomDetail[0].NOTIFY_TO != 0)
                        {
                            Console.WriteLine(item.BOMID + " has notify to.");
                            var updateEmailStatus = bomService.UpdateEmailStatus(bomDetail[0].APPROVE_TRANS_ID, bomDetail[0].BOM_RECID, true);

                            if (updateEmailStatus.result == false)
                            {
                                throw new Exception(updateEmailStatus.message);
                            }
                        }
                        else
                        {
                            Console.WriteLine(item.BOMID + " has no notify to.");
                            var updateEmailStatus = bomService.UpdateEmailStatus(bomDetail[0].APPROVE_TRANS_ID, bomDetail[0].BOM_RECID);

                            if (updateEmailStatus.result == false)
                            {
                                throw new Exception(updateEmailStatus.message);
                            }

                        }

                        // 1 = approve, 2 = reject
                        if (item.APPROVE == 1)
                        {
                            Console.WriteLine(item.BOMID + " is approved");
                            string reason = "";

                            foreach (var r in listsReason)
                            {
                                Console.WriteLine(" get reason from " + r.BOMID + " " + item.BOMID);
                                if (r.BOMID == item.BOMID)
                                {
                                    Console.WriteLine("Reason is " + r.REASON);
                                    reason = r.REASON;
                                }
                            }

                            Console.WriteLine(item.BOMID + " save data.");

                            string _nonce = nonceService.CreateNonce();

                            var approve = ApproveBOM(
                                bomDetail[0].BOM_RECID,
                                bomDetail[0].EMAIL_RECID,
                                bomDetail[0].APPROVE_TRANS_ID,
                                bomDetail[0].BOMID,
                                bomDetail[0].COMPANY,
                                _nonce,
                                reason,
                                listsApprove);

                            if (approve.result == false)
                            {
                                throw new Exception("Approve bom " + bomDetail[0].BOMID + " failed. => " + approve.message);
                            }

                            Console.WriteLine(item.BOMID + " update temp.");
                            var updateTemp = bomService.UpdateTemp(bomDetail[0].ID);

                            if (updateTemp.result == false)
                            {
                                throw new Exception(updateTemp.message);
                            }
                        }
                        else if (item.APPROVE == 2)
                        {
                            Console.WriteLine(item.BOMID + " is reject");
                            string reason = "";

                            foreach (var r in listsReason)
                            {
                                Console.WriteLine(" get reason from " + r.BOMID + " " + item.BOMID);
                                if (r.BOMID == item.BOMID)
                                {
                                    Console.WriteLine("Reason is " + r.REASON);
                                    reason = r.REASON;
                                }
                            }

                            Console.WriteLine(item.BOMID + " save data.");

                            string _nonce = nonceService.CreateNonce();

                            var reject = RejectBOM(
                                bomDetail[0].BOM_RECID,
                                bomDetail[0].EMAIL_RECID,
                                bomDetail[0].APPROVE_TRANS_ID,
                                bomDetail[0].BOMID,
                                bomDetail[0].COMPANY,
                                _nonce,
                                reason,
                                listsApprove);

                            if (reject.result == false)
                            {
                                throw new Exception("Reject bom " + bomDetail[0].BOMID + " failed. => " + reject.message);
                            }

                            var checkIsFinalApprove = bomService.IsFinalApprove(bomDetail[0].EMAIL_RECID);

                            if (checkIsFinalApprove == false)
                            {
                                var updateEmailStatus = bomService.UpdateEmailStatus(bomDetail[0].APPROVE_TRANS_ID, bomDetail[0].BOM_RECID, false, true);

                                if (updateEmailStatus.result == false)
                                {
                                    throw new Exception(updateEmailStatus.message);
                                }

                                var completeBom = bomService.ComplateBom(bomDetail[0].BOM_RECID, 3); // 3 = reject

                                if (completeBom.result == false)
                                {
                                    throw new Exception(completeBom.message);
                                }
                            }

                            Console.WriteLine(item.BOMID + " update temp.");
                            var updateTemp = bomService.UpdateTemp(bomDetail[0].ID);

                            if (updateTemp.result == false)
                            {
                                throw new Exception(updateTemp.message);
                            }
                        }
                    }
                }

                // send noti
                var isFinalApprove = bomService.IsFinalApprove(emailRecId);

                if (isFinalApprove == true)
                {
                    SendMailNotify(bomRecId, listsApprove);
                }
                else
                {
                    var isNextEmailFinalApprove = bomService.GetEmailFromBom(bomId, company);

                    if (isNextEmailFinalApprove.Count > 0)
                    {
                        if (isNextEmailFinalApprove[0].IS_FINAL == 1 && isNextEmailFinalApprove[0].NOTIFY_TO != 0)
                        {
                            Console.WriteLine("========== SEND NOTIFY 1 LEVEL ============");
                            SendMailNotify(bomRecId, listsApprove, true);
                        }
                    }
                }

                var updateNonce = nonceService.UpdateNonce(nonce, 1);

                if (updateNonce.result == false)
                {
                    throw new Exception(updateNonce.message);
                }

                var _data = new ResultViewModel
                {
                    result = true,
                    message = "บันทึกข้อมูลเสร็จสิ้น"
                };

                return View("Result", _data);

            }
            catch (Exception ex)
            {
                var _data = new ResultViewModel
                {
                    result = false,
                    message = ex.Message
                };

                return View("Result", _data);
            }
        }

        [HttpGet]
        public ResponseModel GenerateTempSendMail()
        {
            try
            {
                var gen = bomService.GenerateTempSendEmail();

                if (gen.result == false)
                {
                    throw new Exception("Generate temp failed.");
                }

                return new ResponseModel
                {
                    result = true,
                    message = "Generate temp email success."
                };
            }
            catch (Exception)
            {

                throw;
            }
        }

        public ResponseModel SendMailNotify(int bomRecId, List<ListResultApproveModel> bomLists, bool notify = false)
        {
            try
            {
                if (bomLists.Count > 0)
                {
                    var emails = bomService.GetNotifyEmail(bomRecId, notify);

                    string nonce = nonceService.CreateNonce();

                    string emailBody = "";

                    Console.WriteLine("Email for send notify is " + emails.Count);


                    foreach (var email in emails)
                    {
                        emailBody = "";

                        if (email.EMPNAME == null)
                        {
                            emailBody += @"เรียนคุณ " + email.EMAIL.Split("@")[0] + "<br><br>";
                        }
                        else
                        {
                            emailBody += @"เรียนคุณ " + email.EMPNAME + "<br><br>";
                        }

                        emailBody += @"รายละเอียดการเปลี่ยนแปลงแก้ไข BOM VERSION รายละเอียดด้านล่าง <br><br>";

                        int i = 0;

                        // internal
                        foreach (var bom in bomLists)
                        {
                            string _bomName = bomService.GetBomName(bom.BOM_RECID);

                            if (notify == true && bom.APPROVE == 1)
                            {
                                i++;
                                Console.WriteLine("BOMID = " + bom.BOMID + " BOM RECID = " + bom.BOM_RECID + " APPROVE = " + bom.APPROVE);
                                emailBody += "ลิ้งค์ภายใน : " + bom.BOMID + " (" + _bomName + ") <a href='" + _configuration["CONFIG:BASE_URL"] + "/bom/bomdetail?bomid=" + bom.BOMID + "&bid=" + bom.BOM_RECID + "'>รายละเอียด</a><br/>";
                            }
                            else if (notify == false)
                            {
                                i++;
                                emailBody += "ลิ้งค์ภายใน : " + bom.BOMID + " (" + _bomName + ") <a href='" + _configuration["CONFIG:BASE_URL"] + "/bom/bomdetail?bomid=" + bom.BOMID + "&bid=" + bom.BOM_RECID + "'>รายละเอียด</a><br/>";
                            }
                        }

                        emailBody += "<br/>";

                        // external
                        foreach (var bom in bomLists)
                        {
                            string _bomName = bomService.GetBomName(bom.BOM_RECID);

                            if (notify == true && bom.APPROVE == 1)
                            {
                                i++;
                                Console.WriteLine("BOMID = " + bom.BOMID + " BOM RECID = " + bom.BOM_RECID + " APPROVE = " + bom.APPROVE);
                                emailBody += "ลิ้งค์ภายนอก : " + bom.BOMID + " (" + _bomName + ") <a href='" + _configuration["CONFIG:EXTERNAL_URL"] + "/bom/bomdetail?bomid=" + bom.BOMID + "&bid=" + bom.BOM_RECID + "'>รายละเอียด</a><br/>";
                            }
                            else if (notify == false)
                            {
                                i++;
                                emailBody += "ลิ้งค์ภายนอก : " + bom.BOMID + " (" + _bomName + ") <a href='" + _configuration["CONFIG:EXTERNAL_URL"] + "/bom/bomdetail?bomid=" + bom.BOMID + "&bid=" + bom.BOM_RECID + "'>รายละเอียด</a><br/>";
                            }
                        }

                        if (i > 0)
                        {
                            var sendEmail = emailService.SendEmail(
                                 "แจ้งเตือนการเปลี่ยนแปลงแก้ไข BOM VERSION",
                                 emailBody,
                                 new List<string>() { email.EMAIL });

                            Console.WriteLine(sendEmail.message);
                        }
                    }

                    return new ResponseModel
                    {
                        result = true,
                        message = "Send email success."
                    };
                }
                else
                {
                    return new ResponseModel
                    {
                        result = true,
                        message = "No bom detail to send ."
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

        // end class
    }
}
