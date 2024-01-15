using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deestone.Services;
using Deestone.Models;

namespace Deestone.Controllers
{
  public class BomController : Controller
  {
    NonceService nonceService = new NonceService();
    BomService bomService = new BomService();
    AuthService authService = new AuthService();

    [HttpGet]
    public IActionResult SubmitResult(int bom, int email, int trans, string id, string company, string nonce, int approve)
    {
      try
      {
        ViewData["BomRedId"] = bom;
        ViewData["EmailRecId"] = email;
        ViewData["ApproveTransId"] = trans;
        ViewData["BomId"] = id;
        ViewData["Company"] = company;
        ViewData["Nonce"] = nonce;
        ViewData["isApprove"] = approve;

        if (approve == 1)
        {
          ViewData["Approve"] = "อนุมัติ";
        }
        else
        {
          ViewData["Approve"] = "ไม่อนุมัติ";
        }


        return View("SubmitResult");
      }
      catch (Exception ex)
      {
        return Ok(ex.Message);
      }
    }

    [HttpGet]
    public IActionResult Result(int result, string bom)
    {
      try
      {
        ViewData["bom"] = bom;

        string message = "";

        if (result == 1)
        {
          message += "อนุมัติ BOM VERSION : " + bom + " เสร็จสิ้น";
        }
        else if (result == 2)
        {
          message += "ไม่อนุมัติ BOM VERSION : " + bom + " เสร็จสิ้น";
        }
        else if (result == 3)
        {
          message = "รายการนี้ถูกดำเนินการไปแล้ว";
        }

        ViewData["message"] = message;

        return View("Result");
      }
      catch (Exception ex)
      {
        return Ok(ex.Message);
      }
    }

    [HttpGet]
    public IActionResult Index()
    {
      if (authService.IsLogin(HttpContext.Request).result == false)
      {
        return Redirect("/user/login");
      }

      return View("Bom");
    }

    [HttpGet]
    public IActionResult ApproveList(string email, string nonce)
    {
      try
      {
        var validateNonce = nonceService.ValidateNonce(nonce);

        if (validateNonce.result == false)
        {
          throw new Exception(validateNonce.message);
        }

        var approveList = bomService.GetBomListApprove(email);

        if (approveList.Count == 0)
        {
          throw new Exception("ไม่มีรายการ Approve.");
        }

        var bomRecId = bomService.GetBomRecIdFromEmailSendApprove(email);

        var bom = new ApproveListViewModel
        {
          BOMLISTAPPROVE = approveList,
          BOMREMARK = bomService.GetBomRemark(),
          EMAIL = email,
          NONCE = nonce,
          BOM_RECID = bomRecId
          // BOM_RECID = bomRecId[0].BOM_RECID
        };


        return View("ApproveList", bom);
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
    public IActionResult BomDetail(string bomid, int bid)
    {
      try
      {
        // return Json(bomService.GetBomVersionDetail_v3(bomid));
        var bom = new BomDetailViewModel
        {
          BOM_DETAIL = bomService.GetBomVersionDetail_v4(bomid),
          BOM_REMARK = bomService.GetBomRemarkByBomId(bid),
          REMARK = bomService.GetBomRemarkFromAx(bomid)
        };

        return View("BomDetail", bom);
      }
      catch (Exception ex)
      {
        return BadRequest(ex.Message);
      }

    }

    [HttpPost]
    public IActionResult GetBomLists()
    {
      try
      {
        return Ok("");
      }
      catch (Exception)
      {

        throw;
      }
    }

    [HttpPost]
    public JsonResult GetItemGroup()
    {
      try
      {
        var rows = bomService.GetItemGroup();
        return Json(rows);
      }
      catch (Exception ex)
      {
        return Json(ex.Message);
      }
    }

    [HttpPost]
    public JsonResult GetNotifyToEmail()
    {
      try
      {
        var rows = bomService.GetNotifyToEmail();
        return Json(rows);
      }
      catch (System.Exception)
      {

        throw;
      }
    }

    [HttpPost]
    public JsonResult GetBomAll()
    {
      try
      {
        var rows = bomService.GetBomAll();
        return Json(new
        {
          draw = 1,
          recordsTotal = rows.Count,
          recordsFiltered = rows.Count,
          data = rows
        });
      }
      catch (System.Exception)
      {

        throw;
      }
    }

    [HttpPost]
    public JsonResult GetBomApproveTrans(int id)
    {
      try
      {
        var rows = bomService.GetBomApproveTrans(id);
        return Json(new
        {
          draw = 1,
          recordsTotal = rows.Count,
          recordsFiltered = rows.Count,
          data = rows
        });
      }
      catch (System.Exception)
      {

        throw;
      }
    }

    // end
  }
}
