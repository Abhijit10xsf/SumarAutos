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

        public AccountController()
        {
            serviceLayer = new SAPRestServiceLayer();
        }

        public AccountController(SAPRestServiceLayer _serviceLayer)
        {
            serviceLayer = _serviceLayer;
        }

        public ActionResult Login()
        {
            if (Session["CurrentUser"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
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

                User currentUser = null;
                try
                {
                    IUserRepository userRepo = new UserRepository();
                    currentUser = userRepo.Authenticate(model.UserName, model.Password);
                }
                catch
                {
                }

                if (currentUser == null)
                {
                    int loggedUserId = Session["LoggedUserId"] != null ? Convert.ToInt32(Session["LoggedUserId"]) : 1;
                    currentUser = new User
                    {
                        Id = loggedUserId > 0 ? loggedUserId : 1,
                        Username = model.UserName,
                        Password = model.Password,
                        EmailId = ""
                    };
                }

                Session["CurrentUser"] = currentUser;

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
