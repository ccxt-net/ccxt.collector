# Deployment Guide

## Publishing to NuGet

### Prerequisites
1. **.NET SDK** - Install from https://dotnet.microsoft.com/download
2. **NuGet API Key** - Get from https://www.nuget.org/account/apikeys
3. **PowerShell** - Pre-installed on Windows

### Quick Publish
```bash
# From project root
dotnet pack src/ccxt.collector.csproj -c Release
dotnet nuget push bin/Release/*.nupkg -k YOUR_API_KEY -s https://api.nuget.org/v3/index.json
```

### Version Management
Before publishing, update version in `src/ccxt.collector.csproj`:
```xml
<Version>2.0.0</Version>
<AssemblyVersion>2.0.0.0</AssemblyVersion>
<FileVersion>2.0.0.0</FileVersion>
```

## Linux Deployment (Ubuntu)

### Build and Publish
```bash
# Build for Ubuntu
dotnet restore -r ubuntu.18.04-x64
dotnet publish -c Release -r ubuntu.18.04-x64 -f net8.0 -o /var/ccxt.collector

# Or for Ubuntu 20.04
dotnet publish -c Release -r ubuntu.20.04-x64 -f net8.0 -o /var/ccxt.collector
```

### Supervisor Configuration
Create `/etc/supervisor/conf.d/ccxt.collector.conf`:
```ini
[program:ccxt.collector]
command=/usr/bin/dotnet /var/ccxt.collector/ccxt.collector.dll
directory=/var/ccxt.collector/
autostart=true
autorestart=true
stderr_logfile=/var/log/ccxt.collector.err.log
stdout_logfile=/var/log/ccxt.collector.out.log
environment=Hosting:Environment=Production
startsecs=10
startretries=3
stopsignal=SIGINT
```

### Start/Stop Service
```bash
# Start
sudo supervisorctl start ccxt.collector

# Stop
sudo supervisorctl stop ccxt.collector

# Restart
sudo supervisorctl restart ccxt.collector

# Check status
sudo supervisorctl status ccxt.collector
```

### Log Rotation
Create `/etc/logrotate.d/ccxt.collector`:
```
/var/log/ccxt.collector.out.log {
    rotate 7
    daily
    missingok
    notifempty
    compress
    delaycompress
    postrotate
        /usr/sbin/service supervisor reload > /dev/null
    endscript
}

/var/log/ccxt.collector.err.log {
    rotate 7
    daily
    missingok
    notifempty
    compress
    delaycompress
}
```

## Docker Deployment

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ccxt.collector.csproj", "src/"]
RUN dotnet restore "src/ccxt.collector.csproj"
COPY . .
WORKDIR "/src/src"
RUN dotnet build "ccxt.collector.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ccxt.collector.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ccxt.collector.dll"]
```

### Docker Compose
```yaml
version: '3.8'

services:
  ccxt-collector:
    build: .
    restart: unless-stopped
    environment:
      - Hosting:Environment=Production
    volumes:
      - ./appsettings.json:/app/appsettings.json:ro
      - ./logs:/app/logs
    networks:
      - ccxt-network

  rabbitmq:
    image: rabbitmq:3-management
    restart: unless-stopped
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      - RABBITMQ_DEFAULT_USER=guest
      - RABBITMQ_DEFAULT_PASS=guest
    networks:
      - ccxt-network

networks:
  ccxt-network:
    driver: bridge
```

### Docker Commands
```bash
# Build
docker build -t ccxt-collector .

# Run
docker run -d --name ccxt-collector \
  -v $(pwd)/appsettings.json:/app/appsettings.json:ro \
  ccxt-collector

# With Docker Compose
docker-compose up -d

# View logs
docker logs -f ccxt-collector

# Stop
docker stop ccxt-collector
```

## Windows Service Deployment

### Install as Windows Service
```powershell
# Create service
sc.exe create "CCXT.Collector" binPath="C:\ccxt\ccxt.collector.exe"

