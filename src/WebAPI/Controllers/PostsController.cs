using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mootable.Application.Features.Posts.Commands;
using Mootable.Application.Features.Posts.DTOs;
using Mootable.Application.Features.Posts.Queries;

namespace Mootable.WebAPI.Controllers;

/// <summary>
/// Wonderland Posts Controller
/// Matrix'ten çıkış: Düşüncelerini paylaş, gerçeği keşfet
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PostsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get posts for Wonderland feed
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPosts([FromQuery] GetPostsQuery query)
    {
        var result = await _mediator.Send(query);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get a single post by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPost(Guid id, [FromQuery] bool includeReplies = true)
    {
        var query = new GetPostByIdQuery
        {
            Id = id,
            IncludeReplies = includeReplies
        };

        var result = await _mediator.Send(query);
        return result.Succeeded ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Create a new post in Wonderland
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
    {
        var command = new CreatePostCommand
        {
            Content = dto.Content,
            HtmlContent = dto.HtmlContent,
            Category = dto.Category,
            Tags = dto.Tags,
            MediaUrls = dto.MediaUrls,
            Visibility = dto.Visibility,
            ParentPostId = dto.ParentPostId
        };

        var result = await _mediator.Send(command);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetPost), new { id = result.Data?.Id }, result)
            : BadRequest(result);
    }

    /// <summary>
    /// Update an existing post
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdatePostDto dto)
    {
        var command = new UpdatePostCommand
        {
            Id = id,
            Content = dto.Content,
            HtmlContent = dto.HtmlContent,
            Category = dto.Category,
            Tags = dto.Tags,
            MediaUrls = dto.MediaUrls
        };

        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Delete a post (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var command = new DeletePostCommand { Id = id };
        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Like or unlike a post (Red Pill / Blue Pill)
    /// </summary>
    [HttpPost("{id}/like")]
    public async Task<IActionResult> LikePost(Guid id, [FromBody] LikePostDto dto)
    {
        var command = new LikePostCommand
        {
            PostId = id,
            LikeType = dto.LikeType ?? "RedPill"
        };

        var result = await _mediator.Send(command);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get posts by category
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetPostsByCategory(
        string category,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetPostsQuery
        {
            Category = category,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Search posts
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchPosts(
        [FromQuery] string searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetPostsQuery
        {
            SearchTerm = searchTerm,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get trending posts (highest enlightenment score)
    /// </summary>
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrendingPosts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetPostsQuery
        {
            SortBy = PostSortBy.Enlightened,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}

// DTOs for controller
public class UpdatePostDto
{
    public string Content { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> MediaUrls { get; set; } = new();
}

public class LikePostDto
{
    public string? LikeType { get; set; }
}