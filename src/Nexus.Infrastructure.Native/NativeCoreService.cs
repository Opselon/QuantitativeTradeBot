using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Nexus.Core.Entities;
using Nexus.Core.Interfaces;
using Nexus.Core.ValueObjects;

namespace Nexus.Infrastructure.Native
{
    public class NativeCoreService : INativeCoreService, IDisposable
    {
        private static readonly object Lock = new();
        private static bool _resolverRegistered = false;
        private readonly bool _isAvailable;
        private readonly NativeCoreSafeHandle? _handle;

        public bool IsAvailable => _isAvailable;
        public string LastError
        {
            get
            {
                if (_handle == null || _handle.IsInvalid) return "Native handle is invalid.";
                IntPtr ptr = NativeCoreInterop.GetLastError(_handle.DangerousGetHandle());
                return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) ?? string.Empty : string.Empty;
            }
        }

        static NativeCoreService()
        {
            RegisterResolver();
        }

        public NativeCoreService()
        {
            try
            {
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
                _isAvailable = false;
            }
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
                    // Already registered or unsupported
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

        public unsafe void UpdateTick(Tick tick)
        {
            if (!_isAvailable || _handle == null || _handle.IsInvalid)
            {
                throw new InvalidOperationException("Native core library is not available.");
            }

            var data = new TickData
            {
                Timestamp = tick.Time.Ticks,
                Bid = tick.Bid,
                Ask = tick.Ask,
                Spread = tick.Spread,
                Volume = 1.0 // default volume if not specified
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
        }

        public unsafe MarketVector GetMarketVector()
        {
            if (!_isAvailable || _handle == null || _handle.IsInvalid)
            {
                throw new InvalidOperationException("Native core library is not available.");
            }

            int code = NativeCoreInterop.GetMarketVector(_handle.DangerousGetHandle(), out MarketVectorBuffer buffer);
            if (code != 0)
            {
                throw new InvalidOperationException($"Native core get market vector failed with code: {code}. Error: {LastError}");
            }

            // Map 10 primary floating attributes
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
        }

        public unsafe MarketState GetMarketState()
        {
            if (!_isAvailable || _handle == null || _handle.IsInvalid)
            {
                throw new InvalidOperationException("Native core library is not available.");
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
        }

        public void Dispose()
        {
            _handle?.Dispose();
        }
    }
}
