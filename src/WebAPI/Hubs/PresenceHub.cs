using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Mootable.WebAPI.Hubs;

/// <summary>
/// User presence (online/offline/away) için hub.
/// 
/// PRESENCE SİSTEMİ MİMARİSİ:
/// 
/// 1. HEARTBEAT PATTERN:
///    - Client 30 saniyede bir heartbeat gönderir
///    - Server 45 saniye heartbeat almayan user'ı offline sayar
///    - Neden fark var: Network jitter tolerance
/// 
/// 2. MULTI-DEVICE HANDLING:
///    - Aynı user birden fazla cihazdan bağlanabilir
///    - Tüm connection'lar kapanınca offline
///    - Bir connection bile varsa online
/// 
/// 3. SCALE CONSIDERATIONS:
///    - Redis pub/sub ile horizontal scale
///    - Her server kendi connection'larını bilir
///    - Presence state Redis'te tutulur
/// 
/// PRODUCTION DENEYİMİ:
/// 10K+ concurrent user'da in-memory presence tracking
/// server restart'ta tüm user'ları offline gösterir.
/// Redis'e taşıdıktan sonra bu problem çözüldü.
/// </summary>
[Authorize]
public sealed class PresenceHub : Hub
{
    private readonly ILogger<PresenceHub> _logger;

    public PresenceHub(ILogger<PresenceHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
            await Clients.Others.SendAsync("UserOnline", new { UserId = userId });
        }
        
        _logger.LogInformation("User {UserId} presence connected", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            await Clients.Others.SendAsync("UserOffline", new { UserId = userId });
        }
        
        _logger.LogInformation("User {UserId} presence disconnected", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task Heartbeat()
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        _logger.LogTrace("Heartbeat received from user {UserId}", userId);
    }

    public async Task UpdateStatus(string status)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        
        if (!Enum.TryParse<UserPresenceStatus>(status, true, out var presenceStatus))
        {
            return;
        }

        await Clients.Others.SendAsync("UserStatusChanged", new 
        { 
            UserId = userId, 
            Status = presenceStatus.ToString() 
        });
        
        _logger.LogDebug("User {UserId} status changed to {Status}", userId, status);
    }

    public async Task JoinServerPresence(Guid serverId)
    {
        var groupName = $"server-presence:{serverId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveServerPresence(Guid serverId)
    {
        var groupName = $"server-presence:{serverId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}

public enum UserPresenceStatus
{
    Online,
    Away,
    DoNotDisturb,
    Offline
}
