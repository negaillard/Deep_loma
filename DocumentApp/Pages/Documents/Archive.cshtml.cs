using DocumentApp.Services;
using Contracts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;

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

    public async Task<IActionResult> OnGetAsync(string? statusFilter = null, string? search = null)
    {
        StatusFilter = statusFilter;
        SearchTerm = search;

        if (string.IsNullOrEmpty(statusFilter))
        {
            Documents = await _apiClient.GetDocumentsByStatuses(
            [
                DocumentStatus.SIGNED,
                DocumentStatus.DECLINED
            ]);
        }
        else if (Enum.TryParse<DocumentStatus>(statusFilter, out var status))
        {
            Documents = await _apiClient.GetDocumentsByStatus(status);
        }
        else
        {
            Documents = await _apiClient.GetDocumentsByStatuses(
            [
                DocumentStatus.SIGNED,
                DocumentStatus.DECLINED
            ]);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            Documents = [.. Documents.Where(d =>
                d.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(d.Description) &&
                 d.Description.Contains(search, StringComparison.OrdinalIgnoreCase)))];
        }

        Documents = [.. Documents.OrderByDescending(d => d.CreatedAt)];
        return Page();
    }
}
