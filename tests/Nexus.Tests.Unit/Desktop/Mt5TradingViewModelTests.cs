using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Nexus.Application.Ports;
using Nexus.Application.Workflows.DTOs;
using Nexus.Desktop.Models;
using Nexus.Desktop.Services;
using Nexus.Desktop.ViewModels;

namespace Nexus.Tests.Unit.Desktop
{
    public class Mt5TradingViewModelTests
    {
        [Fact]
        public async Task Refresh_Success_PopulatesPositionsAndSetsState()
        {
            // Arrange
            var positions = new List<DesktopPositionDto>
            {
                new DesktopPositionDto
                {
                    Ticket = 1001,
                    Symbol = "EURUSD",
                    Side = "Buy",
                    Volume = 0.5m,
                    OpenPrice = 1.08500m,
                    CurrentPrice = 1.08600m,
                    Profit = 50.00m,
                    Swap = 0.00m,
                    OpenTime = DateTime.UtcNow.AddMinutes(-5)
                }
            };

            var stubOperator = new StubOperatorService { PositionsToReturn = positions };
            var stubDiagnostics = new StubDiagnosticService();
            var stubConfig = new StubAppConfigurationService();
            var stubBridge = new StubBridgeService();

            using var viewModel = new Mt5TradingViewModel(stubOperator, stubDiagnostics, stubConfig, stubBridge);

            // Act
            await viewModel.RefreshPositionsCommand.ExecuteAsync(null);

            // Assert
            Assert.True(viewModel.IsConnected);
            Assert.Equal("Connected", viewModel.ConnectionStatusText);
            Assert.Single(viewModel.OpenPositions);
            Assert.Equal(1001, viewModel.OpenPositions[0].Ticket);
            Assert.Equal("EURUSD", viewModel.OpenPositions[0].Symbol);
            Assert.Contains("Refresh completed in", stubDiagnostics.LoggedMessages[0]);
        }

        [Fact]
        public async Task Refresh_Failure_SetsDisconnectedStateAndErrorMessage()
        {
            // Arrange
            var stubOperator = new StubOperatorService { ThrowOnGetPositions = new Exception("Connection lost") };
            var stubDiagnostics = new StubDiagnosticService();
            var stubConfig = new StubAppConfigurationService();
            var stubBridge = new StubBridgeService();

            using var viewModel = new Mt5TradingViewModel(stubOperator, stubDiagnostics, stubConfig, stubBridge);

            // Act
            await viewModel.RefreshPositionsCommand.ExecuteAsync(null);

            // Assert
            Assert.False(viewModel.IsConnected);
            Assert.Equal("Disconnected", viewModel.ConnectionStatusText);
            Assert.Empty(viewModel.OpenPositions);
            Assert.Equal("Connection lost", viewModel.ErrorMessage);
            Assert.Equal("Refresh failed.", viewModel.StatusMessage);
            Assert.Contains("Refresh failed", stubDiagnostics.LoggedMessages[0]);
        }

        [Fact]
        public async Task Buy_Success_ExecutesTradeAndRefreshesPositions()
        {
            // Arrange
            var stubOperator = new StubOperatorService();
            var stubDiagnostics = new StubDiagnosticService();
            var stubConfig = new StubAppConfigurationService();
            var stubBridge = new StubBridgeService();

            using var viewModel = new Mt5TradingViewModel(stubOperator, stubDiagnostics, stubConfig, stubBridge)
            {
                SelectedSymbol = "EURUSD",
                OrderVolume = 1.0m
            };

            // Act
            await viewModel.BuyCommand.ExecuteAsync(null);

            // Assert
            Assert.True(stubOperator.PlaceOrderCalled);
            Assert.Equal("EURUSD", stubOperator.PlaceOrderSymbol);
            Assert.Equal(DesktopOrderSide.Buy, stubOperator.PlaceOrderSide);
            Assert.Equal(1.0m, stubOperator.PlaceOrderVolume);
            Assert.True(stubOperator.GetPositionsCalled); // Ensure it refreshed
        }

