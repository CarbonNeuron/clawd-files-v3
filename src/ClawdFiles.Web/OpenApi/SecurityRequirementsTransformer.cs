using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ClawdFiles.Web.OpenApi;

/// <summary>
/// Adds ApiKey and AdminKey security scheme definitions to the OpenAPI document.
/// </summary>
public sealed class SecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var components = document.Components ??= new OpenApiComponents();

        components.SecuritySchemes!["ApiKey"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            Description = "Any valid API key (regular or admin). Pass as: `Authorization: Bearer <key>`",
        };

        components.SecuritySchemes!["AdminKey"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            Description = "Admin API key required. Pass as: `Authorization: Bearer <admin-key>`",
        };

        return Task.CompletedTask;
    }
}

/// <summary>
/// Adds per-operation security requirements based on [Authorize] and [AllowAnonymous] attributes.
/// </summary>
public sealed class SecurityRequirementsTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<AllowAnonymousAttribute>().Any())
            return Task.CompletedTask;

        var authorizeAttrs = metadata.OfType<AuthorizeAttribute>().ToList();
        if (authorizeAttrs.Count == 0)
            return Task.CompletedTask;

        var requiresAdmin = authorizeAttrs.Any(a =>
            a.Roles?.Split(',').Any(r => r.Trim() == "Admin") == true);

        var schemeId = requiresAdmin ? "AdminKey" : "ApiKey";
        var schemeRef = new OpenApiSecuritySchemeReference(schemeId, context.Document);

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [schemeRef] = new List<string>()
        });

        return Task.CompletedTask;
    }
}
