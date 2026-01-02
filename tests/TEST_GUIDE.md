# SignalR Hub Test Guide

## Overview
This guide explains how to test the SignalR hubs in the Mootable project.

## Test Projects

### 1. Mootable.IntegrationTests
Automated integration tests for SignalR hubs using xUnit and WebApplicationFactory.

### 2. Mootable.SignalRTestClient
Interactive console application for manual testing of SignalR hubs.

## Running Integration Tests

```bash
# Navigate to test directory
cd mootable-back/tests/Mootable.IntegrationTests

# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~MootTableHubTests"
```

## Running Manual Test Client

### Prerequisites
1. Start the backend server:
```bash
cd mootable-back/src/WebAPI
dotnet run
```

2. Ensure you have a test user in the database:
- Email: test@test.com
- Password: Test123!

### Running the Test Client

```bash
cd mootable-back/tests/Mootable.SignalRTestClient
dotnet run
```

The test client will:
1. Login to get JWT token
2. Connect to MootTable Hub
3. Test hub methods (Join, Send Message, Leave)
4. Display real-time events

## Test Scenarios Covered

### MootTableHub Tests
- ✅ Connection establishment
- ✅ Join/Leave MootTable
- ✅ Typing indicator
- ✅ Send/Receive messages
- ✅ User connected/disconnected notifications

### PresenceHub Tests
- ✅ User online/offline notifications
- ✅ Status updates (Online, Away, DoNotDisturb, Offline)
- ✅ Heartbeat mechanism
- ✅ Server presence groups

### RabbitHoleHub Tests
- ✅ Join/Leave RabbitHole
- ✅ Message sending in threads
- ✅ RabbitHole resolved/locked notifications

## Test Architecture

```
Tests/
├── Fixtures/
│   ├── MootableWebApplicationFactory.cs  # Test server setup
│   └── TestAuthenticationHandler.cs      # Bypass JWT for tests
│
├── Hubs/
│   ├── MootTableHubTests.cs             # MootTable hub tests
│   ├── PresenceHubTests.cs              # Presence hub tests
│   └── RabbitHoleHubTests.cs            # RabbitHole hub tests
│
└── SignalRTestClient/
    └── Program.cs                        # Manual test client
```

## Troubleshooting

### Common Issues

1. **Connection refused**
   - Ensure backend is running on http://localhost:5000
   - Check firewall settings

2. **Authentication errors**
   - Verify test user exists in database
   - Check JWT configuration in appsettings.json

3. **Hub methods not working**
   - Check hub is registered in Program.cs
   - Verify authorization attributes

## CI/CD Integration

Add to your CI/CD pipeline:

```yaml
# GitHub Actions example
- name: Run Integration Tests
  run: |
    cd mootable-back
    dotnet test tests/Mootable.IntegrationTests
```

## Performance Testing

For load testing SignalR hubs, consider using:
- Artillery with SignalR plugin
- JMeter with WebSocket sampler
- Custom load test using multiple test clients

## Security Testing

Ensure to test:
- JWT token validation
- Authorization for hub methods
- Rate limiting
- Connection limits
- Message size limits

## Next Steps

1. Add more edge case tests
2. Implement performance benchmarks
3. Add security penetration tests
4. Set up continuous monitoring