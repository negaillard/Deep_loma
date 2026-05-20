using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Contracts;
using Contracts.BindingModels;
using Contracts.Requests;
using Contracts.Responses;
using Contracts.ViewModels;
using Models.Enums;

namespace AdminApp.Services;

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
                new { Login = login, appType = (int)AppType.ADMIN_APP });
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

    // ── Roles ─────────────────────────────────────────────────────────────

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

    // ── Users ─────────────────────────────────────────────────────────────

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

    // ── Certificates ──────────────────────────────────────────────────────

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
}
