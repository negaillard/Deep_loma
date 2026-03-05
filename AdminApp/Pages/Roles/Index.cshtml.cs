using AdminApp.Services;
using Contracts.BindingModels;
using Contracts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace AdminApp.Pages.Roles;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApiClient _apiClient;
    public const int PageSize = 8;

    public IndexModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public List<RoleViewModel> Roles { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public bool HasNextPage { get; set; }

    [BindProperty]
    public RoleInputModel NewRole { get; set; } = new();

    [BindProperty]
    public EditRoleInputModel EditRole { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(int pageNumber = 1)
    {
        PageNumber = pageNumber;
        Roles = await _apiClient.GetRolesPaged(pageNumber, PageSize);
        HasNextPage = Roles.Count == PageSize;
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        foreach (var key in ModelState.Keys.Where(k => !k.StartsWith("NewRole")).ToList())
            ModelState.Remove(key);

        if (!ModelState.IsValid)
        {
            PageNumber = 1;
            Roles = await _apiClient.GetRolesPaged(1, PageSize);
            HasNextPage = Roles.Count == PageSize;
            return Page();
        }

        var model = new RoleBindingModel
        {
            Name = NewRole.Name.Trim(),
            Description = NewRole.Description?.Trim() ?? string.Empty
        };

        var (success, message) = await _apiClient.CreateRole(model);

        if (success)
            SuccessMessage = $"Роль «{model.Name}» успешно создана.";
        else
            ErrorMessage = message;

        return RedirectToPage(new { pageNumber = 1 });
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (string.IsNullOrWhiteSpace(EditRole.Name))
        {
            ErrorMessage = "Название роли не может быть пустым.";
            return RedirectToPage(new { pageNumber = PageNumber });
        }

        var model = new RoleBindingModel
        {
            Id = EditRole.Id,
            Name = EditRole.Name.Trim(),
            Description = EditRole.Description?.Trim() ?? string.Empty
        };

        var (success, message) = await _apiClient.UpdateRole(model);

        if (success)
            SuccessMessage = $"Роль «{model.Name}» обновлена.";
        else
            ErrorMessage = message;

        return RedirectToPage(new { pageNumber = EditRole.PageNumber });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, int pageNumber = 1)
    {
        var (success, message) = await _apiClient.DeleteRole(id);

        if (success)
            SuccessMessage = "Роль удалена.";
        else
            ErrorMessage = message;

        return RedirectToPage(new { pageNumber });
    }

    public class EditRoleInputModel
    {
        public int Id { get; set; }
        public int PageNumber { get; set; } = 1;

        [Required(ErrorMessage = "Введите название роли")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class RoleInputModel
    {
        [Required(ErrorMessage = "Введите название роли")]
        [MaxLength(100, ErrorMessage = "Не более 100 символов")]
        [Display(Name = "Название")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Не более 500 символов")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }
    }
}
