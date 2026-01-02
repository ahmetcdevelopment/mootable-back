using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mootable.Application.Features.RabbitHoles.Commands;
using Mootable.Application.Features.RabbitHoles.Queries;
using System;
using System.Threading.Tasks;

namespace Mootable.WebAPI.Controllers
{
    /// <summary>
    /// Rabbit Hole Controller - Topic-specific discussion channels
    /// Matrix theme: "Follow the white rabbit into topics"
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RabbitHolesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RabbitHolesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get all public rabbit holes
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetRabbitHoles([FromQuery] GetRabbitHolesQuery query)
        {
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Create a new rabbit hole
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateRabbitHole([FromBody] CreateRabbitHoleCommand command)
        {
            var result = await _mediator.Send(command);
            return Created($"/api/rabbit-holes/{result.Slug}", result);
        }

        /// <summary>
        /// Follow or unfollow a rabbit hole
        /// </summary>
        [HttpPost("{rabbitHoleId}/follow")]
        public async Task<IActionResult> FollowRabbitHole(Guid rabbitHoleId, [FromBody] FollowRabbitHoleCommand command)
        {
            command.RabbitHoleId = rabbitHoleId;
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Get posts from a rabbit hole
        /// </summary>
        [HttpGet("{rabbitHoleId}/posts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRabbitHolePosts(Guid rabbitHoleId, [FromQuery] GetRabbitHolePostsQuery query)
        {
            query.RabbitHoleId = rabbitHoleId;
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Create a post in a rabbit hole
        /// </summary>
        [HttpPost("{rabbitHoleId}/posts")]
        public async Task<IActionResult> CreatePost(Guid rabbitHoleId, [FromBody] CreateRabbitHolePostCommand command)
        {
            command.RabbitHoleId = rabbitHoleId;
            var result = await _mediator.Send(command);
            return Created($"/api/rabbit-holes/{rabbitHoleId}/posts/{result.Id}", result);
        }
    }
}
