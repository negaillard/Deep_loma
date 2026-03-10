using DocumentApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace DocumentApp.Pages.Account;

public class LoginModel : PageModel
{
    private readonly ApiClient _apiClient;

    public LoginModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    [Required(ErrorMessage = "Введите логин")]
    [Display(Name = "Логин")]
    public string LoginInput { get; set; } = string.Empty;

    [TempData]
    public string? PendingLogin { get; set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Documents/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var (success, message) = await _apiClient.SendLoginCode(LoginInput.Trim());

        if (!success)
        {
            ErrorMessage = message;
            return Page();
        }

        PendingLogin = LoginInput.Trim();
        return RedirectToPage("/Account/Verify");
    }
}
