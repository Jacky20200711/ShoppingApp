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
        // 每個分頁最多顯示10筆
        private readonly int pageSize = 10;

        // 注入會用到的工具
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
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            return View(await _context.Users.ToPagedListAsync(page, pageSize));
        }

        public IActionResult Create()
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Email,PasswordHash")] IdentityUser identityUser)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

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
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            var user = _context.Users.FirstOrDefault(u => u.Id == id);

            // 令超級管理員不能被刪除
            if (user.Email == AuthorizeManager.SuperAdmin)
            {
                return NotFound();
            }

            // 查看該使用者是否為特權用戶，如果是...則從特權資料表和 HashTable 中移除
            if(AuthorizeManager.InAuthorizedMember(user.Email))
            {
                AuthorizeManager.UpdateAuthority("DeleteAll", _context, user.Email, null, null);
            }

            // 刪除該使用者
            _context.Users.Remove(user);
            _context.SaveChanges();
            _logger.LogWarning($"[{User.Identity.Name}]刪除了用戶[{user.Email}]");
            return RedirectToAction("Index");
        }

        public ActionResult Edit(string id)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

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
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            var user = _context.Users.FirstOrDefault(u => u.Id == identityUser.Id);

            // 令超級管理員不能被編輯
            if (user.Email == AuthorizeManager.SuperAdmin)
            {
                return NotFound();
            }
            else
            {
                // 如果是特權用戶，則變更此特權用戶的郵件
                if(AuthorizeManager.InAuthorizedMember(user.Email))
                {
                    AuthorizeManager.UpdateAuthority("ModifyEmail", _context, user.Email, identityUser.Email);
                }

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
        public IActionResult SendVerifyEmail(IFormCollection post)
        {
            // 檢查這個IP的寄送次數
            string ClientIP = HttpContext.Connection.RemoteIpAddress.ToString();
            if (EmailKeyManager.GetSendCountByIP(ClientIP) > 2)
            {
                TempData["ForgotPasswordConfirmation"] = "您的寄送次數已達上限，請聯絡網站的管理員!";
                return View("~/Areas/Identity/Pages/Account/ForgotPasswordConfirmation.cshtml");
            }
            else
            {
                EmailKeyManager.IncrementCount(ClientIP);
            }

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
                string MyAppDomain = config["AppSetting:MyAppDomain"];

                // 取得隨機字串並存入記憶體
                string emailVerifyKey = Path.GetRandomFileName();
                EmailKeyManager.AddVerifyKey(emailVerifyKey, user.Email);

                // 寄信給該郵件
                MailMessage message = new MailMessage
                {
                    From = new MailAddress($"{SmtpEmail}", "阿貓購物網站", Encoding.UTF8),
                    SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8,
                    Subject = "阿貓購物網站-取得新密碼的驗證信",
                    Body = $"請點開此連結以取得新密碼{MyAppDomain}/User/VerifyEmailKey/?key={emailVerifyKey}",
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
            _logger.LogInformation($"系統寄送了新密碼的驗證信給[{userEmail}]");
            TempData["ForgotPasswordConfirmation"] = "請查看您的 Email 以取得新密碼!";
            return View("~/Areas/Identity/Pages/Account/ForgotPasswordConfirmation.cshtml");
        }

        public async Task<IActionResult> VerifyEmailKey(string key="")
        {
            if (EmailKeyManager.IsValidVerifyKey(key))
            {
                string email = EmailKeyManager.GetEmailByVerifyKey(key);
                var user = _context.Users.FirstOrDefault(u => u.Email == email);

                // 取得隨機字串
                string newPassword = Path.GetRandomFileName();

                // 修改使用者的密碼
                await _userManager.RemovePasswordAsync(user);
                await _userManager.AddPasswordAsync(user, newPassword);

                // 令這個 Key 只能使用一次
                EmailKeyManager.RemoveVerifyKey(key);

                _logger.LogInformation($"系統將[{user.Email}]的密碼修改為[{newPassword}]");
                TempData["ForgotPasswordConfirmation"] = $"您的密碼已經被重設為{newPassword}，請盡速登入並修改密碼!";
                return View("~/Areas/Identity/Pages/Account/ForgotPasswordConfirmation.cshtml");
            }
            else
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> DeleteAll()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            // 清空權限 & 會員 & 上架的資料表 
            _context.RemoveRange(_context.AuthorizedMember.Where(m => m.Email != AuthorizeManager.SuperAdmin));
            _context.RemoveRange(_context.Users.Where(m => m.Email != AuthorizeManager.SuperAdmin));
            _context.RemoveRange(_context.Product2);
            await _context.SaveChangesAsync();

            // 刷新權限的HashTable
            AuthorizeManager.RefreshHashTable(_context);

            return RedirectToAction(nameof(Index));
        }
    }
}