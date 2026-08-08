using System;
using System.Web.Mvc;
using SumarAuto.Data.Interfaces;
using SumarAuto.Data.Entities;
using SumarAuto.Data.Repositories;

namespace SumarAuto.Client.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;

        public AccountController()
        {
            _userRepository = new UserRepository();
            _orderRepository = new OrderRepository();
        }

        public AccountController(IUserRepository userRepository, IOrderRepository orderRepository)
        {
            _userRepository = userRepository;
            _orderRepository = orderRepository;
        }

        [HttpGet]
        public ActionResult Details()
        {
            if (Session["CurrentUser"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = Session["CurrentUser"] as User ?? _userRepository.GetUserById(1);
            var orders = _orderRepository.GetOrdersByUserId(user.Id);

            ViewBag.Title = "B2B Account Details - TradeParts";
            ViewBag.Orders = orders;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateDetails(string contactPerson, string phone, string city, string trnNumber, string tradeLicenseNumber)
        {
            if (Session["CurrentUser"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = Session["CurrentUser"] as User;
            if (currentUser != null)
            {
                currentUser.ContactPerson = contactPerson;
                currentUser.Phone = phone;
                currentUser.City = city;
                currentUser.TrnNumber = trnNumber;
                currentUser.TradeLicenseNumber = tradeLicenseNumber;
                Session["CurrentUser"] = currentUser;

                TempData["SuccessMessage"] = "Your account details have been updated successfully.";
            }

            return RedirectToAction("Details");
        }

        [HttpGet]
        public ActionResult Login(string returnUrl = null)
        {
            ViewBag.Title = "B2B Sign In - TradeParts";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string emailOrAccount, string password, bool rememberMe = false, string returnUrl = null)
        {
            ViewBag.Title = "B2B Sign In - TradeParts";

            if (string.IsNullOrWhiteSpace(emailOrAccount) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.ErrorMessage = "Please provide both Email/Account ID and Password.";
                return View();
            }

            var user = _userRepository.Authenticate(emailOrAccount, password);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Invalid credentials or inactive B2B account.";
                return View();
            }

            Session["CurrentUser"] = user;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult Register()
        {
            ViewBag.Title = "B2B Dealer Registration - TradeParts";
            return View(new User());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(User model, string confirmPassword)
        {
            ViewBag.Title = "B2B Dealer Registration - TradeParts";

            if (string.IsNullOrWhiteSpace(model.CompanyName) || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.ErrorMessage = "Please complete all required fields.";
                return View(model);
            }

            if (model.Password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                return View(model);
            }

            string errorMsg;
            if (_userRepository.Register(model, out errorMsg))
            {
                var user = _userRepository.GetUserByEmail(model.Email);
                Session["CurrentUser"] = user;
                TempData["WelcomeMessage"] = "Your wholesale account has been successfully created!";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.ErrorMessage = errorMsg;
                return View(model);
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
