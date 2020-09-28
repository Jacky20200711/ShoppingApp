using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShoppingApp.Data;
using ShoppingApp.Models;
using X.PagedList;

namespace ShoppingApp.Controllers
{
    public class UserController : Controller
    {
        //每個分頁最多顯示10筆
        private readonly int pageSize = 10;

        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly UserManager<IdentityUser> _userManager;

        // 注入修改會員的工具
        public UserController(
                ApplicationDbContext usertext, 
                ILogger<OrderFormController> logger, 
                UserManager<IdentityUser> userManager)
        {
            _context = usertext;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("404 not found");
            }

            return View(await _context.Users.ToPagedListAsync(page, pageSize));
        }

        public ActionResult Delete(string id)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("404 not found");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            // 令管理員不能刪除自己
            if (user.Email == Admin.name)
            {
                return Content("404 not found");
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult Edit(string id)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("404 not found");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(IdentityUser identityUser)
        {
            if (User.Identity.Name != Admin.name)
            {
                _logger.LogWarning($"[{User.Identity.Name}]企圖修改其他會員的密碼!");
                return Content("404 not found");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == identityUser.Email);

            if (user.Email == Admin.name)
            {
                return Content($"管理員[{User.Identity.Name}]不能編輯自己的密碼!");
            }

            // 若沒先 RemovePassword 則 LOG 會出現內建的 Warning
            await _userManager.RemovePasswordAsync(user);
            await _userManager.AddPasswordAsync(user, identityUser.PasswordHash);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SendVerifyEmail(IFormCollection post)
        {

            // 取出 POST 的資料並轉成字串，避免直接取用使得 LINQ 噴出錯誤
            string userEmail = post["email"];

            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);

            if(user != null)
            {
                // 從設定檔取得寄信的相關資訊
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");

                var config = builder.Build();

                string SmtpEmail = config["AppSetting:SmtpEmail"];
                string SmtpPassword = config["AppSetting:SmtpPassword"];
                string SmtpHost = config["AppSetting:SmtpHost"];

                // 取得隨機字串
                string newPassword = Path.GetRandomFileName();
                
                // 修改使用者的密碼
                await _userManager.RemovePasswordAsync(user);
                await _userManager.AddPasswordAsync(user, newPassword);

                // 寄信給該使用者
                MailMessage message = new MailMessage
                {
                    From = new MailAddress($"{SmtpEmail}", "阿貓購物網站", Encoding.UTF8),
                    SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8,
                    Subject = "阿貓購物網站-取得密碼的驗證信",
                    Body = $"您的密碼已經被重設為{newPassword}，請盡速登入並修改密碼。",
                    IsBodyHtml = true,
                };

                message.To.Add(post["email"]);

                SmtpClient smtp = new SmtpClient
                {
                    Port = 587,
                    Host = $"{SmtpHost}",
                };

                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential($"{SmtpEmail}", $"{SmtpPassword}");
                smtp.EnableSsl = true;
                smtp.Send(message);
            }

            return View("~/Areas/Identity/Pages/Account/ForgotPasswordConfirmation.cshtml");
        }
    }
}