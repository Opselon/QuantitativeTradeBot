// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   APPLICATION LAYER (Ports / Abstractions)
// FILE:    IPythonExecutionService.cs
// REFERENCED BY:
//   - src/Nexus.Desktop/ViewModels/Workspaces/TrainSkillsViewModel.cs
//   - src/Nexus.Infrastructure/Services/PythonExecutionService.cs
// ============================================================================

namespace Nexus.Application.Ports
{
    /// <summary>
    /// Port interface defining execution operations for background python scripts,
    /// enabling real-time streaming of stdout/stderr and process lifecycle controls.
    /// </summary>
    public interface IPythonExecutionService
    {
        /// <summary>
        /// Event fired in real-time whenever a new line is printed to standard output or standard error.
        /// </summary>
        event Action<string>? OutputReceived;

        /// <summary>
        /// Event fired when the background python execution process terminates, returning true if succeeded.
        /// </summary>
        event Action<bool>? ExecutionCompleted;

        /// <summary>
        /// Gets whether a python script execution process is currently active.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Installs the required Python packages (MetaTrader5, pandas, numpy, pyarrow) automatically using pip.
        /// Prevents ModuleNotFoundError crashes on host machines.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to abort installation.</param>
        /// <returns>True if installation succeeded.</returns>
        Task<bool> InstallDependenciesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Thread-safely launches the price action dataset builder script in background headless mode.
        /// </summary>
        /// <param name="symbol">The target symbol name to download (e.g. XAUUSD).</param>
        /// <param name="candleCount">The target quantity of candles to extract.</param>
        /// <param name="cancellationToken">Token to trigger early termination of the running script.</param>
        /// <returns>True if the dataset was built and saved successfully.</returns>
        Task<bool> RunDatasetBuilderAsync(string symbol, int candleCount, CancellationToken cancellationToken);

        /// <summary>
        /// Thread-safely launches the ICT/SMC FVG & OrderBlock dataset builder script in background headless mode.
        /// </summary>
        /// <param name="symbol">The target symbol name to download (e.g. XAUUSD).</param>
        /// <param name="candleCount">The target quantity of candles to extract.</param>
        /// <param name="cancellationToken">Token to trigger early termination of the running script.</param>
        /// <returns>True if the dataset was built and saved successfully.</returns>
        Task<bool> RunIctDatasetBuilderAsync(string symbol, int candleCount, CancellationToken cancellationToken);

        /// <summary>
        /// Forcefully halts the active running python script process.
        /// </summary>
        void StopExecution();
    }
}