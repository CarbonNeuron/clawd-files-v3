using Microsoft.AspNetCore.Mvc;

namespace ClawdFiles.Web.Controllers;

[ApiController]
public class DocsController : ControllerBase
{
    private const string LlmsTxt = """
        # Clawd Files API

        ## Overview
        Clawd Files is a file-sharing platform with bucket-based organization, API key authentication, and LLM-friendly endpoints.

        ## Authentication
        Write operations require: Authorization: Bearer <api_key>
        Admin endpoints need the admin key. Public read access requires no authentication.

        ## Endpoints
        - POST /api/keys - Create API key (admin)
        - GET /api/keys - List keys (admin)
        - DELETE /api/keys/{prefix} - Revoke key (admin)
        - POST /api/buckets - Create bucket
        - GET /api/buckets - List buckets
        - GET /api/buckets/{id} - Get bucket details (public)
        - PATCH /api/buckets/{id} - Update bucket
        - DELETE /api/buckets/{id} - Delete bucket
        - POST /api/buckets/{id}/upload - Upload files
        - DELETE /api/buckets/{id}/files - Delete file
        - GET /raw/{bucket_id}/{file_path} - Download raw file (public)
        - GET /api/buckets/{id}/zip - Download ZIP (public)
        - GET /api/buckets/{id}/summary - LLM-friendly summary (public)
        - GET /s/{short_code} - Short URL redirect (public)
        """;

    [HttpGet("llms.txt")]
    public IActionResult GetLlmsTxt() => Content(LlmsTxt, "text/plain");

    [HttpGet("docs/api.md")]
    public IActionResult GetApiDocs() => Content(LlmsTxt, "text/markdown");
}
