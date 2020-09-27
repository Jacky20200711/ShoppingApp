using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ShoppingApp.Data;
using ShoppingApp.Models;
using X.PagedList;

namespace ShoppingApp.Controllers
{
    public class UserController : Controller
    {
        //每個分頁最多顯示10筆
        private readonly int pageSize = 10;

        private readonly ApplicationDbContext _usertext;

        public UserController(ApplicationDbContext usertext)
        {
            _usertext = usertext;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("<h2>404 not found</h2>");
            }

            return View(await _usertext.Users.ToPagedListAsync(page, pageSize));
        }

        public ActionResult Delete(string id)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("<h2>404 not found</h2>");
            }

            var user = _usertext.Users.Where(u => u.Id == id).FirstOrDefault();

            // 令管理員不能刪除自己
            if (user.Email == Admin.name)
            {
                return Content("<h2>404 not found</h2>");
            }

            _usertext.Users.Remove(user);
            _usertext.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult Edit(string id)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("<h2>404 not found</h2>");
            }

            var user = _usertext.Users.Where(u => u.Id == id).FirstOrDefault();

            return View(user);
        }

        [HttpPost]
        public ActionResult Edit(IdentityUser identityUser)
        {
            if (User.Identity.Name != Admin.name)
            {
                return Content("<h2>404 not found</h2>");
            }

            var user = _usertext.Users.Where(u => u.Id == identityUser.Id).FirstOrDefault();

            // 令管理員不能編輯自己
            if (user.Email == Admin.name)
            {
                return Content("<h2>404 not found</h2>");
            }

            PasswordHasher<IdentityUser> PwHasher = new PasswordHasher<IdentityUser>();

            user.Email = identityUser.Email;
            user.PasswordHash = PwHasher.HashPassword(user, identityUser.PasswordHash);

            _usertext.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult SendVerifyEmail(IFormCollection post)
        {

            // 取出 POST 的資料並轉成字串，避免直接取用使得 LINQ 噴出錯誤
            string userEmail = post["email"];

            var user = _usertext.Users.FirstOrDefault(u => u.Email == userEmail);

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

                // 修改該使用者的密碼
                PasswordHasher<IdentityUser> PwHasher = new PasswordHasher<IdentityUser>();
                user.PasswordHash = PwHasher.HashPassword(user, newPassword);
                _usertext.SaveChanges();

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