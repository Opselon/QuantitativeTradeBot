using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexus.Application.Analytics;
using Nexus.Application.Pipeline;
using Nexus.Application.Ports;
using Nexus.Application.Strategies;
using Nexus.Core.Interfaces;
using Nexus.Infrastructure.Persistence;
using Nexus.Infrastructure.Persistence.Repositories;
using Nexus.Tests.EndToEnd.Mocks;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace Nexus.Tests.EndToEnd.Fixture
{
    public class E2ETestHost : IAsyncDisposable
    {
        private readonly PostgreSqlContainer? _postgresContainer;
        private readonly bool _ownsContainer;
        private readonly bool _reusedContainer;
        private readonly ITestOutputHelper? _outputHelper;
        private bool _dockerAvailable = true;

        public IServiceProvider Services { get; private set; } = null!;
        public string ConnectionString { get; private set; } = string.Empty;
        public bool IsDockerAvailable => _dockerAvailable;
        public PostgreSqlContainer? PostgresContainer => _postgresContainer;

        public SimulatedMarketDataFeed MarketFeed { get; }
        public SimulatedExecutionGateway ExecutionGateway { get; }
        public InMemoryStrategyStateStore StateStore { get; }
        public StrategySupervisor Supervisor { get; private set; } = null!;

        public E2ETestHost(PostgreSqlContainer? postgresContainer = null, bool ownsContainer = true, InMemoryStrategyStateStore? stateStore = null, ITestOutputHelper? outputHelper = null)
        {
            _ownsContainer = ownsContainer;
            _outputHelper = outputHelper;
            if (postgresContainer != null)
            {
                _postgresContainer = postgresContainer;
                _dockerAvailable = true;
                _reusedContainer = true;
                ConnectionString = postgresContainer.GetConnectionString();
            }
            else
            {
                _postgresContainer = new PostgreSqlBuilder("postgres:15-alpine")
                    .WithDatabase("nexus_e2e")
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .Build();
                _reusedContainer = false;
            }

            MarketFeed = new SimulatedMarketDataFeed();
            ExecutionGateway = new SimulatedExecutionGateway();
            StateStore = stateStore ?? new InMemoryStrategyStateStore();
        }

        public async Task InitializeAsync()
        {
            if (_postgresContainer != null && _reusedContainer)
            {
                // Container is already started, we just reuse ConnectionString
                _dockerAvailable = true;
                ConnectionString = _postgresContainer.GetConnectionString();
            }
            else
            {
                try
                {
                    if (_postgresContainer != null)
                    {
                        await _postgresContainer.StartAsync();
                        ConnectionString = _postgresContainer.GetConnectionString();

                        // Initialize Database Schema
                        var scriptsDir = GetScriptsDirectory();
                        var script001 = await File.ReadAllTextAsync(Path.Combine(scriptsDir, "001_create_schema.sql"));
                        var script002 = await File.ReadAllTextAsync(Path.Combine(scriptsDir, "002_create_market_partitions.sql"));
                        var script003 = await File.ReadAllTextAsync(Path.Combine(scriptsDir, "003_create_indexes.sql"));

                        await ExecuteRawSqlAsync(script001);
                        await ExecuteRawSqlAsync(script002);
                        await ExecuteRawSqlAsync(script003);
                    }
                    else
                    {
                        _dockerAvailable = false;
                    }
                }
                catch (Exception ex)
                {
                    _dockerAvailable = false;
                    Console.WriteLine($"WARNING: Docker / Testcontainers is not supported or restricted in this environment: {ex.Message}");
                }
            }

            // Set up DI
            var services = new ServiceCollection();

            // Logging
            services.AddLogging(cfg =>
            {
                if (_outputHelper != null)
                {
                    cfg.AddProvider(new TestOutputLoggerProvider(_outputHelper));
                }
                else
                {
                    cfg.AddConsole();
                }
                cfg.SetMinimumLevel(LogLevel.Debug);
            });

            // Database Context
            if (_dockerAvailable)
            {
                services.AddDbContext<NexusDbContext>(options =>
                    options.UseNpgsql(ConnectionString));
            }
            else
            {
                services.AddDbContext<NexusDbContext>(options =>
                    options.UseInMemoryDatabase("nexus_e2e_fallback"));
            }

            // Core Persistence Repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPositionRepository, PositionRepository>();
            services.AddScoped<IMarketDataRepository, MarketDataRepository>();

            // Gateway & Feed Mocks (Registered as singletons so we can access/interact from test methods)
            services.AddSingleton<IMarketDataFeed>(MarketFeed);
            services.AddSingleton(MarketFeed);
            services.AddSingleton<IExecutionGateway>(ExecutionGateway);
            services.AddSingleton(ExecutionGateway);
            services.AddSingleton<IStrategyStateStore>(StateStore);
            services.AddSingleton(StateStore);

            // Indicator Engines
            services.AddSingleton<INativeAnalyticsEngine, NativeAnalyticsEngine>();
            services.AddSingleton<IIndicatorEngine, NativeIndicatorEngine>();

            // Pipeline Services
            services.AddSingleton<OrderIntentFactory>();
            services.AddSingleton<ExecutionAuditService>();
            services.AddSingleton<DefaultRiskManager>();
            services.AddSingleton<IRiskManager>(sp => sp.GetRequiredService<DefaultRiskManager>());
            // services.AddSingleton<PreTradeRiskEvaluator>();
            services.AddScoped<ExecutionCoordinator>();
            services.AddScoped<SignalRouter>();

            // Strategy Hosting
            services.AddSingleton<StrategySupervisor>();

            Services = services.BuildServiceProvider();
            Supervisor = Services.GetRequiredService<StrategySupervisor>();
        }

        private static string GetScriptsDirectory()
        {
            var currentDir = AppContext.BaseDirectory;
            while (currentDir != null)
            {
                var potentialPath = Path.Combine(currentDir, "src", "Nexus.Infrastructure", "Persistence", "Scripts");
                if (Directory.Exists(potentialPath))
                {
                    return potentialPath;
                }
                currentDir = Directory.GetParent(currentDir)?.FullName;
            }
            throw new DirectoryNotFoundException("Could not find the SQL scripts directory.");
        }

        private async Task ExecuteRawSqlAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (Services is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (Services is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (_dockerAvailable && _ownsContainer && _postgresContainer != null)
            {
                await _postgresContainer.StopAsync();
                await _postgresContainer.DisposeAsync();
            }
        }
    }
}
