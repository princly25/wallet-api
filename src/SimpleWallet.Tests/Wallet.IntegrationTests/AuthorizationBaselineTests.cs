using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SimpleWallet.IntegrationTests;

/// <summary>
/// Exercises the running API started by Docker Compose in Development mode.
/// Tests the deliberately admin-only baseline until per-user identity and ownership exist.
/// </summary>
public class AuthorizationBaselineTests
{
    private static HttpClient NewClient() => new()
    {
        BaseAddress = new Uri("http://localhost:8080")
    };

    private static async Task<HttpClient> LoginAsync(string username, string password)
    {
        var client = NewClient();
        using var response = await client.PostAsJsonAsync(
            "/api/Authentication/login", new { username, password });
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static object RequestFor(string action)
    {
        var walletId = Guid.NewGuid();
        return action == "transfer"
            ? new { fromWalletId = walletId, toWalletId = Guid.NewGuid(), amount = 1m }
            : (object)new { walletId, amount = 1m };
    }

    [Theory]
    [InlineData("deposit")]
    [InlineData("withdraw")]
    [InlineData("transfer")]
    public async Task MonetaryActions_WithoutAuthentication_ReturnUnauthorized(string action)
    {
        using var client = NewClient();
        using var response = await client.PostAsJsonAsync(
            $"/api/Transaction/{action}", RequestFor(action));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("deposit")]
    [InlineData("withdraw")]
    [InlineData("transfer")]
    public async Task MonetaryActions_RegularUser_ReturnForbidden(string action)
    {
        using var client = await LoginAsync("user", "user123");
        using var response = await client.PostAsJsonAsync(
            $"/api/Transaction/{action}", RequestFor(action));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/Wallet/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/Wallet/user/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/User/00000000-0000-0000-0000-000000000001")]
    public async Task SensitiveReads_WithoutAuthentication_ReturnUnauthorized(string path)
    {
        using var client = NewClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/Wallet/00000000-0000-0000-0000-000000000001")]
    [InlineData("/api/User/00000000-0000-0000-0000-000000000001")]
    public async Task SensitiveReads_RegularUser_ReturnForbidden(string path)
    {
        using var client = await LoginAsync("user", "user123");
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WalletListing_AdminToken_RemainsAccessible()
    {
        using var client = await LoginAsync("admin", "admin123");
        using var response = await client.GetAsync(
            "/api/Wallet/user/00000000-0000-0000-0000-000000000001");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