        [Fact]
        public async Task Buy_Failure_DisplaysErrorMessageAndLogsWarning()
        {
            // Arrange
            var stubOperator = new StubOperatorService
            {
                PlaceOrderResultToReturn = new DesktopTradeResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Trade rejected: Invalid price"
                }
            };
            var stubDiagnostics = new StubDiagnosticService();
            var stubConfig = new StubAppConfigurationService();
            var stubBridge = new StubBridgeService();

            using var viewModel = new Mt5TradingViewModel(stubOperator, stubDiagnostics, stubConfig, stubBridge)
            {
                SelectedSymbol = "EURUSD",
                OrderVolume = 0.5m
            };

            // Act
            await viewModel.BuyCommand.ExecuteAsync(null);

            // Assert
            Assert.True(stubOperator.PlaceOrderCalled);
            Assert.Equal("Trade rejected: Invalid price", viewModel.ErrorMessage);
            Assert.Equal("Trade execution failed.", viewModel.StatusMessage);
            Assert.Contains("Success: False", stubDiagnostics.LoggedMessages[0]);
        }

        [Fact]
        public async Task Sell_Success_ExecutesTradeAndRefreshesPositions()
        {
            // Arrange
            var stubOperator = new StubOperatorService();
            var stubDiagnostics = new StubDiagnosticService();
            var stubConfig = new StubAppConfigurationService();
            var stubBridge = new StubBridgeService();

            using var viewModel = new Mt5TradingViewModel(stubOperator, stubDiagnostics, stubConfig, stubBridge)
            {
                SelectedSymbol = "GBPUSD",
                OrderVolume = 2.0m
            };

            // Act
            await viewModel.SellCommand.ExecuteAsync(null);

            // Assert
            Assert.True(stubOperator.PlaceOrderCalled);
            Assert.Equal("GBPUSD", stubOperator.PlaceOrderSymbol);
            Assert.Equal(DesktopOrderSide.Sell, stubOperator.PlaceOrderSide);
            Assert.Equal(2.0m, stubOperator.PlaceOrderVolume);
            Assert.True(stubOperator.GetPositionsCalled);
        }

        [Fact]
        public async Task Close_Success_ClosesSelectedPositionAndRefreshes()
        {
            // Arrange
            var stubOperator = new StubOperatorService();
            var stubDiagnostics = new StubDiagnosticService();
            var stubConfig = new StubAppConfigurationService();
            var stubBridge = new StubBridgeService();

            using var viewModel = new Mt5TradingViewModel(stubOperator, stubDiagnostics, stubConfig, stubBridge);

            var positionVm = new DesktopPositionViewModel(new DesktopPositionDto
            {
                Ticket = 12345,
                Symbol = "EURUSD"
            });

            viewModel.SelectedPosition = positionVm;

            // Act
            await viewModel.CloseCommand.ExecuteAsync(null);

            // Assert
            Assert.True(stubOperator.ClosePositionCalled);
            Assert.Equal(12345, stubOperator.ClosePositionTicket);
            Assert.Equal("EURUSD", stubOperator.ClosePositionSymbol);
            Assert.True(stubOperator.GetPositionsCalled);
        }

        [Theory]
        [InlineData("", 0.1, "Symbol is required.")]
        [InlineData("EURUSD", 0.005, "Volume must be between 0.01 and 100.")]
        [InlineData("EURUSD", 100.5, "Volume must be between 0.01 and 100.")]
        public void Validation_InvalidInputs_DisablesCommandAndShowsMessage(string symbol, decimal volume, string expectedError)
        {
            // Arrange
            var stubOperator = new StubOperatorService();
            var stubDiagnostics = new StubDiagnosticService();
            var stubConfig = new StubAppConfigurationService();
            var stubBridge = new StubBridgeService();

            using var viewModel = new Mt5TradingViewModel(stubOperator, stubDiagnostics, stubConfig, stubBridge);

            // Act
            viewModel.SelectedSymbol = symbol;
            viewModel.OrderVolume = volume;

            // Assert
            Assert.Equal(expectedError, viewModel.ErrorMessage);
            Assert.False(viewModel.BuyCommand.CanExecute(null));
            Assert.False(viewModel.SellCommand.CanExecute(null));
        }

