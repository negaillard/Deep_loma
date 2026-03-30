using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Contracts.BindingModels;
using Contracts.Requests;
using Contracts.Responses;
using Contracts.ViewModels;
using Models;

namespace DocumentApp.Services;

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
                new { Login = login, appType = (int)AppType.DOCUMENT_APP });
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

    // ── Users ─────────────────────────────────────────────────────────────

    public async Task<List<UserViewModel>> SearchUsers(string fullname)
    {
        try
        {
            return await Client().GetFromJsonAsync<List<UserViewModel>>(
                $"api/users/filter?fullname={Uri.EscapeDataString(fullname)}") ?? [];
        }
        catch { return []; }
    }

    public async Task<UserViewModel?> GetCurrentUser()
    {
        try
        {
            var userId = _ctx.HttpContext?.User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId)) return null;
            return await Client().GetFromJsonAsync<UserViewModel>($"api/users/{userId}");
        }
        catch { return null; }
    }

    // ── Documents ─────────────────────────────────────────────────────────

    /// <summary>
    /// Единый запрос: фильтры и пагинация (ответ API — <see cref="PagedResult{T}"/>).
    /// </summary>
    public async Task<PagedResult<DocumentViewModel>> GetDocumentsFilteredPaged(
        DocumentStatus[]? statuses = null,
        DocumentStatus? status = null,
        string? search = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        try
        {
            var qs = new StringBuilder();
            qs.Append("isDeleted=false");
            qs.Append("&pageNumber=").Append(pageNumber);
            qs.Append("&pageSize=").Append(pageSize);
            if (statuses is { Length: > 0 })
            {
                foreach (var s in statuses)
                    qs.Append("&statuses=").Append((int)s);
            }
            else if (status.HasValue)
                qs.Append("&status=").Append((int)status.Value);
            if (!string.IsNullOrWhiteSpace(search))
                qs.Append("&search=").Append(Uri.EscapeDataString(search));

            var url = $"api/documents/filter?{qs}";
            return await Client().GetFromJsonAsync<PagedResult<DocumentViewModel>>(url)
                   ?? new PagedResult<DocumentViewModel>
                   {
                       PageNumber = pageNumber,
                       PageSize = pageSize
                   };
        }
        catch
        {
            return new PagedResult<DocumentViewModel>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }

    public async Task<(bool Success, string Message)> UploadDocument(
        string title, string description, List<int> userIds, bool isSequential, Stream fileStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(title), "title");
            content.Add(new StringContent(description), "description");
            foreach (var id in userIds)
                content.Add(new StringContent(id.ToString()), "userIds");
            content.Add(new StringContent(isSequential ? "true" : "false"), "isSequential");
            content.Add(new StreamContent(fileStream), "file", fileName);

            var response = await Client().PostAsync("api/documents", content);
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка при загрузке документа: {ex.Message}");
        }
    }
    public async Task<(Stream? Stream, string? FileName)> DownloadDocument(int id)
    {
        try
        {
            var response = await Client().GetAsync($"api/documents/{id}/file",
                HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                return (null, null);
            }

            var stream = await response.Content.ReadAsStreamAsync();

            // Пытаемся достать имя файла из заголовка Content-Disposition
            var cd = response.Content.Headers.ContentDisposition;
            var fileName = cd?.FileNameStar ?? cd?.FileName;

            // Убираем лишние кавычки, если они есть
            fileName = fileName?.Trim('\"');

            return (stream, fileName);
        }
        catch
        {
            return (null, null);
        }
    }

    public async Task<(bool Success, string Message)> DeleteDocument(int id)
    {
        try
        {
            var response = await Client().DeleteAsync($"api/documents/{id}");
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // ── Document Users ─────────────────────────────────────────────────────

    public async Task<List<DocumentUserViewModel>> GetDocumentUsers(int documentId)
    {
        try
        {
            return await Client().GetFromJsonAsync<List<DocumentUserViewModel>>(
                $"api/signing/{documentId}/signers") ?? [];
        }
        catch { return []; }
    }
}
