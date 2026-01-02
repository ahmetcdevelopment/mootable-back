using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Mootable.IntegrationTests.Fixtures;
using System.Collections.Concurrent;
using Xunit;

namespace Mootable.IntegrationTests.Hubs;

/// <summary>
/// Integration tests for PresenceHub
/// </summary>
public class PresenceHubTests : IClassFixture<MootableWebApplicationFactory>, IAsyncLifetime
{
    private readonly MootableWebApplicationFactory _factory;
    private HubConnection? _hubConnection1;
    private HubConnection? _hubConnection2;
    private readonly Guid _testUserId1 = Guid.NewGuid();
    private readonly Guid _testUserId2 = Guid.NewGuid();
    private readonly Guid _testServerId = Guid.NewGuid();

    public PresenceHubTests(MootableWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        var client = _factory.CreateClient();
        var baseUrl = client.BaseAddress!.ToString().Replace("http://", "ws://");

        // Create connection for user 1
        _hubConnection1 = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}hubs/presence", options =>
            {
                options.Headers.Add("X-Test-User", _testUserId1.ToString());
                options.Headers.Add("X-Test-Username", "TestUser1");
                options.Headers.Add("X-Test-Roles", "User");
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Information))
            .Build();

        // Create connection for user 2
        _hubConnection2 = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}hubs/presence", options =>
            {
                options.Headers.Add("X-Test-User", _testUserId2.ToString());
                options.Headers.Add("X-Test-Username", "TestUser2");
                options.Headers.Add("X-Test-Roles", "User");
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Information))
            .Build();

        await _hubConnection1.StartAsync();
        await _hubConnection2.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_hubConnection1 != null)
        {
            await _hubConnection1.DisposeAsync();
        }
        if (_hubConnection2 != null)
        {
            await _hubConnection2.DisposeAsync();
        }
    }

    [Fact]
    public async Task Connection_ShouldBeEstablished()
    {
        // Assert
        _hubConnection1!.State.Should().Be(HubConnectionState.Connected);
        _hubConnection2!.State.Should().Be(HubConnectionState.Connected);
    }

    [Fact]
    public async Task UserOnline_ShouldNotifyOthers_WhenConnected()
    {
        // Arrange
        var notifications = new ConcurrentBag<dynamic>();
        var tcs = new TaskCompletionSource();

        _hubConnection1!.On<dynamic>("UserOnline", notification =>
        {
            notifications.Add(notification);
            tcs.SetResult();
        });

        // Act - Create and connect a new user
        var client = _factory.CreateClient();
        var baseUrl = client.BaseAddress!.ToString().Replace("http://", "ws://");
        var newUserId = Guid.NewGuid();

        await using var newConnection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}hubs/presence", options =>
            {
                options.Headers.Add("X-Test-User", newUserId.ToString());
                options.Headers.Add("X-Test-Username", "NewUser");
                options.Headers.Add("X-Test-Roles", "User");
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await newConnection.StartAsync();

        // Wait for notification with timeout
        await Task.WhenAny(tcs.Task, Task.Delay(5000));

        // Assert
        notifications.Should().HaveCount(1);
        var notification = notifications.First();
        ((string)notification.UserId).Should().Be(newUserId.ToString());
    }

    [Fact]
    public async Task UserOffline_ShouldNotifyOthers_WhenDisconnected()
    {
        // Arrange
        var notifications = new ConcurrentBag<dynamic>();
        var tcs = new TaskCompletionSource();

        _hubConnection1!.On<dynamic>("UserOffline", notification =>
        {
            notifications.Add(notification);
            tcs.SetResult();
        });

        // Create a temporary connection
        var client = _factory.CreateClient();
        var baseUrl = client.BaseAddress!.ToString().Replace("http://", "ws://");
        var tempUserId = Guid.NewGuid();

        var tempConnection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}hubs/presence", options =>
            {
                options.Headers.Add("X-Test-User", tempUserId.ToString());
                options.Headers.Add("X-Test-Username", "TempUser");
                options.Headers.Add("X-Test-Roles", "User");
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await tempConnection.StartAsync();

        // Act - Disconnect the temporary connection
        await tempConnection.DisposeAsync();

        // Wait for notification with timeout
        await Task.WhenAny(tcs.Task, Task.Delay(5000));

        // Assert
        notifications.Should().HaveCount(1);
        var notification = notifications.First();
        ((string)notification.UserId).Should().Be(tempUserId.ToString());
    }

    [Fact]
    public async Task Heartbeat_ShouldBeReceived()
    {
        // Act & Assert - Should not throw
        var act = async () => await _hubConnection1!.InvokeAsync("Heartbeat");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateStatus_ShouldNotifyOthers()
    {
        // Arrange
        var notifications = new ConcurrentBag<dynamic>();
        var tcs = new TaskCompletionSource();

        _hubConnection2!.On<dynamic>("UserStatusChanged", notification =>
        {
            notifications.Add(notification);
            tcs.SetResult();
        });

        // Act
        await _hubConnection1!.InvokeAsync("UpdateStatus", "Away");

        // Wait for notification with timeout
        await Task.WhenAny(tcs.Task, Task.Delay(5000));

        // Assert
        notifications.Should().HaveCount(1);
        var notification = notifications.First();
        ((string)notification.UserId).Should().Be(_testUserId1.ToString());
        ((string)notification.Status).Should().Be("Away");
    }

    [Fact]
    public async Task UpdateStatus_ShouldIgnoreInvalidStatus()
    {
        // Arrange
        var notifications = new ConcurrentBag<dynamic>();

        _hubConnection2!.On<dynamic>("UserStatusChanged", notification =>
        {
            notifications.Add(notification);
        });

        // Act - Send invalid status
        await _hubConnection1!.InvokeAsync("UpdateStatus", "InvalidStatus");

        // Wait a bit to ensure no notification is sent
        await Task.Delay(1000);

        // Assert
        notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task JoinServerPresence_ShouldAddToGroup()
    {
        // Act & Assert - Should not throw
        var act = async () => await _hubConnection1!.InvokeAsync("JoinServerPresence", _testServerId);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LeaveServerPresence_ShouldRemoveFromGroup()
    {
        // Arrange
        await _hubConnection1!.InvokeAsync("JoinServerPresence", _testServerId);

        // Act & Assert - Should not throw
        var act = async () => await _hubConnection1!.InvokeAsync("LeaveServerPresence", _testServerId);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("Online")]
    [InlineData("Away")]
    [InlineData("DoNotDisturb")]
    [InlineData("Offline")]
    public async Task UpdateStatus_ShouldAcceptAllValidStatuses(string status)
    {
        // Arrange
        var notifications = new ConcurrentBag<dynamic>();
        var tcs = new TaskCompletionSource();

        _hubConnection2!.On<dynamic>("UserStatusChanged", notification =>
        {
            notifications.Add(notification);
            tcs.SetResult();
        });

        // Act
        await _hubConnection1!.InvokeAsync("UpdateStatus", status);

        // Wait for notification with timeout
        await Task.WhenAny(tcs.Task, Task.Delay(5000));

        // Assert
        notifications.Should().HaveCount(1);
        var notification = notifications.First();
        ((string)notification.Status).Should().Be(status);
    }
}