# Configure service
sc.exe config "CCXT.Collector" start=auto
sc.exe description "CCXT.Collector" "Real-time cryptocurrency data collector"

# Start service
sc.exe start "CCXT.Collector"

# Stop service
sc.exe stop "CCXT.Collector"

# Delete service
sc.exe delete "CCXT.Collector"
```

## Cloud Deployments

### Azure App Service
```bash
# Create resource group
az group create --name ccxt-rg --location eastus

# Create app service plan
az appservice plan create --name ccxt-plan --resource-group ccxt-rg --sku B1 --is-linux

# Create web app
az webapp create --resource-group ccxt-rg --plan ccxt-plan --name ccxt-collector --runtime "DOTNET|8.0"

# Deploy
az webapp deployment source config-zip --resource-group ccxt-rg --name ccxt-collector --src publish.zip
```

### AWS Elastic Beanstalk
```bash
# Initialize EB
eb init -p docker ccxt-collector

# Create environment
eb create ccxt-env

# Deploy
eb deploy

# Open application
eb open
```

### Kubernetes
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ccxt-collector
spec:
  replicas: 3
  selector:
    matchLabels:
      app: ccxt-collector
  template:
    metadata:
      labels:
        app: ccxt-collector
    spec:
      containers:
      - name: ccxt-collector
        image: ccxt-collector:latest
        ports:
        - containerPort: 80
        env:
        - name: Hosting__Environment
          value: "Production"
        volumeMounts:
        - name: config
          mountPath: /app/appsettings.json
          subPath: appsettings.json
      volumes:
      - name: config
        configMap:
          name: ccxt-config
---
apiVersion: v1
kind: Service
metadata:
  name: ccxt-collector-service
spec:
  selector:
    app: ccxt-collector
  ports:
  - port: 80
    targetPort: 80
  type: LoadBalancer
```

## Environment Configuration

### appsettings.Production.json
```json
{
  "appsettings": {
    "websocket.retry.waiting.milliseconds": "1000",
    "websocket.retry.max.attempts": "10",
    "use.auto.start": "true",
    "auto.start.exchange.name": "binance",
    "auto.start.symbol.names": "BTC/USDT,ETH/USDT"
  },
  "rabbitmq": {
    "enabled": "true",
    "hostName": "rabbitmq",
    "port": "5672",
    "virtualHost": "/",
    "userName": "guest",
    "password": "guest"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

## Monitoring

### Health Check Endpoint
```csharp
// Add to Startup.cs
services.AddHealthChecks()
    .AddCheck("websocket", new WebSocketHealthCheck())
    .AddRabbitMQ(rabbitConnectionString);

app.UseHealthChecks("/health");
```

### Application Insights (Azure)
```xml
<PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.21.0" />
```

```csharp
services.AddApplicationInsightsTelemetry(Configuration["ApplicationInsights:InstrumentationKey"]);
```

### Prometheus Metrics
```csharp
app.UseHttpMetrics();
app.UseMetricServer();
```

## Security Considerations

1. **API Keys**: Never commit API keys. Use environment variables or secure vaults
2. **HTTPS**: Always use HTTPS in production
3. **Firewall**: Restrict ports to only necessary services
4. **Updates**: Keep .NET runtime and dependencies updated
5. **Logging**: Don't log sensitive information

## Troubleshooting

### Common Issues

**Port Already in Use**
```bash
# Find process using port
netstat -ano | findstr :5000
# Kill process
taskkill /PID <PID> /F
```

**Permission Denied (Linux)**
```bash
# Grant execute permission
chmod +x /var/ccxt.collector/ccxt.collector
# Change ownership
chown -R www-data:www-data /var/ccxt.collector
```

**Service Won't Start**
- Check logs in `/var/log/ccxt.collector.err.log`
- Verify configuration in `appsettings.json`
- Ensure all dependencies are installed
- Check firewall settings

---
*For additional deployment scenarios or support, contact support@ccxt.net*