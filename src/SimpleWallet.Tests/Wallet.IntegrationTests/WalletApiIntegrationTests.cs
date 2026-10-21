using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SimpleWallet.IntegrationTests;

public class WalletApiIntegrationTests
{
    private readonly HttpClient _client;
    private readonly string _baseUrl = "http://localhost:8080";

    public WalletApiIntegrationTests()
    {
        _client = new HttpClient();
    }

    [Fact]
    public async Task FullWorkflow_LoginThenAuthenticatedRequests_ReturnsSuccess()
    {
        // arrange
        var loginRequest = new { username = "admin", password = "admin123" };

        // act 1: login to get JWT token
        var loginResponse = await _client.PostAsJsonAsync($"{_baseUrl}/api/Authentication/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(loginContent);
        var token = jsonDoc.RootElement.GetProperty("token").GetString();
        Assert.NotNull(token);

        // act 2: add authorization header for subsequent requests
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // act 3: get wallets for a user (GET endpoints don't require auth in this API)
        var userId = "00000000-0000-0000-0000-000000000001";
        var getResponse = await _client.GetAsync($"{_baseUrl}/api/Wallet/user/{userId}");
        
        // assert: should return 200 OK
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenInOkResponse()
    {
        // arrange
        var loginRequest = new { username = "user", password = "user123" };

        // act
        var loginResponse = await _client.PostAsJsonAsync($"{_baseUrl}/api/Authentication/login", loginRequest);

        // assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var content = await loginResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        Assert.True(jsonDoc.RootElement.TryGetProperty("token", out var tokenProperty));
        Assert.NotEqual("", tokenProperty.GetString());
    }

    [Fact]
    public async Task DeleteWallet_WithoutToken_Returns401Unauthorized()
    {
        // arrange - don't add authorization header
        var walletId = "00000000-0000-0000-0000-000000000001";

        // act: try to DELETE (which requires auth) without token
        var response = await _client.DeleteAsync($"{_baseUrl}/api/Wallet/{walletId}");

        // assert: should return 401 Unauthorized since DELETE requires [Authorize]
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchUser_WithoutToken_Returns401Unauthorized()
    {
        // arrange
        var userId = "00000000-0000-0000-0000-000000000001";
        var patchBody = JsonSerializer.Serialize(new { name = "NoAuth Update" });
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_baseUrl}/api/User/{userId}")
        {
            Content = new StringContent(patchBody, Encoding.UTF8, "application/json")
        };

        // act
        var response = await _client.SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchUser_WithAdminTokenAndValidPayload_ReturnsNoContent()
    {
        // arrange
        var loginRequest = new { username = "admin", password = "admin123" };
        var loginResponse = await _client.PostAsJsonAsync($"{_baseUrl}/api/Authentication/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(loginContent);
        var token = jsonDoc.RootElement.GetProperty("token").GetString();
        Assert.NotNull(token);

        var userId = "00000000-0000-0000-0000-000000000001";
        var patchBody = JsonSerializer.Serialize(new { name = "Alice Updated" });
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_baseUrl}/api/User/{userId}")
        {
            Content = new StringContent(patchBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // act
        var response = await _client.SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_AfterPatch_ReturnsUpdatedName()
    {
        // arrange
        var loginRequest = new { username = "admin", password = "admin123" };
        var loginResponse = await _client.PostAsJsonAsync($"{_baseUrl}/api/Authentication/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(loginContent);
        var token = jsonDoc.RootElement.GetProperty("token").GetString();
        Assert.NotNull(token);

        var userId = "00000000-0000-0000-0000-000000000001";
        var expectedName = "Alice Patched";

        var patchBody = JsonSerializer.Serialize(new { name = expectedName });
        var patchRequest = new HttpRequestMessage(new HttpMethod("PATCH"), $"{_baseUrl}/api/User/{userId}")
        {
            Content = new StringContent(patchBody, Encoding.UTF8, "application/json")
        };
        patchRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var patchResponse = await _client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);

        // act
        // The user endpoint is now private; include the admin token received above.
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/User/{userId}");
        getRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var getResponse = await _client.SendAsync(getRequest);

        // assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var body = await getResponse.Content.ReadAsStringAsync();
        var userDoc = JsonDocument.Parse(body);
        var actualName = userDoc.RootElement.GetProperty("name").GetString();
        Assert.Equal(expectedName, actualName);
    }
}

