using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Application.Models;
using OpsFlow.Application.Security;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Identity;
using OpsFlow.Infrastructure.Persistence;

namespace OpsFlow.IntegrationTests;

public sealed class ApiWorkflowTests : IClassFixture<OpsFlowApiFactory>, IAsyncLifetime
{
    private readonly OpsFlowApiFactory _factory;
    private readonly HttpClient _client;

    public ApiWorkflowTests(OpsFlowApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DatabaseSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<UserManager<OpsUser>>(),
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_returns_healthy()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_returns_token_and_profile()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@opsflow.demo", "Demo123!"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
        auth.User.Roles.Should().Contain(Roles.Administrator);
    }

    [Fact]
    public async Task Invalid_login_is_unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@opsflow.demo", "wrong"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Assets_require_authentication()
    {
        var response = await _client.GetAsync("/api/assets");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Viewer_can_read_assets_but_cannot_create_them()
    {
        await Authenticate("viewer@opsflow.demo");
        (await _client.GetAsync("/api/assets")).StatusCode.Should().Be(HttpStatusCode.OK);
        var request = new AssetRequest("Test laptop", "Laptop", "Dell", "Test", null, "Boise HQ", "Engineering",
            1, 1, null, null, AssetStatus.InStock, null, null);
        (await _client.PostAsJsonAsync("/api/assets", request)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Inventory_manager_can_create_and_retrieve_asset()
    {
        await Authenticate("inventory@opsflow.demo");
        var request = new AssetRequest("Replacement Laptop", "Laptop", "Dell", "Latitude 7650", "DEMO-NEW",
            "Boise HQ", "Engineering", 5, 2, new DateOnly(2026, 7, 17), new DateOnly(2029, 7, 17),
            AssetStatus.InStock, null, "Integration test asset");
        var created = await _client.PostAsJsonAsync("/api/assets", request);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var list = await _client.GetStringAsync("/api/assets?search=Replacement");
        list.Should().Contain("Replacement Laptop");
    }

    [Fact]
    public async Task Support_engineer_can_create_incident()
    {
        await Authenticate("support@opsflow.demo");
        var request = new IncidentRequest("Ordering latency increased", "P95 latency exceeds the alert threshold.",
            IncidentSeverity.Sev2, IncidentPriority.High, "Maya Chen", "Maya Chen", null, null);
        var response = await _client.PostAsJsonAsync("/api/incidents", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadAsStringAsync()).Should().Contain("INC-2026");
    }

    [Fact]
    public async Task Viewer_cannot_read_admin_audit_log()
    {
        await Authenticate("viewer@opsflow.demo");
        (await _client.GetAsync("/api/audit-logs")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task Authenticate(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Demo123!"));
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
    }
}