        [Fact]
        public void Validation_ValidInputs_EnablesCommandAndClearsError()
        {
            // Arrange
            var stubOperator = new StubOperatorService();
            var stubDiagnostics = new StubDiagnosticService();
            var stubConfig = new StubAppConfigurationService();
            var stubBridge = new StubBridgeService();

            using var viewModel = new Mt5TradingViewModel(stubOperator, stubDiagnostics, stubConfig, stubBridge);

            // Act
            viewModel.SelectedSymbol = "EURUSD";
            viewModel.OrderVolume = 1.0m;

            // Assert
            Assert.Empty(viewModel.ErrorMessage);
            Assert.True(viewModel.BuyCommand.CanExecute(null));
            Assert.True(viewModel.SellCommand.CanExecute(null));
        }

        [Fact]
        public async Task BusyState_DuringExecution_PreventsDoubleExecution()
        {
            // Arrange
            var stubOperator = new StubOperatorService { DelayMilliseconds = 200 };
            var stubDiagnostics = new StubDiagnosticService();
            var stubConfig = new StubAppConfigurationService();
            var stubBridge = new StubBridgeService();

            using var viewModel = new Mt5TradingViewModel(stubOperator, stubDiagnostics, stubConfig, stubBridge)
            {
                SelectedSymbol = "EURUSD",
                OrderVolume = 1.0m
            };

            // Act - Start trade buy and immediately check states
            var buyTask = viewModel.BuyCommand.ExecuteAsync(null);

            Assert.True(viewModel.IsExecutingTrade);
            Assert.True(viewModel.IsBusy);
            Assert.False(viewModel.BuyCommand.CanExecute(null));

            await buyTask;

            Assert.False(viewModel.IsExecutingTrade);
            Assert.False(viewModel.IsBusy);
        }

