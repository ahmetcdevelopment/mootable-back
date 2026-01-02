using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mootable.IntegrationTests.Fixtures;
using System.Collections.Concurrent;
using Xunit;

namespace Mootable.IntegrationTests.Hubs;

/// <summary>
/// Integration tests for MootTableHub
/// </summary>
public class MootTableHubTests : IClassFixture<MootableWebApplicationFactory>, IAsyncLifetime
{
    private readonly MootableWebApplicationFactory _factory;
    private HubConnection? _hubConnection1;
    private HubConnection? _hubConnection2;
    private readonly Guid _testUserId1 = Guid.NewGuid();
    private readonly Guid _testUserId2 = Guid.NewGuid();
    private readonly Guid _testMootTableId = Guid.NewGuid();

    public MootTableHubTests(MootableWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        // Setup hub connections for testing
        var client = _factory.CreateClient();
        var baseUrl = client.BaseAddress!.ToString().Replace("http://", "ws://");

        // Create connection for user 1
        _hubConnection1 = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}hubs/moot-table", options =>
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
            .WithUrl($"{baseUrl}hubs/moot-table", options =>
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
    public async Task JoinMootTable_ShouldNotifyOtherUsers()
    {
        // Arrange
        var notifications = new ConcurrentBag<dynamic>();
        var tcs = new TaskCompletionSource();

        _hubConnection2!.On<dynamic>("UserJoinedMootTable", notification =>
        {
            notifications.Add(notification);
            tcs.SetResult();
        });

        // Act
        await _hubConnection1!.InvokeAsync("JoinMootTable", _testMootTableId);

        // Wait for notification with timeout
        await Task.WhenAny(tcs.Task, Task.Delay(5000));

        // Assert
        notifications.Should().HaveCount(1);
        var notification = notifications.First();
        ((string)notification.UserId).Should().Be(_testUserId1.ToString());
        ((string)notification.MootTableId).Should().Be(_testMootTableId.ToString());
    }

    [Fact]
    public async Task LeaveMootTable_ShouldNotifyOtherUsers()
    {
        // Arrange
        await _hubConnection1!.InvokeAsync("JoinMootTable", _testMootTableId);
        await _hubConnection2!.InvokeAsync("JoinMootTable", _testMootTableId);

        var notifications = new ConcurrentBag<dynamic>();
        var tcs = new TaskCompletionSource();

        _hubConnection2!.On<dynamic>("UserLeftMootTable", notification =>
        {
            notifications.Add(notification);
            tcs.SetResult();
        });

        // Act
        await _hubConnection1!.InvokeAsync("LeaveMootTable", _testMootTableId);

        // Wait for notification with timeout
        await Task.WhenAny(tcs.Task, Task.Delay(5000));

        // Assert
        notifications.Should().HaveCount(1);
        var notification = notifications.First();
        ((string)notification.UserId).Should().Be(_testUserId1.ToString());
        ((string)notification.MootTableId).Should().Be(_testMootTableId.ToString());
    }

    [Fact]
    public async Task SendTypingIndicator_ShouldNotifyOthersInGroup()
    {
        // Arrange
        await _hubConnection1!.InvokeAsync("JoinMootTable", _testMootTableId);
        await _hubConnection2!.InvokeAsync("JoinMootTable", _testMootTableId);

        var notifications = new ConcurrentBag<dynamic>();
        var tcs = new TaskCompletionSource();

        _hubConnection2!.On<dynamic>("TypingIndicator", notification =>
        {
            notifications.Add(notification);
            tcs.SetResult();
        });

        // Act
        await _hubConnection1!.InvokeAsync("SendTypingIndicator", _testMootTableId, true);

        // Wait for notification with timeout
        await Task.WhenAny(tcs.Task, Task.Delay(5000));

        // Assert
        notifications.Should().HaveCount(1);
        var notification = notifications.First();
        ((string)notification.UserId).Should().Be(_testUserId1.ToString());
        ((string)notification.MootTableId).Should().Be(_testMootTableId.ToString());
        ((bool)notification.IsTyping).Should().BeTrue();
    }

    [Fact]
    public async Task SendMessage_ShouldBroadcastToGroup()
    {
        // Arrange
        await _hubConnection1!.InvokeAsync("JoinMootTable", _testMootTableId);
        await _hubConnection2!.InvokeAsync("JoinMootTable", _testMootTableId);

        var receivedMessages = new ConcurrentBag<dynamic>();
        var tcs = new TaskCompletionSource();

        _hubConnection2!.On<dynamic>("ReceiveMessage", message =>
        {
            receivedMessages.Add(message);
            tcs.SetResult();
        });

        var testMessage = new
        {
            Content = "Test message",
            AuthorId = _testUserId1.ToString(),
            Timestamp = DateTime.UtcNow
        };

        // Act
        await _hubConnection1!.InvokeAsync("SendMessage", _testMootTableId, testMessage);

        // Wait for message with timeout
        await Task.WhenAny(tcs.Task, Task.Delay(5000));

        // Assert
        receivedMessages.Should().HaveCount(1);
        var receivedMessage = receivedMessages.First();
        ((string)receivedMessage.Content).Should().Be("Test message");
        ((string)receivedMessage.AuthorId).Should().Be(_testUserId1.ToString());
    }

    [Fact]
    public async Task UserConnected_ShouldNotifyOthers()
    {
        // Arrange
        var notifications = new ConcurrentBag<string>();
        var tcs = new TaskCompletionSource();

        _hubConnection1!.On<string>("UserConnected", userId =>
        {
            notifications.Add(userId);
            tcs.SetResult();
        });

        // Act - Create and connect a new user
        var client = _factory.CreateClient();
        var baseUrl = client.BaseAddress!.ToString().Replace("http://", "ws://");
        var newUserId = Guid.NewGuid();

        await using var newConnection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}hubs/moot-table", options =>
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
        notifications.Should().Contain(newUserId.ToString());
    }

    [Fact]
    public async Task UserDisconnected_ShouldNotifyOthers()
    {
        // Arrange
        var notifications = new ConcurrentBag<string>();
        var tcs = new TaskCompletionSource();

        _hubConnection1!.On<string>("UserDisconnected", userId =>
        {
            notifications.Add(userId);
            tcs.SetResult();
        });

        // Create a temporary connection
        var client = _factory.CreateClient();
        var baseUrl = client.BaseAddress!.ToString().Replace("http://", "ws://");
        var tempUserId = Guid.NewGuid();

        var tempConnection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}hubs/moot-table", options =>
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
        notifications.Should().Contain(tempUserId.ToString());
    }
}