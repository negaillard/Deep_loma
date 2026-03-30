using DocumentApp.Services;
using Contracts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;

namespace DocumentApp.Pages.Documents;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApiClient _apiClient;

    public IndexModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public List<DocumentViewModel> Documents { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage => PageNumber < TotalPages;

    // Upload form properties
    [BindProperty]
    public string Title { get; set; } = string.Empty;

    [BindProperty]
    public string Description { get; set; } = string.Empty;

    [BindProperty]
    public List<int> SignerIds { get; set; } = [];

    [BindProperty]
    public bool IsSequential { get; set; }

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    public string? StatusFilter { get; set; }

    public static string StatusLabel(DocumentStatus status) => status switch
    {
        DocumentStatus.NOT_SIGNED   => "Ожидает подписи",
        DocumentStatus.PARTLY_SIGNED => "Частично подписан",
        DocumentStatus.SIGNED       => "Подписан",
        DocumentStatus.DECLINED     => "Отклонён",
        _ => status.ToString()
    };

    public static string StatusBadgeClass(DocumentStatus status) => status switch
    {
        DocumentStatus.NOT_SIGNED    => "not-signed",
        DocumentStatus.PARTLY_SIGNED => "partly",
        DocumentStatus.SIGNED        => "signed",
        DocumentStatus.DECLINED      => "declined",
        _ => ""
    };

    public static string StatusIcon(DocumentStatus status) => status switch
    {
        DocumentStatus.NOT_SIGNED    => "bi-hourglass-split",
        DocumentStatus.PARTLY_SIGNED => "bi-hourglass-top",
        DocumentStatus.SIGNED        => "bi-check-circle",
        DocumentStatus.DECLINED      => "bi-x-circle",
        _ => "bi-file-earmark"
    };

    /// <param name="p"></param>
    public async Task<IActionResult> OnGetAsync(string? statusFilter = null, int p = 1, int pageSize = 10)
    {
        if (p < 1) p = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;
        StatusFilter = statusFilter;
        PageNumber = p;
        PageSize = pageSize;
        await LoadDocumentsAsync(statusFilter, p, pageSize);
        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            TempData["ErrorMessage"] = "Введите название документа";
            await LoadDocumentsAsync(null, 1, PageSize);
            return Page();
        }

        if (UploadedFile == null || UploadedFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Выберите файл для загрузки";
            await LoadDocumentsAsync(null, 1, PageSize);
            return Page();
        }

        if (SignerIds.Count == 0)
        {
            TempData["ErrorMessage"] = "Выберите хотя бы одного подписанта";
            await LoadDocumentsAsync(null, 1, PageSize);
            return Page();
        }

        using var stream = UploadedFile.OpenReadStream();
        var (success, message) = await _apiClient.UploadDocument(
            Title.Trim(), Description.Trim(), SignerIds, IsSequential, stream, UploadedFile.FileName);

        if (success)
            TempData["SuccessMessage"] = "Документ успешно отправлен на подпись";
        else
            TempData["ErrorMessage"] = $"Ошибка: {message}";

        return RedirectToPage();
    }

    public async Task<JsonResult> OnPostUploadAjaxAsync()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return new JsonResult(new { success = false, message = "Введите название документа" });

        if (UploadedFile == null || UploadedFile.Length == 0)
            return new JsonResult(new { success = false, message = "Файл не выбран" });

        if (SignerIds.Count == 0)
            return new JsonResult(new { success = false, message = "Выберите хотя бы одного подписанта" });

        using var stream = UploadedFile.OpenReadStream();
        var (success, message) = await _apiClient.UploadDocument(
            Title.Trim(), Description.Trim(), SignerIds, IsSequential, stream, UploadedFile.FileName);

        return new JsonResult(new { success, message });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var (success, message) = await _apiClient.DeleteDocument(id);
        if (success)
            TempData["SuccessMessage"] = "Документ удалён";
        else
            TempData["ErrorMessage"] = $"Ошибка: {message}";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id)
    {
        var (stream, fileName) = await _apiClient.DownloadDocument(id);
        if (stream == null)
        {
            return NotFound();
        }

        // Если API не вернуло имя файла, подставляем техническое
        fileName ??= $"document-{id}";
        return File(stream, "application/octet-stream", fileName);
    }

    public async Task<JsonResult> OnGetSignersAsync(int id)
    {
        var signers = await _apiClient.GetDocumentUsers(id);
        return new JsonResult(signers);
    }

    public async Task<JsonResult> OnGetSearchUsersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new JsonResult(new List<object>());

        var users = await _apiClient.SearchUsers(query);
        var result = users.Select(u => new { u.Id, u.Fullname, u.Login }).ToList();
        return new JsonResult(result);
    }

    private async Task LoadDocumentsAsync(string? statusFilter, int p, int pageSize)
    {
        DocumentStatus[]? statuses = null;
        DocumentStatus? single = null;
        if (string.IsNullOrEmpty(statusFilter))
        {
            statuses =
            [
                DocumentStatus.NOT_SIGNED,
                DocumentStatus.PARTLY_SIGNED,
                DocumentStatus.DECLINED
            ];
        }
        else if (Enum.TryParse<DocumentStatus>(statusFilter, out var st))
            single = st;
        else
        {
            statuses =
            [
                DocumentStatus.NOT_SIGNED,
                DocumentStatus.PARTLY_SIGNED,
                DocumentStatus.DECLINED
            ];
        }

        var paged = await _apiClient.GetDocumentsFilteredPaged(statuses, single, null, p, pageSize);
        Documents = paged.Items;
        TotalCount = paged.TotalCount;
        TotalPages = paged.TotalPages > 0 ? paged.TotalPages : 1;
    }
}
