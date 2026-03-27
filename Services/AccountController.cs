using Microsoft.AspNetCore.Mvc;
using AcademicAnalytics.Models;

namespace AcademicAnalytics.Services
{
    public class AccountController : Controller
    {
        // Hardcoded faculty list
        private static Dictionary<string, string> users = new Dictionary<string, string>()
        {
            { "vinaya", "Vinaya#K7p2" },
            { "abhijit", "Abhijit#M4x9" },
            { "ram", "Ram#Q8z3" },
            { "monika", "Monika#L2v7" },
            { "satish", "Satish#R5n1" },
            { "neha", "Neha#T9k4" },
            { "harshal", "Harshal#B6y8" },
            { "arjun", "Qwerty@789" },
            { "stevina", "Stevina#D7q2" },
            { "prachi", "Prachi#H4m9" },
            { "neharam", "Neharam#J8t3" },
            { "sharvari", "Sharvari#F2x6" },
            { "richa", "Richa#N5z7" },
            { "sweedle", "Sweedle#C9p1" },
            { "anushree", "Anushree#V3k8" },
            { "monali", "Monali#G7r4" },
            { "savyasaachi", "Savyasaachi#X2w9" },
            { "pravin", "Pravin#L8y5" },
            { "leena", "Leena#M6q3" },
            { "chandrashekhar", "Chandra#Z4t7" }
        };

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginModel model)
        {
            if (users.ContainsKey(model.Username) && users[model.Username] == model.Password)
            {
                HttpContext.Session.SetString("User", model.Username);
                return RedirectToAction("Index", "Upload"); // your main page
            }

            ViewBag.Error = "Invalid credentials";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}