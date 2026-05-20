using Contracts.Requests;
using Contracts.Responses;
using Contracts.ViewModels;
using Models.Enums;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SignerApp.Services;

public class ApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly IHttpContextAccessor _ctx;

    public ApiClient(IHttpClientFactory factory, IHttpContextAccessor ctx)
    {
        _factory = factory;
        _ctx = ctx;
    }

    private string? Token =>
        _ctx.HttpContext?.User.FindFirst("SessionToken")?.Value;

    private HttpClient Client()
    {
        var client = _factory.CreateClient("ApiClient");
        if (!string.IsNullOrEmpty(Token))
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Token);
        return client;
    }

    // ── Auth ──────────────────────────────────────────────────────────────

    public async Task<(bool Success, string Message)> SendLoginCode(string login)
    {
        try
        {
            var client = _factory.CreateClient("ApiClient");
            var response = await client.PostAsJsonAsync("api/auth/send-login-code",
                new { Login = login, appType = (int)AppType.SIGNER_APP });
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка подключения к API: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, VerifyLoginResponse? Data)> VerifyLogin(
        string login, string code)
    {
        try
        {
            var client = _factory.CreateClient("ApiClient");
            var response = await client.PostAsJsonAsync("api/auth/verify-login",
                new VerifyLoginRequest { Login = login, Code = code });
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<VerifyLoginResponse>();
                return (true, "OK", data);
            }
            var body = await response.Content.ReadAsStringAsync();
            return (false, body.Trim('"'), null);
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка подключения к API: {ex.Message}", null);
        }
    }

    public async Task Logout(string token)
    {
        try
        {
            var client = _factory.CreateClient("ApiClient");
            await client.PostAsJsonAsync("api/auth/logout",
                new LogoutRequest { SessionToken = token });
        }
        catch { }
    }

    // ── Documents for sign ───────────────────────────────────────────────

    public async Task<PagedResult<DocumentForSignViewModel>> GetDocumentsForSign(
        SigningStatus? signingStatus = null, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            var url = new StringBuilder("api/documents/get-for-sign?");
            url.Append($"pageNumber={pageNumber}&pageSize={pageSize}");
            if (signingStatus.HasValue)
                url.Append($"&signingStatus={(int)signingStatus.Value}");

            return await Client().GetFromJsonAsync<PagedResult<DocumentForSignViewModel>>(url.ToString())
                ?? new PagedResult<DocumentForSignViewModel> { PageNumber = pageNumber, PageSize = pageSize };
        }
        catch
        {
            return new PagedResult<DocumentForSignViewModel> { PageNumber = pageNumber, PageSize = pageSize };
        }
    }

    public async Task<(Stream? Stream, string? FileName)> DownloadDocument(int id)
    {
        try
        {
            var response = await Client().GetAsync($"api/documents/{id}/file",
                HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) return (null, null);

            var stream = await response.Content.ReadAsStreamAsync();
            var cd = response.Content.Headers.ContentDisposition;
            var fileName = (cd?.FileNameStar ?? cd?.FileName)?.Trim('"');
            return (stream, fileName);
        }
        catch { return (null, null); }
    }

    // ── Signing ───────────────────────────────────────────────────────────

    /// <summary>
    /// Internal mode: намерение подписать, сервер выполняет подписание.
    /// </summary>
    public async Task<(bool Success, string Message)> SignIntent(int documentId)
    {
        try
        {
            var response = await Client().PostAsync($"api/signing/{documentId}/sign-intent", null);
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка: {ex.Message}");
        }
    }

    /// <summary>
    /// Local mode: клиент присылает готовую PKCS#7 подпись в base64.
    /// </summary>
    public async Task<(bool Success, string Message)> SubmitSignature(int documentId, string signatureBase64)
    {
        try
        {
            var response = await Client().PostAsJsonAsync(
                $"api/signing/{documentId}/submit-signature",
                new { SignatureBase64 = signatureBase64 });
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> Reject(int documentId)
    {
        try
        {
            var response = await Client().PostAsync($"api/signing/{documentId}/reject", null);
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка: {ex.Message}");
        }
    }
}
