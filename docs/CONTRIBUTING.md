# Contributing to CCXT.Collector

Thank you for your interest in contributing to CCXT.Collector! We welcome contributions from the community and are grateful for any help you can provide.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Development Process](#development-process)
- [Coding Standards](#coding-standards)
- [Testing](#testing)
- [Documentation](#documentation)
- [Pull Request Process](#pull-request-process)
- [Community](#community)

## Code of Conduct

This project adheres to a Code of Conduct that all contributors are expected to follow. Please be respectful and professional in all interactions.

### Our Standards

- Be respectful and inclusive
- Welcome newcomers and help them get started
- Focus on constructive criticism
- Accept responsibility and apologize for mistakes
- Learn from the experience

## Getting Started

### Prerequisites

- .NET 8.0 or .NET 9.0 SDK
- Visual Studio 2022 or Visual Studio Code
- Git
- Basic knowledge of C# and async/await patterns

### Setting Up Your Development Environment

1. **Fork the Repository**
   ```bash
   # Fork on GitHub, then clone your fork
   git clone https://github.com/YOUR_USERNAME/ccxt.collector.git
   cd ccxt.collector
   ```

2. **Add Upstream Remote**
   ```bash
   git remote add upstream https://github.com/ccxt-net/ccxt.collector.git
   ```

3. **Install Dependencies**
   ```bash
   dotnet restore
   ```

4. **Build the Project**
   ```bash
   dotnet build
   ```

5. **Run Tests**
   ```bash
   dotnet test
   ```

## How to Contribute

### Types of Contributions

#### 🐛 Bug Reports
- Use the GitHub Issues tab
- Provide a clear description of the bug
- Include steps to reproduce
- Specify your environment (OS, .NET version, etc.)
- Include relevant code snippets or error messages

#### ✨ Feature Requests
- Open a GitHub Issue with the "enhancement" label
- Clearly describe the feature and its use case
- Explain why this feature would be useful
- Provide examples if possible

#### 📝 Documentation
- Improve existing documentation
- Add examples and tutorials
- Translate documentation to other languages
- Fix typos and clarify confusing sections

#### 💻 Code Contributions
- Fix bugs
- Implement new features
- Add new exchange support
- Improve performance
- Add technical indicators

### Finding Issues to Work On

Look for issues labeled with:
- `good first issue` - Great for newcomers
- `help wanted` - We need help with these
- `bug` - Known bugs that need fixing
- `enhancement` - New features or improvements

## Development Process

### Branch Strategy

We use a simplified Git flow:

- `master` - Stable release branch
- `develop` - Development branch for next release
- `feature/*` - Feature branches
- `bugfix/*` - Bug fix branches
- `hotfix/*` - Urgent fixes for production

### Creating a Feature Branch

```bash
git checkout develop
git pull upstream develop
git checkout -b feature/your-feature-name
```

### Exchange Implementation Guide

When adding a new exchange:

1. **Create WebSocket Client**
   ```
   src/exchanges/[exchange_name]/
   ├── [ExchangeName]WebSocketClient.cs  # WebSocket implementation
   ├── config.cs                         # Configuration (optional)
   ├── logger.cs                         # Logging (optional)
   └── [legacy files]                    # REST API fallback (optional)
   ```

2. **Implement WebSocket Client**
   ```csharp
   public class NewExchangeWebSocketClient : WebSocketClientBase
   {
       public override string ExchangeName => "NewExchange";
       protected override string WebSocketUrl => "wss://...";
       protected override int PingIntervalMs => 60000;
       
       protected override async Task ProcessMessageAsync(string message)
       {
           // Parse and process WebSocket messages
       }
       
       public override async Task<bool> SubscribeOrderbookAsync(string symbol)
       {
           // Implement subscription logic
       }
       
       // Implement other required methods...
   }
   ```

3. **Follow Unified Data Model**
   - Use `SOrderBooks` for orderbook data
   - Use `SCompleteOrders` for trade data
   - Use `STicker` for ticker data
   - Convert exchange format to unified format in `ProcessMessageAsync`

4. **Add Tests**
   Create test file in `tests/exchanges/[ExchangeName]Tests.cs`:
   ```csharp
   [TestClass]
   public class NewExchangeTests
   {
       [TestMethod]
       public async Task Test_WebSocket_Connection() { }
       
       [TestMethod]
       public async Task Test_Orderbook_Stream() { }
       
       // Add more tests...
   }
   ```

5. **Create Sample**
   Add example in `samples/exchanges/[ExchangeName]Sample.cs`

## Coding Standards

### C# Style Guide

We follow the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) with some additions:

#### Naming Conventions
```csharp
// Classes, Interfaces, Methods: PascalCase
public class ExchangeClient { }
public interface IDataHandler { }
public void ProcessData() { }

// Local variables, parameters: camelCase
string symbolName = "BTC/USDT";
void ProcessTicker(string symbol) { }

// Private fields: _camelCase with underscore
private readonly string _apiKey;

// Constants: UPPER_CASE
public const int MAX_RECONNECT_ATTEMPTS = 5;
```

#### Code Organization
```csharp
public class ExchangeClient
{
    // 1. Fields
    private readonly string _apiKey;
    
    // 2. Properties
    public string ExchangeName { get; set; }
    
    // 3. Events
    public event Action<Ticker> OnTickerReceived;
    
    // 4. Constructors
    public ExchangeClient(string apiKey) { }
    
    // 5. Public methods
    public async Task ConnectAsync() { }
    
    // 6. Private methods
    private void ProcessData() { }
}
```

#### Async/Await Best Practices
```csharp
// Always use Async suffix for async methods
public async Task<Data> GetDataAsync()
{
    // Configure await where appropriate
    await ProcessAsync().ConfigureAwait(false);
}

// Use CancellationToken for cancellable operations
public async Task LongRunningOperation(CancellationToken cancellationToken)
{
    await Task.Delay(1000, cancellationToken);
}
```

### WebSocket Implementation Standards

```csharp
public class ExchangeWebSocket
{
    // Always implement reconnection logic
    private async Task HandleDisconnection()
    {
        await Task.Delay(_reconnectDelay);
        await ReconnectAsync();
    }
    
    // Implement proper error handling
    private void OnError(Exception ex)
    {
        _logger.LogError(ex, "WebSocket error");
        // Attempt recovery
    }
    
    // Use heartbeat/ping-pong to maintain connection
    private async Task SendHeartbeat()
    {
        await _webSocket.SendAsync("ping");
    }
}
```

## Testing

### Test Requirements

- All new features must have unit tests
- Bug fixes should include regression tests
- Maintain test coverage above 80%
- Integration tests for exchange connections

### Writing Tests

```csharp
[TestClass]
public class IndicatorTests
{
    [TestMethod]
    public void RSI_Calculate_ReturnsValidValue()
    {
        // Arrange
        var rsi = new RSI(14);
        var data = GenerateTestData();
        
        // Act
        var result = rsi.Calculate(data);
        
        // Assert
        Assert.IsTrue(result >= 0 && result <= 100);
    }
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter Category=Unit

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Documentation

### Code Documentation

Use XML documentation comments:

```csharp
/// <summary>
/// Connects to the exchange WebSocket API
/// </summary>
/// <param name="symbols">List of symbols to subscribe</param>
/// <returns>Task representing the connection operation</returns>
/// <exception cref="ConnectionException">Thrown when connection fails</exception>
public async Task ConnectAsync(List<string> symbols)
{
    // Implementation
}
```

### README Updates

When adding new features, update the README:
- Add to feature list if significant
- Update code examples if API changes
- Add to supported exchanges table

### Wiki Contributions

For detailed documentation, contribute to the wiki:
- Architecture explanations
- Detailed guides
- Troubleshooting tips

## Pull Request Process

### Before Submitting

1. **Update from upstream**
   ```bash
   git fetch upstream
   git rebase upstream/develop
   ```

2. **Run tests**
   ```bash
   dotnet test
   ```

3. **Check code style**
   ```bash
   dotnet format
   ```

4. **Update documentation**

### PR Guidelines

1. **Title Format**: `[Type] Brief description`
   - Types: `[Feature]`, `[Bug]`, `[Docs]`, `[Refactor]`, `[Test]`
   - Example: `[Feature] Add Kraken exchange support`

2. **Description Template**:
   ```markdown
   ## Description
   Brief description of changes
   
   ## Type of Change
   - [ ] Bug fix
   - [ ] New feature
   - [ ] Breaking change
   - [ ] Documentation update
   
   ## Testing
   - [ ] Unit tests pass
   - [ ] Integration tests pass
   - [ ] Manual testing completed
   
   ## Checklist
   - [ ] Code follows style guidelines
   - [ ] Self-review completed
   - [ ] Documentation updated
   - [ ] No new warnings
   ```

3. **Keep PRs focused** - One feature/fix per PR

4. **Respond to feedback** - Address review comments promptly

### Review Process

1. Automated checks must pass
2. At least one maintainer review required
3. All feedback addressed
4. Final approval from maintainer
5. Squash and merge to develop

## Community

### Communication Channels

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: General discussions and questions
- **Discord**: Real-time chat (if available)
- **Email**: ccxt.net@gmail.com

### Getting Help

- Check existing issues and discussions
- Read the documentation and wiki
- Ask clear, specific questions
- Provide context and examples

### Recognition

Contributors are recognized in:
- The README.md Contributors section
- Release notes
- Special thanks in major releases

## License

By contributing to CCXT.Collector, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to CCXT.Collector! Your efforts help make cryptocurrency exchange integration easier for developers worldwide.