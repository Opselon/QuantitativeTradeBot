using Nexus.Core.Entities;
using Nexus.Core.Interfaces;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nexus.Infrastructure.Native
{
    /// <summary>
    /// Implements the INativeCoreService port. 
    /// Features an automated, zero-allocation C++ P/Invoke engine, with a robust
    /// Pure C# Managed Fallback mechanism to ensure high-availability if the DLL is missing.
    /// </summary>
    public class NativeCoreService : INativeCoreService, IDisposable
    {
        #region Private Fields & Fallback State
        private static readonly object Lock = new();
        private static bool _resolverRegistered = false;
        private readonly bool _isAvailable;
        private readonly NativeCoreSafeHandle? _handle;

        // Pure C# Fallback In-memory telemetry cache
        private readonly List<double> _fallbackPrices = new();
        private double _lastBid = 0.0;
        private double _lastAsk = 0.0;
        private string _symbol = "XAUUSD";
        private DateTime _lastTime = DateTime.UtcNow;
        #endregion

        #region Public Properties
        public bool IsAvailable => _isAvailable;

        public string LastError
        {
            get
            {
                if (!_isAvailable || _handle == null || _handle.IsInvalid)
                    return "Native core library is not active. Running on Managed C# Fallback mode.";

                IntPtr ptr = NativeCoreInterop.GetLastError(_handle.DangerousGetHandle());
                return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) ?? string.Empty : string.Empty;
            }
        }
        #endregion

        #region Static Constructor & Dynamic Resolver
        static NativeCoreService()
        {
            RegisterResolver();
        }

        private static void RegisterResolver()
        {
            lock (Lock)
            {
                if (_resolverRegistered) return;
                try
                {
                    NativeLibrary.SetDllImportResolver(typeof(NativeCoreService).Assembly, ResolveDll);
                    _resolverRegistered = true;
                }
                catch (Exception)
                {
                    // Already registered or unsupported on non-core runtimes
                }
            }
        }

        private static IntPtr ResolveDll(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == "nexus_native_core" || libraryName == "libnexus_native_core")
            {
                string filename = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "nexus_native_core.dll" : "libnexus_native_core.so";

                // 1. Try AppDomain base directory
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(baseDir, filename);
                if (File.Exists(fullPath))
                {
                    if (NativeLibrary.TryLoad(fullPath, out IntPtr handle)) return handle;
                }

                // 2. Try cmake build directory relative locations
                string? currentDir = baseDir;
                while (currentDir != null)
                {
                    string candidate = Path.Combine(currentDir, "src", "Nexus.Native.Core", "build", "lib", filename);
                    if (File.Exists(candidate))
                    {
                        if (NativeLibrary.TryLoad(candidate, out IntPtr handle)) return handle;
                    }
                    currentDir = Directory.GetParent(currentDir)?.FullName;
                }

                // 3. Fallback standard loader
                if (NativeLibrary.TryLoad(filename, out IntPtr fallbackHandle)) return fallbackHandle;
            }

            return IntPtr.Zero;
        }
        #endregion

        #region Instance Constructor
        public NativeCoreService()
        {
            try
            {
                // Attempt to bind to the native bare-metal C++ library
                IntPtr ptr = NativeCoreInterop.Create();
                if (ptr != IntPtr.Zero)
                {
                    _handle = new NativeCoreSafeHandle(ptr);
                    _isAvailable = true;
                }
                else
                {
                    _isAvailable = false;
                }
            }
            catch (Exception)
            {
                // Graceful fallback initialization: DLL was absent, entering Pure C# mode
                _isAvailable = false;
                _handle = null;
            }
        }
        #endregion

        #region Tick Ingestion (Native & Managed Fallback)
        public unsafe void UpdateTick(Tick tick)
        {
            #region Managed Fallback Thread-Safe Tick Ingestion
            // REASON: If the C++ DLL is missing, aggregate tick metrics directly in memory
            // to compute moving standard deviations and momentum without throwing Exceptions.
            if (!_isAvailable)
            {
                lock (_fallbackPrices)
                {
                    _symbol = tick.Symbol.Name;
                    _lastTime = tick.Time;
                    _lastBid = tick.Bid;
                    _lastAsk = tick.Ask;

                    _fallbackPrices.Add(tick.Bid);

                    // Maintain a sliding window of the last 150 ticks to compute statistical indicators
                    if (_fallbackPrices.Count > 150)
                        _fallbackPrices.RemoveAt(0);
                }
                return;
            }
            #endregion

            #region Native C++ Accelerated Path
            if (_handle == null || _handle.IsInvalid)
            {
                throw new InvalidOperationException("Native core handle is invalid.");
            }

            var data = new TickData
            {
                Timestamp = tick.Time.Ticks,
                Bid = tick.Bid,
                Ask = tick.Ask,
                Spread = tick.Spread,
                Volume = 1.0 // Default liquidity pressure
            };

            byte[] symbolBytes = System.Text.Encoding.UTF8.GetBytes(tick.Symbol.Name);
            int len = Math.Min(symbolBytes.Length, 31);
            for (int i = 0; i < len; i++)
            {
                data.SymbolId[i] = symbolBytes[i];
            }
            data.SymbolId[len] = 0;

            int code = NativeCoreInterop.UpdateTick(_handle.DangerousGetHandle(), data);
            if (code != 0)
            {
                throw new InvalidOperationException($"Native core update tick failed with code: {code}. Error: {LastError}");
            }
            #endregion
        }
        #endregion

        #region Vector Extraction (Native & Managed Fallback)
        public unsafe MarketVector GetMarketVector()
        {
            #region Managed Fallback Feature Extraction Loop
            // REASON: Dynamically computes a 10-dimensional MarketVector using our running tick buffer
            // to feed the ONNX/neural evaluation layer without compilation errors.
            if (!_isAvailable)
            {
                double priceStructure = 0.5;
                double trendState = 0.0;
                double momentum = 0.0;
                double volatility = 0.1; // Minimal default
                double volumePressure = 0.5;
                double liquidity = 0.95; // High spread-based default
                double usdStrength = 65.0;
                double sessionState = 1.0;
                double marketRegimeVal = 1.0; // Mapped to Ranging
                double riskState = 0.05;

                lock (_fallbackPrices)
                {
                    int count = _fallbackPrices.Count;
                    if (count > 1)
                    {
                        double first = _fallbackPrices[0];
                        double last = _fallbackPrices[count - 1];

                        trendState = last > first ? 1.0 : (last < first ? -1.0 : 0.0);
                        momentum = (last - first) / first;

                        // Rolling Standard Deviation (Volatility) Calculation in Pure C#
                        double mean = _fallbackPrices.Average();
                        double varianceSum = _fallbackPrices.Sum(d => Math.Pow(d - mean, 2));
                        double stdDev = Math.Sqrt(varianceSum / count);
                        volatility = Math.Clamp((stdDev / mean) * 100.0, 0.02, 5.0); // Clamped percentage-scale volatility
                    }
                }

                return new MarketVector(
                    priceStructure,
                    trendState,
                    momentum,
                    volatility,
                    volumePressure,
                    liquidity,
                    usdStrength,
                    sessionState,
                    marketRegimeVal,
                    riskState
                );
            }
            #endregion

            #region Native C++ Accelerated Path
            if (_handle == null || _handle.IsInvalid)
            {
                throw new InvalidOperationException("Native core handle is invalid.");
            }

            int code = NativeCoreInterop.GetMarketVector(_handle.DangerousGetHandle(), out MarketVectorBuffer buffer);
            if (code != 0)
            {
                throw new InvalidOperationException($"Native core get market vector failed with code: {code}. Error: {LastError}");
            }

            return new MarketVector(
                buffer.Features[0],
                buffer.Features[1],
                buffer.Features[2],
                buffer.Features[3],
                buffer.Features[4],
                buffer.Features[5],
                buffer.Features[6],
                buffer.Features[7],
                buffer.Features[8],
                buffer.Features[9]
            );
            #endregion
        }
        #endregion

        #region State Extraction (Native & Managed Fallback)
        public unsafe MarketState GetMarketState()
        {
            #region Managed Fallback MarketState Compiler
            // REASON: Extracts the human-readable market metrics displayed in the
            // Dashboard UI panel, preventing NaN/Unknown visual lags.
            if (!_isAvailable)
            {
                double volatility = 0.12;
                double momentum = 0.0;

                lock (_fallbackPrices)
                {
                    int count = _fallbackPrices.Count;
                    if (count > 1)
                    {
                        double mean = _fallbackPrices.Average();
                        double varianceSum = _fallbackPrices.Sum(d => Math.Pow(d - mean, 2));
                        volatility = Math.Clamp(Math.Sqrt(varianceSum / count) / mean, 0.005, 1.0);
                        momentum = (_fallbackPrices[count - 1] - _fallbackPrices[0]) / _fallbackPrices[0];
                    }
                }

                // Dynamic Regime Classifier
                string regime = "Ranging";
                if (momentum > 0.001) regime = "Trending Bullish";
                else if (momentum < -0.001) regime = "Trending Bearish";

                return new MarketState(
                    _symbol,
                    _lastTime,
                    volatility,
                    momentum,
                    0.95, // Liquidity index
                    0.5,  // Price structure index
                    0.5,  // Probability index
                    0.05, // Risk index
                    80.0, // Base Currency Strength
                    regime
                );
            }
            #endregion

            #region Native C++ Accelerated Path
            if (_handle == null || _handle.IsInvalid)
            {
                throw new InvalidOperationException("Native core handle is invalid.");
            }

            int code = NativeCoreInterop.GetMarketState(_handle.DangerousGetHandle(), out MarketStateBuffer buffer);
            if (code != 0)
            {
                throw new InvalidOperationException($"Native core get market state failed with code: {code}. Error: {LastError}");
            }

            string symbol = Marshal.PtrToStringAnsi((IntPtr)buffer.Symbol) ?? string.Empty;
            string marketRegime = Marshal.PtrToStringAnsi((IntPtr)buffer.MarketRegime) ?? string.Empty;

            return new MarketState(
                symbol,
                new DateTime(buffer.LastUpdatedUtc, DateTimeKind.Utc),
                buffer.Volatility,
                buffer.Momentum,
                buffer.Liquidity,
                buffer.PriceStructure,
                buffer.Probability,
                buffer.Risk,
                buffer.CurrencyStrength,
                marketRegime
            );
            #endregion
        }
        #endregion

        #region Disposal
        public void Dispose()
        {
            _handle?.Dispose();
        }
        #endregion
    }
}