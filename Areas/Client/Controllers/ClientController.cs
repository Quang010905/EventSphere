using EventSphere.Models.ModelViews;
using EventSphere.Models.Repositories;
using EventSphere.Service.Email;
using EventSphere.Service.Otp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EventSphere.Areas.Client.Controllers
{
    [Area("Client")]
    public class ClientController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly OtpSettings _otpSettings;
        public ClientController(IWebHostEnvironment webHostEnvironment, IOptions<OtpSettings> otpOptions)
        {
            _webHostEnvironment = webHostEnvironment;
            _otpSettings = otpOptions.Value;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Register()
        {
            return View();
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("UEmail");
            HttpContext.Session.Remove("UId");
            return RedirectToAction("Index", "Home", new {area = "Client"});
        }
        public ActionResult SaveItem(IFormFile upFile)
        {
            string? FullName = Request.Form["FullName"];
            string? Email = Request.Form["Email"];
            string? Password = Request.Form["Password"];
            string? Phone = Request.Form["Phone"];
            string? EnrollmentNo = Request.Form["EnrollmentNo"];
            string? Department = Request.Form["Department"];
            string normalizedEmail = UserRepository.Instance.NormalizeName(Email);
            string normalizedPhone = UserRepository.Instance.NormalizeName(Phone);
            string normalizedEnrollmentNo = UserRepository.Instance.NormalizeName(EnrollmentNo);
            if (UserRepository.Instance.checkEmail(normalizedEmail))
            {
                ViewBag.ErrorMessage = "Email already exists!";
                return View("Register");
            }
            if (UserRepository.Instance.checkPhone(normalizedPhone))
            {
                ViewBag.ErrorMessage = "Phone already exists!";
                return View("Register");
            }
            if (UserRepository.Instance.checkEnrollmentNo(normalizedEnrollmentNo))
            {
                ViewBag.ErrorMessage = "Enrollment No already exists!";
                return View("Register");
            }
            string fileName = "";
            string pathSave = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(pathSave);
            try
            {
                if (upFile != null && upFile.Length > 0)
                {
                    fileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(upFile.FileName)}";
                    string filePath = Path.Combine(pathSave, fileName);


                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        upFile.CopyTo(stream);
                    }
                }
                else
                {
                    fileName = "noimage.png";
                }

                var entity = new UserView
                {
                    Email = Email,
                    Password = Password,
                    UserDetail = new UserDetailView
                    {
                        FullName = FullName,
                        Image = fileName,
                        Department = Department,
                        EnrollmentNo = EnrollmentNo,
                        Phone = Phone
                    }
                };
                UserRepository.Instance.Add(entity);
            }
            catch (Exception)
            {

                throw;
            }
            return RedirectToAction("Login", "Client");
        }
        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public IActionResult vOtp(string email, string otp, [FromServices] IOtpService otpService)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
            {
                ViewBag.ErrorMessage = "Email or OTP cannot be null!";
                ViewBag.Email = email;
                return View("VerifyOtp");
            }

            string normalizedEmail = email.Trim().ToLower();
            string key = $"OTP_{normalizedEmail}";

            if (otpService.ValidateOtp(key, otp))
            {
                UserView? mv = UserRepository.Instance.GetUserByEmail(normalizedEmail);

                if (mv != null)
                {
                    HttpContext.Session.SetString("UEmail", mv.Email ?? "");
                    HttpContext.Session.SetInt32("UId", mv.Id);

                    otpService.RemoveOtp(key);

                    return mv.Role switch
                    {
                        0 => RedirectToAction("Index", "Admin", new { area = "Admin" }),
                        1 => RedirectToAction("Index", "Home", new { area = "Client" }),
                        2 => RedirectToAction("Index", "Organizer", new {area = "Organizer"}),
                        _ => RedirectToAction("Login", "Client")
                    };
                }
                else
                {
                    ViewBag.ErrorMessage = "Email or password is invalid!";
                }
            }
            else
            {
                ViewBag.ErrorMessage = "OTP invalid or time out!";
            }

            ViewBag.Email = email;
            return View("VerifyOtp");
        }

        public async Task<ActionResult> checkLogin([FromServices] IOtpService otpService, [FromServices] IEmailSender emailSender)
        {
            string? email = Request.Form["Email"];
            string? password = Request.Form["Password"];
            string hashedPassword = UserRepository.Instance.HashMD5(password);

            UserView? mv = UserRepository.Instance.checkLogin(email, hashedPassword);

            if (mv == null)
            {
                ViewBag.ErrorMessage = "Email or password is invalid!";
                return View("Login");
            }

            string normalizedEmail = email.Trim().ToLower();
            string otp = otpService.GenerateNumericOtp();
            string key = $"OTP_{normalizedEmail}";

            otpService.StoreOtp(key, otp, TimeSpan.FromMinutes(_otpSettings.ExpiryMinutes));


            await emailSender.SendEmailAsync(email, "Your OTP Code", $"Your OTP is: <b>{otp}</b>");

            return RedirectToAction("VerifyOtp", new { email });
        }

    }
}
