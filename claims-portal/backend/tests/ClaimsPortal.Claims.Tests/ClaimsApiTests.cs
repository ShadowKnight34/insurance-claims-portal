using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClaimsPortal.Claims.Application.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClaimsPortal.Claims.Tests;

public sealed class ClaimsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}

public sealed class ClaimsApiTests : IClassFixture<ClaimsApiFactory>
{
    private readonly ClaimsApiFactory _factory;

    public ClaimsApiTests(ClaimsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_IsPublic()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Claims_RequireAuthentication()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/claims");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsRoleAndProtectsTheDashboard()
    {
        using var anonymous = _factory.CreateClient();
        var claimant = await LoginAsync(anonymous, "claimant@claims.demo", "Claimant123!");
        var officer = await LoginAsync(anonymous, "officer@claims.demo", "Officer123!");

        Assert.Equal("Claimant", claimant.User.Role);
        Assert.Equal("ClaimsOfficer", officer.User.Role);

        using var claimantClient = Authorized(claimant.AccessToken);
        using var officerClient = Authorized(officer.AccessToken);

        var ownClaims = await claimantClient.GetFromJsonAsync<PagedClaims>("/api/claims");
        var dashboardDenied = await claimantClient.GetAsync("/api/dashboard/statistics");
        var dashboard = await officerClient.GetFromJsonAsync<DashboardDto>("/api/dashboard/statistics");

        Assert.NotNull(ownClaims);
        Assert.NotEmpty(ownClaims!.Items);
        Assert.All(ownClaims.Items, item => Assert.Equal("Aisha Rahman", item.ClaimantName));
        Assert.Equal(HttpStatusCode.Forbidden, dashboardDenied.StatusCode);
        Assert.NotNull(dashboard);
        Assert.True(dashboard!.TotalClaims >= 6);
        Assert.True(dashboard.OutstandingLiability > 0);
        Assert.Contains(dashboard.ByType, item => item.Label == "Motor" && item.Count > 0);
        Assert.Contains(dashboard.ByType, item => item.Label == "Property" && item.Count > 0);
    }

    private HttpClient Authorized(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<LoginResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail(body);
        }

        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    private sealed record PagedClaims(List<ClaimSummaryDto> Items, int Page, int PageSize, int TotalCount);
}
