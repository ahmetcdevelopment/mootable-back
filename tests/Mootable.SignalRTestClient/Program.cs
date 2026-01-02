using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Mootable.SignalRTestClient;

/// <summary>
/// Interactive SignalR Hub Test Client - Simplified Version
/// This console application allows manual testing of SignalR hubs
/// </summary>
class Program
{
    private static string _baseUrl = "http://localhost:5000";
    private static string? _accessToken;
    private static HubConnection? _mootTableHub;
    private static HttpClient _httpClient = new HttpClient();

    static async Task Main(string[] args)
    {
        Console.WriteLine("=====================================");
        Console.WriteLine("  MOOTABLE SIGNALR TEST CLIENT");
        Console.WriteLine("=====================================\n");

        _httpClient.BaseAddress = new Uri(_baseUrl);

        // Simple test flow
        Console.WriteLine("Starting SignalR Test...\n");

        // Step 1: Login
        Console.WriteLine("Step 1: Logging in...");
        await LoginAsync("test@test.com", "Test123!");

        if (string.IsNullOrEmpty(_accessToken))
        {
            Console.WriteLine("Login failed. Please check credentials and server.");
            return;
        }

        // Step 2: Connect to Hub
        Console.WriteLine("\nStep 2: Connecting to MootTable Hub...");
        await ConnectToHubAsync();

        // Step 3: Test Hub Methods
        Console.WriteLine("\nStep 3: Testing Hub Methods...");
        await TestHubMethodsAsync();

        Console.WriteLine("\nPress any key to disconnect and exit...");
        Console.ReadKey();

        // Cleanup
        if (_mootTableHub != null)
        {
            await _mootTableHub.DisposeAsync();
        }
    }

    static async Task LoginAsync(string email, string password)
    {
        try
        {
            var loginData = new
            {
                email,
                password,
                ipAddress = "127.0.0.1"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(loginData),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseContent)!;
                _accessToken = result.accessToken;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Login successful! User: {result.username}");
                Console.ResetColor();

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _accessToken);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Login failed: {response.StatusCode}");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Login error: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task ConnectToHubAsync()
    {
        try
        {
            _mootTableHub = new HubConnectionBuilder()
                .WithUrl($"{_baseUrl}/hubs/moot-table", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_accessToken);
                })
                .WithAutomaticReconnect()
                .Build();

            // Setup event handlers
            _mootTableHub.On<string>("UserConnected", userId =>
            {
                Console.WriteLine($"[Event] User connected: {userId}");
            });

            _mootTableHub.On<string>("UserDisconnected", userId =>
            {
                Console.WriteLine($"[Event] User disconnected: {userId}");
            });

            _mootTableHub.On<dynamic>("UserJoinedMootTable", data =>
            {
                Console.WriteLine($"[Event] User joined MootTable: {JsonConvert.SerializeObject(data)}");
            });

            _mootTableHub.On<dynamic>("ReceiveMessage", message =>
            {
                Console.WriteLine($"[Event] Message received: {JsonConvert.SerializeObject(message)}");
            });

            await _mootTableHub.StartAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Connected to MootTable Hub");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Connection failed: {ex.Message}");
            Console.ResetColor();
        }
    }

    static async Task TestHubMethodsAsync()
    {
        if (_mootTableHub?.State != HubConnectionState.Connected)
        {
            Console.WriteLine("Hub is not connected!");
            return;
        }

        try
        {
            var testMootTableId = Guid.NewGuid();

            // Test 1: Join MootTable
            Console.WriteLine($"\nTest 1: Joining MootTable {testMootTableId}");
            await _mootTableHub.InvokeAsync("JoinMootTable", testMootTableId);
            await Task.Delay(500); // Wait for event

            // Test 2: Send Typing Indicator
            Console.WriteLine($"Test 2: Sending typing indicator");
            await _mootTableHub.InvokeAsync("SendTypingIndicator", testMootTableId, true);
            await Task.Delay(500);

            // Test 3: Send Message
            Console.WriteLine($"Test 3: Sending message");
            var message = new
            {
                Content = "Hello from test client!",
                Timestamp = DateTime.UtcNow,
                AuthorId = "TestUser"
            };
            await _mootTableHub.InvokeAsync("SendMessage", testMootTableId, message);
            await Task.Delay(500);

            // Test 4: Leave MootTable
            Console.WriteLine($"Test 4: Leaving MootTable");
            await _mootTableHub.InvokeAsync("LeaveMootTable", testMootTableId);
            await Task.Delay(500);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✓ All tests completed successfully!");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ Test failed: {ex.Message}");
            Console.ResetColor();
        }
    }
}