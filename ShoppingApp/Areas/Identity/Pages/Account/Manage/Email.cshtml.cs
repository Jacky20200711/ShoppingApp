using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using ShoppingApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ShoppingApp.Areas.Identity.Pages.Account.Manage
{
    public partial class EmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        // 使用 DI 注入資料庫
        public EmailModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IEmailSender emailSender,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
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
            [EmailAddress(ErrorMessage = "郵件格式錯誤")]
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
                    string oldEmail = email.ToString();

                    var oldUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == oldEmail);

                    oldUser.Email = Input.NewEmail;
                    oldUser.UserName = Input.NewEmail;

                    await _context.SaveChangesAsync();
                    StatusMessage = $"您的郵件已經變更成[{Input.NewEmail}]，請重新登入以利操作。";
                }
                else
                {
                    StatusMessage = "郵件變更失敗，此郵件已被註冊!";
                }

                return RedirectToPage();
            }

            StatusMessage = "您的郵件和之前的一樣!";
            return RedirectToPage();
        }
    }
}
