using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MyApi.Tests;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task WeatherForecast_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/weatherforecast");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithBadCredentials_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/login",
            new { username = "khin", password = "wrongpassword" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithGoodCredentials_ReturnsToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/login",
            new { username = "khin", password = "password123" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body?.access_token));
    }

    [Fact]
    public async Task WeatherForecast_WithValidToken_Returns200()
    {
        var client = _factory.CreateClient();

        // First log in to get a real token
        var login = await client.PostAsJsonAsync("/login",
            new { username = "khin", password = "password123" });
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!.access_token;

        // Then call the protected endpoint with it
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync("/weatherforecast");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private record TokenResponse(string access_token);
}