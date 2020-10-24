using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ShoppingApp.Data;
using Microsoft.Extensions.Logging;
using ShoppingApp.Models;
using Microsoft.AspNetCore.Http;

namespace ShoppingApp.Areas.Identity.Pages.Account.Manage
{
    public partial class EmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _context;

        // 注入會用到的工具
        public EmailModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IEmailSender emailSender,
            ILogger<EmailModel> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _context = context;
        }

        public string Username { get; set; }

        public string Email { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "此欄位不能為空")]
            [EmailAddress(ErrorMessage = "請務必確認郵件格式")]
            [Display(Name = "新郵件")]
            public string NewEmail { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            Input = new InputModel
            {
                NewEmail = email,
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != email)
            {
                var GetUserByEmail = _context.Users.FirstOrDefault(u => u.Email == Input.NewEmail);

                // 若此信箱沒有被註冊過，則允許修改信箱
                if(GetUserByEmail == null)
                {
                    // 檢查是否為特權用戶
                    if(AuthorizeManager.InAuthorizedMember(user.Email))
                    {
                        AuthorizeManager.UpdateAuthority("ModifyEmail", _context, user.Email, Input.NewEmail);
                    }

                    // 變更郵件
                    user.UserName = Input.NewEmail;
                    user.Email = Input.NewEmail;
                    await _userManager.UpdateAsync(user);

                    // 令使用者登出
                    _logger.LogInformation($"[{email}]的郵件已經變更為[{Input.NewEmail}]");
                    TempData["LoginFail"] = $"郵件變更成功，請重新登入!";
                    await _signInManager.SignOutAsync();
                    HttpContext.Session.SetString("UserModifyEmail", "1");
                }
                else
                {
                    StatusMessage = "變更失敗，此郵件已被註冊!";
                }

                return RedirectToPage();
            }

            StatusMessage = "您的郵件和之前的一樣!";
            return RedirectToPage();
        }
    }
}
