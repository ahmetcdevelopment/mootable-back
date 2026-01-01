using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mootable.Application.Features.MootTables.Commands.CreateMootTable;
using Mootable.Application.Features.MootTables.Queries.GetMootTable;

namespace Mootable.WebAPI.Controllers;

[Authorize]
public class MootTablesController : BaseApiController
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateMootTableResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateMootTable([FromBody] CreateMootTableRequest request)
    {
        var command = new CreateMootTableCommand(
            request.ServerId,
            request.Name,
            request.Topic,
            request.CategoryId
        );
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetMootTable), new { mootTableId = result.MootTableId }, result);
    }

    [HttpGet("{mootTableId:guid}")]
    [ProducesResponseType(typeof(GetMootTableResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMootTable(Guid mootTableId)
    {
        var query = new GetMootTableQuery(mootTableId);
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("server/{serverId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServerMootTables(Guid serverId)
    {
        return Ok(Array.Empty<object>());
    }

    [HttpGet("{mootTableId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(
        Guid mootTableId,
        [FromQuery] int limit = 50,
        [FromQuery] Guid? before = null)
    {
        return Ok(Array.Empty<object>());
    }

    [HttpPost("{mootTableId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage(Guid mootTableId, [FromBody] SendMessageRequest request)
    {
        return CreatedAtAction(nameof(GetMessages), new { mootTableId }, new { });
    }
}

public sealed record CreateMootTableRequest(Guid ServerId, string Name, string? Topic, Guid? CategoryId);
public sealed record SendMessageRequest(string Content, Guid? ReplyToId);
