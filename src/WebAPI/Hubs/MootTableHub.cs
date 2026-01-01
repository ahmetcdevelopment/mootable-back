using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Mootable.WebAPI.Hubs;

/// <summary>
/// MootTable (tartışma masası) için real-time SignalR Hub.
/// 
/// SIGNALR MİMARİSİ (PRODUCTION DENEYİMİ):
/// 
/// 1. CONNECTION LIFECYCLE:
///    - OnConnectedAsync: User online olduğunda, ilgili server'lara bildir
///    - OnDisconnectedAsync: Graceful disconnect vs connection drop ayrımı önemli
/// 
/// 2. GROUP MANAGEMENT:
///    - Her MootTable bir Group
///    - User MootTable'a girdiğinde JoinMootTable
///    - User çıktığında LeaveMootTable
///    - Group üzerinden broadcast = sadece o MootTable'daki kullanıcılara
/// 
/// 3. RECONNECT HANDLING (KRİTİK):
///    - SignalR client disconnect olduğunda otomatik reconnect dener
///    - Reconnect başarılı olduğunda client state'i senkronize edilmeli
///    - Yoksa: User yeni mesajları kaçırır
/// 
/// ANTI-PATTERN:
/// Hub içinde DbContext kullanmak.
/// Connection sayısı arttığında DbContext pool tükenir.
/// Hub sadece message routing yapmalı, business logic handler'da.
/// </summary>
[Authorize]
public sealed class MootTableHub : Hub
{
    private readonly ILogger<MootTableHub> _logger;

    public MootTableHub(ILogger<MootTableHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        _logger.LogInformation("User {UserId} connected to MootTableHub", userId);
        
        await Clients.Others.SendAsync("UserConnected", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        
        if (exception != null)
        {
            _logger.LogWarning(exception, "User {UserId} disconnected with error", userId);
        }
        else
        {
            _logger.LogInformation("User {UserId} disconnected gracefully", userId);
        }

        await Clients.Others.SendAsync("UserDisconnected", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinMootTable(Guid mootTableId)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        var groupName = GetMootTableGroupName(mootTableId);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserJoinedMootTable", new { UserId = userId, MootTableId = mootTableId });
        
        _logger.LogDebug("User {UserId} joined MootTable {MootTableId}", userId, mootTableId);
    }

    public async Task LeaveMootTable(Guid mootTableId)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        var groupName = GetMootTableGroupName(mootTableId);
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Group(groupName).SendAsync("UserLeftMootTable", new { UserId = userId, MootTableId = mootTableId });
        
        _logger.LogDebug("User {UserId} left MootTable {MootTableId}", userId, mootTableId);
    }

    public async Task SendTypingIndicator(Guid mootTableId, bool isTyping)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        var groupName = GetMootTableGroupName(mootTableId);
        
        await Clients.OthersInGroup(groupName).SendAsync("TypingIndicator", new 
        { 
            UserId = userId, 
            MootTableId = mootTableId, 
            IsTyping = isTyping 
        });
    }

    public async Task SendMessage(Guid mootTableId, object message)
    {
        var groupName = GetMootTableGroupName(mootTableId);
        await Clients.Group(groupName).SendAsync("ReceiveMessage", message);
    }

    private static string GetMootTableGroupName(Guid mootTableId) => $"moot-table:{mootTableId}";
}
