using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Mootable.Application.Interfaces;
using Mootable.Domain.Entities;
using Mootable.Infrastructure.Persistence;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mootable.WebAPI.Hubs
{
    /// <summary>
    /// Matrix Hub - Main SignalR hub for real-time communication
    /// Matrix theme: All connections are "jacking into the Matrix"
    /// </summary>
    [Authorize]
    public class MatrixHub : Hub
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        // Track online users (userId -> connectionId mapping)
        private static readonly ConcurrentDictionary<string, List<string>> _userConnections = new();

        // Track user's current channel (connectionId -> channelId)
        private static readonly ConcurrentDictionary<string, string> _userChannels = new();

        public MatrixHub(
            IApplicationDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier ?? _currentUserService.UserId?.ToString();
            if (string.IsNullOrEmpty(userId))
            {
                await base.OnConnectedAsync();
                return;
            }

            // Add user to connection tracking
            _userConnections.AddOrUpdate(userId,
                new List<string> { Context.ConnectionId },
                (key, list) =>
                {
                    list.Add(Context.ConnectionId);
                    return list;
                });

            // Notify others that user has "jacked in"
            await Clients.Others.SendAsync("UserJackedIn", new
            {
                userId,
                username = Context.User?.Identity?.Name,
                message = "has entered the Matrix",
                timestamp = DateTime.UtcNow
            });

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier ?? _currentUserService.UserId?.ToString();
            if (!string.IsNullOrEmpty(userId))
            {
                // Remove connection
                if (_userConnections.TryGetValue(userId, out var connections))
                {
                    connections.Remove(Context.ConnectionId);
                    if (connections.Count == 0)
                    {
                        _userConnections.TryRemove(userId, out _);

                        // Notify others that user has "unplugged"
                        await Clients.Others.SendAsync("UserUnplugged", new
                        {
                            userId,
                            username = Context.User?.Identity?.Name,
                            message = "has left the Matrix",
                            timestamp = DateTime.UtcNow
                        });
                    }
                }

                // Remove from channel if in one
                if (_userChannels.TryRemove(Context.ConnectionId, out var channelId))
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel_{channelId}");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a MootTable (channel) for real-time messages
        /// </summary>
        public async Task JoinChannel(string channelId)
        {
            var userId = _currentUserService.UserId ?? throw new HubException("User not authenticated");

            // Verify user has access to this channel
            var mootTable = await _context.MootTables
                .Include(mt => mt.Server)
                .ThenInclude(s => s.Members)
                .FirstOrDefaultAsync(mt => mt.Id == Guid.Parse(channelId));

            if (mootTable == null)
            {
                await Clients.Caller.SendAsync("Error", new { message = "Channel not found in the Matrix" });
                return;
            }

            var isMember = mootTable.Server.Members.Any(m => m.UserId == userId);
            if (!isMember)
            {
                await Clients.Caller.SendAsync("Error", new { message = "Access denied. You are not part of this ship's crew." });
                return;
            }

            // Leave previous channel if in one
            if (_userChannels.TryGetValue(Context.ConnectionId, out var previousChannelId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel_{previousChannelId}");
            }

            // Join new channel
            await Groups.AddToGroupAsync(Context.ConnectionId, $"channel_{channelId}");
            _userChannels[Context.ConnectionId] = channelId;

            // Notify channel members
            await Clients.Group($"channel_{channelId}").SendAsync("UserJoinedChannel", new
            {
                userId = userId.ToString(),
                username = Context.User?.Identity?.Name,
                channelId,
                message = "has connected to this transmission deck",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Leave current channel
        /// </summary>
        public async Task LeaveChannel()
        {
            if (_userChannels.TryRemove(Context.ConnectionId, out var channelId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"channel_{channelId}");

                var userId = _currentUserService.UserId?.ToString();
                await Clients.Group($"channel_{channelId}").SendAsync("UserLeftChannel", new
                {
                    userId,
                    username = Context.User?.Identity?.Name,
                    channelId,
                    message = "has disconnected from this transmission deck",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Send a message to the current channel
        /// Matrix theme: "Transmit data through the Matrix"
        /// </summary>
        public async Task SendMessage(string content, string? replyToId = null)
        {
            var userId = _currentUserService.UserId ?? throw new HubException("User not authenticated");

            if (!_userChannels.TryGetValue(Context.ConnectionId, out var channelId))
            {
                await Clients.Caller.SendAsync("Error", new { message = "You must be connected to a channel to transmit" });
                return;
            }

            // Save message to database
            var message = new Message
            {
                Id = Guid.NewGuid(),
                Content = content,
                AuthorId = userId,
                MootTableId = Guid.Parse(channelId),
                ReplyToId = string.IsNullOrEmpty(replyToId) ? null : Guid.Parse(replyToId),
                Type = MessageType.Default,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Get author details
            var author = await _context.Users.FindAsync(userId);

            // Broadcast to channel
            await Clients.Group($"channel_{channelId}").SendAsync("ReceiveMessage", new
            {
                id = message.Id.ToString(),
                content,
                authorId = userId.ToString(),
                authorUsername = author?.Username ?? "Unknown",
                authorAvatarUrl = author?.AvatarUrl,
                channelId,
                replyToId,
                createdAt = message.CreatedAt,
                type = "transmission" // Matrix theme
            });
        }

        /// <summary>
        /// Edit a message
        /// </summary>
        public async Task EditMessage(string messageId, string newContent)
        {
            var userId = _currentUserService.UserId ?? throw new HubException("User not authenticated");

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == Guid.Parse(messageId) && m.AuthorId == userId);

            if (message == null)
            {
                await Clients.Caller.SendAsync("Error", new { message = "Transmission not found or access denied" });
                return;
            }

            message.Content = newContent;
            message.IsEdited = true;
            message.UpdatedBy = userId;
            message.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Broadcast edit to channel
            await Clients.Group($"channel_{message.MootTableId}").SendAsync("MessageEdited", new
            {
                messageId,
                newContent,
                editedAt = message.UpdatedAt,
                message = "Transmission data altered in the Matrix"
            });
        }

        /// <summary>
        /// Delete a message (soft delete)
        /// </summary>
        public async Task DeleteMessage(string messageId)
        {
            var userId = _currentUserService.UserId ?? throw new HubException("User not authenticated");

            var message = await _context.Messages
                .Include(m => m.MootTable)
                .ThenInclude(mt => mt!.Server)
                .FirstOrDefaultAsync(m => m.Id == Guid.Parse(messageId));

            if (message == null)
            {
                await Clients.Caller.SendAsync("Error", new { message = "Transmission not found" });
                return;
            }

            // Check if user can delete (author or server owner)
            var isAuthor = message.AuthorId == userId;
            var isOwner = message.MootTable?.Server?.OwnerId == userId;

            if (!isAuthor && !isOwner)
            {
                await Clients.Caller.SendAsync("Error", new { message = "Access denied. Cannot erase this transmission." });
                return;
            }

            message.IsDeleted = true;
            message.UpdatedBy = userId;
            message.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Broadcast deletion to channel
            await Clients.Group($"channel_{message.MootTableId}").SendAsync("MessageDeleted", new
            {
                messageId,
                deletedBy = Context.User?.Identity?.Name,
                message = "Transmission erased from the Matrix"
            });
        }

        /// <summary>
        /// Send typing indicator
        /// </summary>
        public async Task StartTyping()
        {
            if (_userChannels.TryGetValue(Context.ConnectionId, out var channelId))
            {
                var userId = _currentUserService.UserId?.ToString();
                await Clients.OthersInGroup($"channel_{channelId}").SendAsync("UserTyping", new
                {
                    userId,
                    username = Context.User?.Identity?.Name,
                    channelId,
                    isTyping = true
                });
            }
        }

        /// <summary>
        /// Stop typing indicator
        /// </summary>
        public async Task StopTyping()
        {
            if (_userChannels.TryGetValue(Context.ConnectionId, out var channelId))
            {
                var userId = _currentUserService.UserId?.ToString();
                await Clients.OthersInGroup($"channel_{channelId}").SendAsync("UserTyping", new
                {
                    userId,
                    username = Context.User?.Identity?.Name,
                    channelId,
                    isTyping = false
                });
            }
        }

        /// <summary>
        /// Get online users in a channel
        /// </summary>
        public async Task<List<object>> GetChannelUsers(string channelId)
        {
            var channelUsers = new List<object>();

            foreach (var kvp in _userChannels)
            {
                if (kvp.Value == channelId)
                {
                    // Find userId from connectionId
                    var userId = _userConnections
                        .FirstOrDefault(uc => uc.Value.Contains(kvp.Key))
                        .Key;

                    if (!string.IsNullOrEmpty(userId))
                    {
                        var user = await _context.Users.FindAsync(Guid.Parse(userId));
                        channelUsers.Add(new
                        {
                            userId,
                            username = user?.Username,
                            avatarUrl = user?.AvatarUrl,
                            status = "online"
                        });
                    }
                }
            }

            return channelUsers;
        }

        /// <summary>
        /// React to a message
        /// </summary>
        public async Task ReactToMessage(string messageId, string reactionType)
        {
            var userId = _currentUserService.UserId ?? throw new HubException("User not authenticated");

            var message = await _context.Messages
                .Include(m => m.Reactions)
                .FirstOrDefaultAsync(m => m.Id == Guid.Parse(messageId));

            if (message == null)
            {
                await Clients.Caller.SendAsync("Error", new { message = "Transmission not found" });
                return;
            }

            // Check if user already reacted
            var existingReaction = message.Reactions
                .FirstOrDefault(r => r.UserId == userId && r.Emoji == reactionType);

            if (existingReaction != null)
            {
                // Remove reaction
                _context.MessageReactions.Remove(existingReaction);
                await _context.SaveChangesAsync();

                await Clients.Group($"channel_{message.MootTableId}").SendAsync("ReactionRemoved", new
                {
                    messageId,
                    userId = userId.ToString(),
                    reactionType
                });
            }
            else
            {
                // Add reaction
                var reaction = new MessageReaction
                {
                    Id = Guid.NewGuid(),
                    MessageId = message.Id,
                    UserId = userId,
                    Emoji = reactionType,
                    CreatedAt = DateTime.UtcNow
                };

                _context.MessageReactions.Add(reaction);
                await _context.SaveChangesAsync();

                await Clients.Group($"channel_{message.MootTableId}").SendAsync("ReactionAdded", new
                {
                    messageId,
                    userId = userId.ToString(),
                    username = Context.User?.Identity?.Name,
                    reactionType
                });
            }
        }
    }
}