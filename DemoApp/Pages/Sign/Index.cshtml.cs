using Contracts.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;
using DemoApp.Services;
using Models.Enums;

namespace DemoApp.Pages.Sign;

public class IndexModel : PageModel
{
    private readonly ApiClient _apiClient;
    private readonly IConfiguration _configuration;
    public const int PageSize = 10;

    public IndexModel(ApiClient apiClient, IConfiguration configuration)
    {
        _apiClient = apiClient;
        _configuration = configuration;
    }

    public PagedResult<DocumentForSignViewModel> PendingDocs { get; set; } = new();
    public PagedResult<DocumentForSignViewModel> SignedDocs { get; set; } = new();
    public PagedResult<DocumentForSignViewModel> DeclinedDocs { get; set; } = new();
    public CertificateMode AppCertificateMode { get; private set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public int PendingPage { get; set; } = 1;
    public int SignedPage { get; set; } = 1;
    public int DeclinedPage { get; set; } = 1;
    public string ActiveTab { get; set; } = "pending";

    public async Task OnGetAsync(
        int pendingPage = 1, int signedPage = 1, int declinedPage = 1,
        string tab = "pending")
    {
        PendingPage = pendingPage;
        SignedPage = signedPage;
        DeclinedPage = declinedPage;
        ActiveTab = tab;
        AppCertificateMode = _configuration.GetValue<CertificateMode>("CertificateMode");

        PendingDocs = await _apiClient.GetDocumentsForSign(SigningStatus.NOT_SIGNED, pendingPage, PageSize);
        SignedDocs = await _apiClient.GetDocumentsForSign(SigningStatus.SIGNED, signedPage, PageSize);
        DeclinedDocs = await _apiClient.GetDocumentsForSign(SigningStatus.DECLINED, declinedPage, PageSize);
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id, string tab = "pending")
    {
        var (stream, fileName) = await _apiClient.DownloadDocument(id);
        if (stream == null)
        {
            ErrorMessage = "Не удалось скачать файл документа";
            return RedirectToPage(new { tab });
        }
        return File(stream, "application/octet-stream", fileName ?? $"document_{id}");
    }

    // Internal mode: намерение подписать
    public async Task<IActionResult> OnPostSignIntentAsync(int id)
    {
        var (success, message) = await _apiClient.SignIntent(id);
        if (success)
            SuccessMessage = "Документ принят в обработку для подписания";
        else
            ErrorMessage = $"Ошибка: {message}";

        return RedirectToPage(new { tab = "pending" });
    }

    /// <summary>
    /// Local mode: AJAX-хендлер. JS подписывает через CryptoPro Browser Plugin
    /// и присылает готовую base64-подпись. Возвращает JSON.
    /// </summary>
    public async Task<IActionResult> OnPostSignLocalAsync([FromBody] SignLocalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.SignatureBase64))
            return new JsonResult(new { success = false, message = "Подпись не передана" });

        var (success, message) = await _apiClient.SubmitSignature(request.Id, request.SignatureBase64);
        return new JsonResult(new { success, message });
    }

    /// <summary>
    /// Возвращает байты документа в base64 — используется JS для подписания.
    /// </summary>
    public async Task<IActionResult> OnGetDocumentBytesAsync(int id)
    {
        var (stream, _) = await _apiClient.DownloadDocument(id);
        if (stream == null)
            return new JsonResult(new { success = false, message = "Не удалось загрузить документ" });

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var base64 = Convert.ToBase64String(ms.ToArray());
        return new JsonResult(new { success = true, base64 });
    }

    public record SignLocalRequest(int Id, string SignatureBase64);

    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        var (success, message) = await _apiClient.Reject(id);
        if (success)
            SuccessMessage = "Отказ от подписи зафиксирован";
        else
            ErrorMessage = $"Ошибка: {message}";

        return RedirectToPage(new { tab = "pending" });
    }
}
