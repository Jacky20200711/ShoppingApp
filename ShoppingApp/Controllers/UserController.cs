using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        // 使用 DI 注入會用到的工具
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;
        private readonly UserManager<IdentityUser> _userManager;

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
            if (!AuthorizeManager.inAdminGroup(User.Identity.Name)) return NotFound();

            return View(await _context.Users.ToPagedListAsync(page, pageSize));
        }

        public IActionResult Create()
        {
            if (!AuthorizeManager.inAdminGroup(User.Identity.Name)) return NotFound();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Email,PasswordHash")] IdentityUser identityUser)
        {
            if (!AuthorizeManager.inAdminGroup(User.Identity.Name)) return NotFound();

            // 這並不是用 Entity Framework 產生的 CRUD，所以要自行檢查欄位
            if (string.IsNullOrEmpty(identityUser.Email) ||
                string.IsNullOrEmpty(identityUser.PasswordHash) ||
                !Regex.IsMatch(identityUser.Email, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$") ||
                identityUser.PasswordHash.Length < 6)
            {
                ViewData["CreateUserError"] = "輸入資料錯誤!";
                return View();
            }

            var user = new IdentityUser { UserName = identityUser.Email, Email = identityUser.Email };

            // _userManager 會自動幫你檢查該郵件是否已被註冊，若是...則不會進行動作
            await _userManager.CreateAsync(user, identityUser.PasswordHash);

            _logger.LogInformation($"[{User.Identity.Name}]新增了用戶[{user.Email}]");

            return RedirectToAction("Index");
        }

        public ActionResult Delete(string id)
        {
            if (!AuthorizeManager.inAdminGroup(User.Identity.Name)) return NotFound();

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            // 令超級管理員不能被刪除
            if (user.Email == AuthorizeManager.SuperAdmin)
            {
                return NotFound();
            }

            // 查看該使用者是否為特權用戶，如果是...則從特權資料表和 HashTable 中移除
            var authorizedMember = _context.AuthorizedMember.FirstOrDefault(m => m.Email == user.Email);

            if(authorizedMember != null)
            {
                AuthorizeManager.updateHashTable(authorizedMember, "delete");
                _context.AuthorizedMember.Remove(authorizedMember);
                _context.SaveChanges();
            }

            // 刪除該使用者
            _context.Users.Remove(user);
            _context.SaveChanges();
            _logger.LogInformation($"[{User.Identity.Name}]刪除了用戶[{user.Email}]");
            return RedirectToAction("Index");
        }

        public ActionResult Edit(string id)
        {
            if (!AuthorizeManager.inAdminGroup(User.Identity.Name)) return NotFound();

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            // 令超級管理員不能被編輯
            if (user.Email == AuthorizeManager.SuperAdmin)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(IdentityUser identityUser)
        {
            if (!AuthorizeManager.inAdminGroup(User.Identity.Name))
            {
                _logger.LogWarning($"非管理員用戶[{User.Identity.Name}]企圖修改會員的資料!");
                return NotFound();
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == identityUser.Id);

            // 令超級管理員不能被編輯
            if (user.Email == AuthorizeManager.SuperAdmin)
            {
                return NotFound();
            }
            else
            {
                user.Email = identityUser.Email;
                user.UserName = identityUser.Email;
            }

            // 若沒先 RemovePassword 則 LOG 會出現內建的 Warning
            await _userManager.RemovePasswordAsync(user);
            await _userManager.AddPasswordAsync(user, identityUser.PasswordHash);
            _logger.LogInformation($"[{User.Identity.Name}]修改了[{user.Email}]的資料");
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
            _logger.LogInformation($"系統寄了新密碼給[{userEmail}]");
            return View("~/Areas/Identity/Pages/Account/ForgotPasswordConfirmation.cshtml");
        }
    }
}