using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Contracts;
using Contracts.BindingModels;
using Contracts.Requests;
using Contracts.Responses;
using Contracts.ViewModels;
using Models;

namespace DemoApp.Services;

/// <summary>
/// Клиент API: объединяет вызовы админ-приложения, документооборота и подписанта.
/// </summary>
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
                new { Login = login, appType = (int)AppType.DEMO_APP });
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

    // ── Roles (admin) ─────────────────────────────────────────────────────

    public async Task<List<RoleViewModel>> GetRolesPaged(int page, int pageSize)
    {
        try
        {
            var all = await Client().GetFromJsonAsync<List<RoleViewModel>>(
                $"api/roles/paged?pageNumber={page}&pageSize={pageSize}") ?? [];
            return all.Where(r => r.Name != SystemConstants.NoRoleName).ToList();
        }
        catch { return []; }
    }

    public async Task<List<RoleViewModel>> GetAllRoles()
    {
        try
        {
            var all = await Client().GetFromJsonAsync<List<RoleViewModel>>("api/roles") ?? [];
            return all.Where(r => r.Name != SystemConstants.NoRoleName).ToList();
        }
        catch { return []; }
    }

    public async Task<(bool Success, string Message)> UpdateRole(RoleBindingModel model)
    {
        try
        {
            var response = await Client().PutAsJsonAsync("api/roles", model);
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> CreateRole(RoleBindingModel model)
    {
        try
        {
            var response = await Client().PostAsJsonAsync("api/roles", model);
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> DeleteRole(int id)
    {
        try
        {
            var response = await Client().DeleteAsync($"api/roles/{id}");
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // ── Users (admin) ─────────────────────────────────────────────────────

    public async Task<List<UserViewModel>> GetUsersPaged(int page, int pageSize)
    {
        try
        {
            return await Client().GetFromJsonAsync<List<UserViewModel>>(
                $"api/users/paged?pageNumber={page}&pageSize={pageSize}") ?? [];
        }
        catch { return []; }
    }

    public async Task<List<UserViewModel>> FilterUsers(string fullname)
    {
        try
        {
            return await Client().GetFromJsonAsync<List<UserViewModel>>(
                $"api/users/filter?fullname={Uri.EscapeDataString(fullname)}") ?? [];
        }
        catch { return []; }
    }

    public async Task<(bool Success, string Message)> UpdateUser(UserBindingModel model)
    {
        try
        {
            var response = await Client().PutAsJsonAsync("api/users", model);
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> CreateUser(UserBindingModel model)
    {
        try
        {
            var response = await Client().PostAsJsonAsync("api/users", model);
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> DeleteUser(int id)
    {
        try
        {
            var response = await Client().DeleteAsync($"api/users/{id}");
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> GenerateCertificate(int userId)
    {
        try
        {
            var response = await Client().PostAsync($"api/certificates/{userId}/generate", null);
            var body = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, body.Trim('"'));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // ── Users (документооборот) ───────────────────────────────────────────

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

    // ── Documents (менеджер) ─────────────────────────────────────────────

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
                return (null, null);

            var stream = await response.Content.ReadAsStreamAsync();
            var cd = response.Content.Headers.ContentDisposition;
            var fileName = cd?.FileNameStar ?? cd?.FileName;
            fileName = fileName?.Trim('"');
            return (stream, fileName);
        }
        catch
        {
            return (null, null);
        }
    }

    public async Task<(Stream? Stream, string? FileName)> DownloadVerificationPackage(int id)
    {
        try
        {
            var response = await Client().GetAsync($"api/documents/{id}/verification-package",
                HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
                return (null, null);

            var stream = await response.Content.ReadAsStreamAsync();
            var cd = response.Content.Headers.ContentDisposition;
            var fileName = cd?.FileNameStar ?? cd?.FileName;
            fileName = fileName?.Trim('"');
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

    public async Task<List<DocumentUserViewModel>> GetDocumentUsers(int documentId)
    {
        try
        {
            return await Client().GetFromJsonAsync<List<DocumentUserViewModel>>(
                $"api/signing/{documentId}/signers") ?? [];
        }
        catch { return []; }
    }

    // ── Подписание ───────────────────────────────────────────────────────

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
