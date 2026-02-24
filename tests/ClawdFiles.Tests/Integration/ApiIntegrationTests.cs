using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClawdFiles.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClawdFiles.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;
    private readonly SqliteConnection _connection;
    private const string AdminKey = "test-admin-key";

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Keep an open connection so the in-memory SQLite database persists
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ClawdFilesDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                // Use in-memory SQLite for tests with shared connection
                services.AddDbContext<ClawdFilesDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });
            });
            builder.UseSetting("AdminApiKey", AdminKey);
        });
        _client = customFactory.CreateClient();
    }

    [Fact]
    public async Task CreateKey_ListKeys_RoundTrip()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminKey);

        var createResponse = await _client.PostAsJsonAsync("/api/keys", new { name = "test-key" });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var listResponse = await _client.GetAsync("/api/keys");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_AdminEndpoint_Returns401Or403()
    {
        // Without auth, admin endpoints should reject
        var response = await _client.GetAsync("/api/keys");
        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden,
            $"Expected 401 or 403, got {response.StatusCode}");
    }

    [Fact]
    public async Task GetBucket_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/buckets/nonexistent");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LlmsTxt_ReturnsPlainText()
    {
        var response = await _client.GetAsync("/llms.txt");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Clawd Files", content);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
