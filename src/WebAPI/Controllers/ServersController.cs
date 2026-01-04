using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Features.Servers.Commands;
using Application.Features.Servers.Queries;
using Mootable.Application.Features.Servers.Commands.CreateServer;

namespace Mootable.WebAPI.Controllers;

/// <summary>
/// Server (Nebuchadnezzar/Ship) management endpoints
/// Matrix theme: Every server is a ship in the Matrix
/// </summary>
[Authorize]
public class ServersController : BaseApiController
{
    /// <summary>
    /// Launch a new ship into the Matrix
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateServer([FromBody] CreateServerRequest request)
    {
        var command = new CreateServerCommand(
            request.Name,
            request.Description,
            request.IsPublic
        );

        var result = await Mediator.Send(command);

        return CreatedAtAction(nameof(GetServer), new { serverId = result.ServerId }, result);
    }

    /// <summary>
    /// Locate a ship in the Matrix
    /// </summary>
    [HttpGet("{serverId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetServer(Guid serverId)
    {
        var query = new GetServerQuery(serverId);
        var result = await Mediator.Send(query);

        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Access denied"))
            {
                return Forbid();
            }
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Scan for ships (list user's servers or public servers)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServers([FromQuery] GetServersQueryRequest request)
    {
        var query = new GetServersQuery
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            OnlyMyServers = request.OnlyMyServers,
            OnlyPublic = request.OnlyPublic,
            SearchTerm = request.SearchTerm
        };

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Discover public ships in the Matrix (for discovery page)
    /// Sorted by popularity and access score
    /// </summary>
    [HttpGet("discover")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DiscoverServers([FromQuery] GetPublicServersQueryRequest request)
    {
        var query = new GetPublicServersQuery
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            SearchTerm = request.SearchTerm,
            Category = request.Category,
            SortBy = request.SortBy
        };

        var result = await Mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Board a ship using transmission code (invite code)
    /// </summary>
    [HttpPost("join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinServer([FromBody] JoinServerRequest request)
    {
        var command = new JoinServerCommand
        {
            InviteCode = request.InviteCode
        };

        var result = await Mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Abandon ship (leave server)
    /// </summary>
    [HttpPost("{serverId:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveServer(Guid serverId)
    {
        var command = new LeaveServerCommand(serverId);
        var result = await Mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Destroy ship (delete server - Captain only)
    /// </summary>
    [HttpDelete("{serverId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteServer(Guid serverId)
    {
        var command = new DeleteServerCommand(serverId);
        var result = await Mediator.Send(command);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("Only the Captain")))
            {
                return Forbid();
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get ship decks (channels/MootTables)
    /// </summary>
    [HttpGet("{serverId:guid}/channels")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServerChannels(Guid serverId)
    {
        // Get server details which includes channels
        var query = new GetServerQuery(serverId);
        var result = await Mediator.Send(query);

        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        return Ok(new { data = result.Data!.Channels, succeeded = true });
    }

    /// <summary>
    /// Get ship crew (members)
    /// </summary>
    [HttpGet("{serverId:guid}/members")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServerMembers(Guid serverId)
    {
        // TODO: Implement GetServerMembersQuery for paginated member list
        // For now, return basic member info from server details
        var query = new GetServerQuery(serverId);
        var result = await Mediator.Send(query);

        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        return Ok(new { data = new List<object>(), succeeded = true, memberCount = result.Data!.MemberCount });
    }

    /// <summary>
    /// Get ship roles
    /// </summary>
    [HttpGet("{serverId:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServerRoles(Guid serverId)
    {
        var query = new GetServerQuery(serverId);
        var result = await Mediator.Send(query);

        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        return Ok(new { data = result.Data!.Roles, succeeded = true });
    }

    /// <summary>
    /// Generate new transmission code (invite)
    /// </summary>
    [HttpPost("{serverId:guid}/invites")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GenerateInvite(Guid serverId)
    {
        // TODO: Implement GenerateInviteCommand with proper authorization
        // For now, return the existing invite code from server
        var query = new GetServerQuery(serverId);
        var result = await Mediator.Send(query);

        if (!result.Succeeded || !result.Data!.IsMember)
        {
            return Forbid();
        }

        return Ok(new
        {
            data = new
            {
                inviteCode = result.Data.InviteCode,
                url = $"/invite/{result.Data.InviteCode}"
            },
            succeeded = true
        });
    }
}

// Request DTOs
public sealed record CreateServerRequest(string Name, string? Description, bool IsPublic = false);
public sealed record JoinServerRequest(string InviteCode);
public sealed record GetServersQueryRequest(
    int PageNumber = 1,
    int PageSize = 20,
    bool OnlyMyServers = false,
    bool OnlyPublic = false,
    string? SearchTerm = null
);
public sealed record GetPublicServersQueryRequest(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    string? Category = null,
    DiscoverySortBy SortBy = DiscoverySortBy.Popular
);
