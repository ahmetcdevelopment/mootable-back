using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mootable.Application.Features.Servers.Commands.CreateServer;

namespace Mootable.WebAPI.Controllers;

[Authorize]
public class ServersController : BaseApiController
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateServerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateServer([FromBody] CreateServerRequest request)
    {
        var command = new CreateServerCommand(request.Name, request.Description, request.IsPublic);
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetServer), new { serverId = result.ServerId }, result);
    }

    [HttpGet("{serverId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServer(Guid serverId)
    {
        return Ok(new { serverId });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserServers()
    {
        return Ok(Array.Empty<object>());
    }

    [HttpPost("{serverId:guid}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinServer(Guid serverId, [FromBody] JoinServerRequest request)
    {
        return Ok(new { serverId, request.InviteCode });
    }

    [HttpPost("{serverId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LeaveServer(Guid serverId)
    {
        return NoContent();
    }
}

public sealed record CreateServerRequest(string Name, string? Description, bool IsPublic = false);
public sealed record JoinServerRequest(string InviteCode);
