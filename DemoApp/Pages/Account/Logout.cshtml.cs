using DemoApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DemoApp.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly ApiClient _apiClient;

    public LogoutModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var token = User.FindFirst("SessionToken")?.Value;

        if (!string.IsNullOrEmpty(token))
            await _apiClient.Logout(token);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Account/Login");
    }

    public IActionResult OnGet() => RedirectToPage("/Account/Login");
}
