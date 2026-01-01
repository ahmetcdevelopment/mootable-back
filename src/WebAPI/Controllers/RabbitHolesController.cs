using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mootable.Application.Features.RabbitHoles.Commands.CreateRabbitHole;

namespace Mootable.WebAPI.Controllers;

[Authorize]
public class RabbitHolesController : BaseApiController
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateRabbitHoleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRabbitHole([FromBody] CreateRabbitHoleRequest request)
    {
        var command = new CreateRabbitHoleCommand(
            request.MootTableId,
            request.StarterMessageId,
            request.Title
        );
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetRabbitHole), new { rabbitHoleId = result.RabbitHoleId }, result);
    }

    [HttpGet("{rabbitHoleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRabbitHole(Guid rabbitHoleId)
    {
        return Ok(new { rabbitHoleId });
    }

    [HttpGet("moot-table/{mootTableId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMootTableRabbitHoles(Guid mootTableId)
    {
        return Ok(Array.Empty<object>());
    }

    [HttpGet("{rabbitHoleId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(
        Guid rabbitHoleId,
        [FromQuery] int limit = 50,
        [FromQuery] Guid? before = null)
    {
        return Ok(Array.Empty<object>());
    }

    [HttpPost("{rabbitHoleId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage(Guid rabbitHoleId, [FromBody] SendRabbitHoleMessageRequest request)
    {
        return CreatedAtAction(nameof(GetMessages), new { rabbitHoleId }, new { });
    }

    [HttpPatch("{rabbitHoleId:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResolveRabbitHole(Guid rabbitHoleId)
    {
        return NoContent();
    }

    [HttpPatch("{rabbitHoleId:guid}/lock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LockRabbitHole(Guid rabbitHoleId)
    {
        return NoContent();
    }
}

public sealed record CreateRabbitHoleRequest(Guid MootTableId, Guid StarterMessageId, string Title);
public sealed record SendRabbitHoleMessageRequest(string Content, Guid? ReplyToId);
