using DemoApp.Services;
using Contracts.BindingModels;
using Contracts.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System.ComponentModel.DataAnnotations;
using Models.Enums;

namespace DemoApp.Pages.Users;

public class IndexModel : PageModel
{
    private readonly ApiClient _apiClient;
    private readonly IConfiguration _configuration;
    public const int PageSize = 8;

    public IndexModel(ApiClient apiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _configuration = configuration;
    }

    public List<UserViewModel> Users { get; set; } = [];
    public List<SelectListItem> RoleSelectList { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public bool HasNextPage { get; set; }
    public string? SearchTerm { get; set; }
    public bool IsSearchActive => !string.IsNullOrWhiteSpace(SearchTerm);
    public CertificateMode AppCertificateMode { get; private set; }

    [BindProperty]
    public UserInputModel NewUser { get; set; } = new();

    [BindProperty]
    public EditUserInputModel EditUser { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(int pageNumber = 1, string? search = null)
    {
        PageNumber = pageNumber;
        SearchTerm = search?.Trim();
        AppCertificateMode = _configuration.GetValue<CertificateMode>("CertificateMode");

        await LoadRolesAsync();

        Users = IsSearchActive
            ? await _apiClient.FilterUsers(SearchTerm!)
            : await _apiClient.GetUsersPaged(pageNumber, PageSize);

        HasNextPage = !IsSearchActive && Users.Count == PageSize;
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        await LoadRolesAsync();

        foreach (var key in ModelState.Keys.Where(k => !k.StartsWith("NewUser")).ToList())
            ModelState.Remove(key);

        if (!ModelState.IsValid)
        {
            Users = await _apiClient.GetUsersPaged(1, PageSize);
            HasNextPage = Users.Count == PageSize;
            PageNumber = 1;
            return Page();
        }

        var model = new UserBindingModel
        {
            Fullname = NewUser.Fullname.Trim(),
            Login = NewUser.Login.Trim(),
            Email = NewUser.Email.Trim(),
            RoleId = NewUser.RoleId,
            SystemRole = NewUser.SystemRole,
            Created = DateTime.UtcNow,
            IsActive = true
        };

        var (success, message) = await _apiClient.CreateUser(model);

        if (success)
            SuccessMessage = $"Пользователь «{model.Fullname}» успешно создан.";
        else
            ErrorMessage = message;

        return RedirectToPage(new { pageNumber = 1 });
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        // Clear validation errors from other BindProperty models on this page
        foreach (var key in ModelState.Keys.Where(k => !k.StartsWith("EditUser")).ToList())
            ModelState.Remove(key);

        if (!ModelState.IsValid)
        {
            ErrorMessage = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            return RedirectToPage(new { pageNumber = EditUser.PageNumber, search = EditUser.Search });
        }

        var model = new UserBindingModel
        {
            Id = EditUser.Id,
            Fullname = EditUser.Fullname.Trim(),
            Login = EditUser.Login.Trim(),
            Email = EditUser.Email.Trim(),
            CertificateId = EditUser.CertificateId,
            RoleId = EditUser.RoleId,
            SystemRole = EditUser.SystemRole,
            Created = EditUser.Created,
            IsActive = EditUser.IsActive
        };

        var (success, message) = await _apiClient.UpdateUser(model);

        if (success)
            SuccessMessage = $"Пользователь «{model.Fullname}» обновлён.";
        else
            ErrorMessage = message;

        return RedirectToPage(new { pageNumber = EditUser.PageNumber, search = EditUser.Search });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, int pageNumber = 1, string? search = null)
    {
        var (success, message) = await _apiClient.DeleteUser(id);

        if (success)
            SuccessMessage = "Пользователь деактивирован.";
        else
            ErrorMessage = message;

        return RedirectToPage(new { pageNumber, search });
    }

    public async Task<IActionResult> OnPostGenerateCertificateAsync(int id, int pageNumber = 1, string? search = null)
    {
        var (success, message) = await _apiClient.GenerateCertificate(id);

        if (success)
            SuccessMessage = "Сертификат успешно выпущен.";
        else
            ErrorMessage = $"Не удалось выпустить сертификат: {message}";

        return RedirectToPage(new { pageNumber, search });
    }

    public IActionResult OnPostSearch(string? search)
    {
        return RedirectToPage(new { search });
    }

    private async Task LoadRolesAsync()
    {
        var roles = await _apiClient.GetAllRoles();
        RoleSelectList = roles
            .Select(r => new SelectListItem(r.Name, r.Id.ToString()))
            .ToList();
        RoleSelectList.Insert(0, new SelectListItem("— Без роли —", "0"));
    }

    public class EditUserInputModel
    {
        public int Id { get; set; }
        public int PageNumber { get; set; } = 1;
        public string? Search { get; set; }
        public int CertificateId { get; set; }
        public DateTime Created { get; set; }

        [Required(ErrorMessage = "Введите ФИО")]
        [MaxLength(200)]
        [Display(Name = "ФИО")]
        public string Fullname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите логин")]
        [MaxLength(100)]
        [Display(Name = "Логин")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        [MaxLength(200)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Системная роль")]
        public SystemRole SystemRole { get; set; }

        [Display(Name = "Роль (подразделение)")]
        public int RoleId { get; set; }

        [Display(Name = "Активен")]
        public bool IsActive { get; set; }
    }

    public class UserInputModel
    {
        [Required(ErrorMessage = "Введите ФИО")]
        [MaxLength(200)]
        [Display(Name = "ФИО")]
        public string Fullname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите логин")]
        [MaxLength(100)]
        [Display(Name = "Логин")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        [MaxLength(200)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Системная роль")]
        public SystemRole SystemRole { get; set; } = SystemRole.DocumentManager;

        [Display(Name = "Роль (подразделение)")]
        public int RoleId { get; set; }
    }

    public static string SystemRoleLabel(SystemRole role) => role switch
    {
        SystemRole.SystemAdmin => "Системный администратор",
        SystemRole.DocumentManager => "Менеджер документов",
        SystemRole.Signer => "Подписант",
        _ => role.ToString()
    };

    public static string SystemRoleBadgeClass(SystemRole role) => role switch
    {
        SystemRole.SystemAdmin => "admin",
        SystemRole.DocumentManager => "manager",
        SystemRole.Signer => "signer",
        _ => "manager"
    };
}
