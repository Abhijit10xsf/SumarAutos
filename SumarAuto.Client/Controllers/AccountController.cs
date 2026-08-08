using SumarAuto.Client.Models;
using SumarAuto.Data.Entities;
using SumarAuto.Data.Interfaces;
using SumarAuto.Data.Repositories;
using System;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace SumarAuto.Client.Controllers
{
    public class AccountController : Controller
    {
        SAPRestServiceLayer serviceLayer;

        public AccountController(SAPRestServiceLayer _serviceLayer)
        {
            serviceLayer = _serviceLayer;
        }

        public ActionResult Login()
        {
            return View(new LoginVM());
        }

        public ActionResult Logout()
        {

            Session.Clear();
            Session.Abandon();
            System.Web.Security.FormsAuthentication.SignOut();

            return RedirectToAction("Login", "Account");
        }
        [HttpPost]
        public JsonResult Login(LoginVM model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.UserName) ||
                    string.IsNullOrEmpty(model.Password))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Enter username and password"
                    });
                }

                if (!serviceLayer.ValidateUserCredentials(model))
                {
                    return Json(new
                    {
                        success = false,
                        message = Utilities.GetResultMessage()
                    });
                }

                Session["User"] = model.UserName;
                Session["TempUser"] = model.UserName;
                Session["TempPassword"] = model.Password;

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

    }
}
