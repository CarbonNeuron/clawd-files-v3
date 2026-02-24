using ClawdFiles.Application.DTOs;
using ClawdFiles.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClawdFiles.Web.Controllers;

[ApiController]
[Route("api/keys")]
[Authorize(Roles = "Admin")]
public class KeysController(KeyManagementService keyService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ErrorResponse("name is required", "Provide a name for the API key"));
        return Ok(await keyService.CreateKeyAsync(request));
    }

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await keyService.ListKeysAsync());

    [HttpDelete("{prefix}")]
    public async Task<IActionResult> Revoke(string prefix)
    {
        if (!await keyService.RevokeKeyAsync(prefix))
            return NotFound(new ErrorResponse("Key not found", $"No key with prefix '{prefix}'"));
        return Ok(new { message = "Key revoked" });
    }
}
