using DocumentApp.Services;
using Contracts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models.Enums;

namespace DocumentApp.Pages.Documents;

[Authorize]
public class ArchiveModel : PageModel
{
    private readonly ApiClient _apiClient;

    public ArchiveModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public List<DocumentViewModel> Documents { get; set; } = [];
    public string? StatusFilter { get; set; }
    public string? SearchTerm { get; set; }

    [BindNever]
    public int PageNumber { get; set; } = 1;

    [BindNever]
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public static string StatusLabel(DocumentStatus status) => status switch
    {
        DocumentStatus.SIGNED   => "Подписан",
        DocumentStatus.DECLINED => "Отклонён",
        _ => status.ToString()
    };

    public static string StatusBadgeClass(DocumentStatus status) => status switch
    {
        DocumentStatus.SIGNED   => "signed",
        DocumentStatus.DECLINED => "declined",
        _ => ""
    };

    public static string StatusIcon(DocumentStatus status) => status switch
    {
        DocumentStatus.SIGNED   => "bi-check-circle",
        DocumentStatus.DECLINED => "bi-x-circle",
        _ => "bi-file-earmark"
    };

    /// <param name="p">
    /// Номер страницы (не <c>page</c>: в Razor Pages query <c>page</c> зарезервирован под путь к странице).
    /// </param>
    public async Task<IActionResult> OnGetAsync(
        [FromQuery] string? statusFilter = null,
        [FromQuery] string? search = null,
        [FromQuery] int p = 1,
        [FromQuery] int pageSize = 20)
    {
        StatusFilter = statusFilter;
        SearchTerm = search;
        if (p < 1) p = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;
        PageNumber = p;
        PageSize = pageSize;

        DocumentStatus[]? statuses = null;
        DocumentStatus? singleStatus = null;

        if (string.IsNullOrEmpty(statusFilter))
        {
            statuses =
            [
                DocumentStatus.SIGNED,
                DocumentStatus.DECLINED
            ];
        }
        else if (Enum.TryParse<DocumentStatus>(statusFilter, out var st))
            singleStatus = st;
        else
        {
            statuses =
            [
                DocumentStatus.SIGNED,
                DocumentStatus.DECLINED
            ];
        }

        var paged = await _apiClient.GetDocumentsFilteredPaged(
            statuses,
            singleStatus,
            string.IsNullOrWhiteSpace(search) ? null : search,
            p,
            pageSize);

        Documents = paged.Items ?? [];
        TotalCount = paged.TotalCount;
        TotalPages = paged.TotalPages > 0 ? paged.TotalPages : 1;

        return Page();
    }

    public async Task<JsonResult> OnGetSignersAsync(int id)
    {
        var signers = await _apiClient.GetDocumentUsers(id);
        return new JsonResult(signers);
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id)
    {
        var (stream, fileName) = await _apiClient.DownloadDocument(id);
        if (stream == null)
            return NotFound();

        fileName ??= $"document-{id}";
        return File(stream, "application/octet-stream", fileName);
    }

    public async Task<IActionResult> OnGetVerificationPackageAsync(int id)
    {
        var (stream, fileName) = await _apiClient.DownloadVerificationPackage(id);
        if (stream == null)
            return NotFound();

        fileName ??= $"verification-{id}.zip";
        return File(stream, "application/zip", fileName);
    }
}
