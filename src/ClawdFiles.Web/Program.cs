using ClawdFiles.Application.Services;
using ClawdFiles.Infrastructure;
using ClawdFiles.Infrastructure.Data;
using ClawdFiles.Web.Authentication;
using ClawdFiles.Web.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure (DB, repos, storage, background services)
builder.Services.AddInfrastructure(builder.Configuration);

// Application services
builder.Services.AddScoped<KeyManagementService>();
builder.Services.AddScoped<BucketService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<BucketSummaryService>();

// Authentication
builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.SchemeName)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.SchemeName, _ => { });
builder.Services.AddAuthorization();

// API controllers
builder.Services.AddControllers();

// MudBlazor
builder.Services.AddMudServices();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOpenApi(options => options.AddScalarTransformers());

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClawdFilesDbContext>();
    if (app.Environment.IsDevelopment())
        await db.Database.EnsureCreatedAsync();
    else
        await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.MapStaticAssets();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

// API controllers before Blazor
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}


app.Run();

// For integration tests
public partial class Program;
