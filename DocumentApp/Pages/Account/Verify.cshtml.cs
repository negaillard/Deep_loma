using DocumentApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace DocumentApp.Pages.Account;

public class VerifyModel : PageModel
{
    private readonly ApiClient _apiClient;

    public VerifyModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [TempData]
    public string? PendingLogin { get; set; }

    [BindProperty]
    public string LoginInput { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Введите код")]
    [Display(Name = "Код")]
    public string Code { get; set; } = string.Empty;

    public string LoginDisplay => LoginInput;
    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Documents/Index");

        if (string.IsNullOrEmpty(PendingLogin))
            return RedirectToPage("/Account/Login");

        LoginInput = PendingLogin;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var (success, message, data) = await _apiClient.VerifyLogin(LoginInput, Code.Trim());

        if (!success || data == null)
        {
            ErrorMessage = message;
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, data.Login),
            new("UserId", data.UserId.ToString()),
            new("SystemRole", data.SystemRole.ToString()),
            new("SessionToken", data.SessionToken)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });

        return RedirectToPage("/Documents/Index");
    }
}