        [Fact]
        public async Task Cancellation_DuringTrade_IsHandledGracefully()
        {
            // Arrange
            var stubOperator = new StubOperatorService { ThrowOnPlaceOrder = new OperationCanceledException() };
            var stubDiagnostics = new StubDiagnosticService();
            var stubConfig = new StubAppConfigurationService();
            var stubBridge = new StubBridgeService();

            using var viewModel = new Mt5TradingViewModel(stubOperator, stubDiagnostics, stubConfig, stubBridge)
            {
                SelectedSymbol = "EURUSD",
                OrderVolume = 0.5m
            };

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await viewModel.BuyCommand.ExecuteAsync(null);
            });

            Assert.Equal("Operation cancelled", viewModel.ErrorMessage);
            Assert.Equal("Trade cancelled.", viewModel.StatusMessage);
        }

        // --- STUBS FOR TESTING ---

        private class StubOperatorService : IMt5OperatorService
        {
            public List<DesktopPositionDto> PositionsToReturn { get; set; } = new();
            public DesktopTradeResult PlaceOrderResultToReturn { get; set; } = new() { IsSuccess = true, Ticket = 555 };
            public DesktopTradeResult ClosePositionResultToReturn { get; set; } = new() { IsSuccess = true };

            public Exception? ThrowOnGetPositions { get; set; }
            public Exception? ThrowOnPlaceOrder { get; set; }

            public int DelayMilliseconds { get; set; }

            public bool GetPositionsCalled { get; private set; }
            public bool PlaceOrderCalled { get; private set; }
            public bool ClosePositionCalled { get; private set; }

            public string? PlaceOrderSymbol { get; private set; }
            public DesktopOrderSide PlaceOrderSide { get; private set; }
            public decimal PlaceOrderVolume { get; private set; }

            public long ClosePositionTicket { get; private set; }
            public string? ClosePositionSymbol { get; private set; }

            public async Task<IReadOnlyList<DesktopPositionDto>> GetPositionsAsync(CancellationToken cancellationToken)
            {
                GetPositionsCalled = true;
                if (DelayMilliseconds > 0) await Task.Delay(DelayMilliseconds, cancellationToken);
                if (ThrowOnGetPositions != null) throw ThrowOnGetPositions;
                return PositionsToReturn;
            }

            public async Task<DesktopTradeResult> PlaceOrderAsync(string symbol, DesktopOrderSide side, decimal volume, CancellationToken cancellationToken)
            {
                PlaceOrderCalled = true;
                PlaceOrderSymbol = symbol;
                PlaceOrderSide = side;
                PlaceOrderVolume = volume;

                if (DelayMilliseconds > 0) await Task.Delay(DelayMilliseconds, cancellationToken);
                if (ThrowOnPlaceOrder != null) throw ThrowOnPlaceOrder;
                return PlaceOrderResultToReturn;
            }

            public async Task<DesktopTradeResult> ClosePositionAsync(long ticket, string symbol, CancellationToken cancellationToken)
            {
                ClosePositionCalled = true;
                ClosePositionTicket = ticket;
                ClosePositionSymbol = symbol;

                if (DelayMilliseconds > 0) await Task.Delay(DelayMilliseconds, cancellationToken);
                return ClosePositionResultToReturn;
            }
        }

        private class StubDiagnosticService : IDiagnosticService
        {
            public ObservableCollection<LogEntry> Logs { get; } = new();
            public List<string> LoggedMessages { get; } = new();

            public void Log(string subsystem, string level, string message)
            {
                Logs.Add(new LogEntry { Subsystem = subsystem, Level = level, Message = message });
                LoggedMessages.Add(message);
            }
        }

        private class StubAppConfigurationService : IAppConfigurationService
        {
            public AppSettings Settings { get; set; } = new AppSettings();
            public AppSettings GetSettings() => Settings;
            public void SaveSettings(AppSettings settings) => Settings = settings;
        }

        private class StubBridgeService : IMt5BridgeService
        {
            public bool IsConnected => true;
            public bool IsAuthenticated => true;
            public string ConnectionStatusText => "Connected";
            public double PingLatencyMs => 0;
            public DateTime LastHeartbeatUtc => DateTime.UtcNow;
            public string LastErrorMessage => "";
            public IReadOnlyCollection<string> SubscribedSymbols => new List<string>();
            public bool IsEaPresentInRepository => true;
            public long EaRepositoryFileSize => 0;
            public DateTime EaRepositoryFileLastModifiedUtc => DateTime.UtcNow;
            public string EaRepositoryFilePath => "";
            public bool IsEaInstalledConfirmed { get; set; }
            public bool IsHandshakeSucceeded => true;
            public string EaName => "NexusBridge";
            public string EaVersion => "1.00";
            public string ChartSymbol => "EURUSD";
            public string HandshakeAccountId => "123456";
            public string HandshakeBrokerServer => "Broker";
            public string RemoteEndPoint => "127.0.0.1:5000";

            public BridgeLifecycleState CurrentState { get; set; } = BridgeLifecycleState.Authenticated;

            public event Action<PriceTickEnvelope>? OnTickReceived;
            public event Action<string>? OnStatusChanged;

            public Task ConnectAsync(string host, int port, CancellationToken ct = default) => Task.CompletedTask;
            public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;
            public Task<bool> LoginAsync(string accountId, string password, string brokerServer, CancellationToken ct = default) => Task.FromResult(true);
            public Task<AccountSnapshotDto?> GetAccountSnapshotAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<AccountSnapshotDto?>(new AccountSnapshotDto
                {
                    Balance = 100000,
                    Equity = 100000,
                    Currency = "USD"
                });
            }
            public Task SubscribeSymbolAsync(string symbol, CancellationToken ct = default) => Task.CompletedTask;
            public Task UnsubscribeSymbolAsync(string symbol, CancellationToken ct = default) => Task.CompletedTask;
            public Task<string> AutoDetectAndInstallEaAsync(CancellationToken cancellationToken) => Task.FromResult("");
            public Task ExportEaAsync(string destinationDirectory, CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
