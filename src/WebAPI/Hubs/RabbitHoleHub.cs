using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Mootable.WebAPI.Hubs;

/// <summary>
/// RabbitHole (thread) için ayrı SignalR Hub.
/// 
/// NEDEN AYRI HUB:
/// MootTable ve RabbitHole farklı lifecycle'lara sahip.
/// - MootTable: Kullanıcı server'dayken sürekli bağlı
/// - RabbitHole: Kullanıcı thread'e girdiğinde bağlı, çıkınca disconnect
/// 
/// Tek hub'da yönetmek = connection state karmaşası.
/// Ayrı hub = net separation of concerns.
/// 
/// SIGNALR RECONNECT EDGE CASE:
/// User RabbitHole'dayken disconnect olursa:
/// 1. Client reconnect olur
/// 2. Eski group membership kaybolur
/// 3. Client yeniden JoinRabbitHole çağırmalı
/// 
/// Bu durumu handle etmek için client tarafında:
/// - onreconnected event'inde aktif RabbitHole varsa yeniden join
/// </summary>
[Authorize]
public sealed class RabbitHoleHub : Hub
{
    private readonly ILogger<RabbitHoleHub> _logger;

    public RabbitHoleHub(ILogger<RabbitHoleHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        _logger.LogInformation("User {UserId} connected to RabbitHoleHub", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        _logger.LogInformation("User {UserId} disconnected from RabbitHoleHub", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRabbitHole(Guid rabbitHoleId)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        var groupName = GetRabbitHoleGroupName(rabbitHoleId);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserJoinedRabbitHole", new 
        { 
            UserId = userId, 
            RabbitHoleId = rabbitHoleId 
        });
        
        _logger.LogDebug("User {UserId} joined RabbitHole {RabbitHoleId}", userId, rabbitHoleId);
    }

    public async Task LeaveRabbitHole(Guid rabbitHoleId)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        var groupName = GetRabbitHoleGroupName(rabbitHoleId);
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserLeftRabbitHole", new 
        { 
            UserId = userId, 
            RabbitHoleId = rabbitHoleId 
        });
        
        _logger.LogDebug("User {UserId} left RabbitHole {RabbitHoleId}", userId, rabbitHoleId);
    }

    public async Task SendTypingIndicator(Guid rabbitHoleId, bool isTyping)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        var groupName = GetRabbitHoleGroupName(rabbitHoleId);
        
        await Clients.OthersInGroup(groupName).SendAsync("TypingIndicator", new 
        { 
            UserId = userId, 
            RabbitHoleId = rabbitHoleId, 
            IsTyping = isTyping 
        });
    }

    public async Task SendMessage(Guid rabbitHoleId, object message)
    {
        var groupName = GetRabbitHoleGroupName(rabbitHoleId);
        await Clients.Group(groupName).SendAsync("ReceiveMessage", message);
    }

    public async Task NotifyRabbitHoleResolved(Guid rabbitHoleId)
    {
        var groupName = GetRabbitHoleGroupName(rabbitHoleId);
        await Clients.Group(groupName).SendAsync("RabbitHoleResolved", new { RabbitHoleId = rabbitHoleId });
    }

    public async Task NotifyRabbitHoleLocked(Guid rabbitHoleId)
    {
        var groupName = GetRabbitHoleGroupName(rabbitHoleId);
        await Clients.Group(groupName).SendAsync("RabbitHoleLocked", new { RabbitHoleId = rabbitHoleId });
    }

    private static string GetRabbitHoleGroupName(Guid rabbitHoleId) => $"rabbit-hole:{rabbitHoleId}";
}
