using System;
using System.Runtime.InteropServices;

namespace Nexus.Infrastructure.Native
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct TickData
    {
        public long Timestamp;
        public fixed byte SymbolId[32];
        public double Bid;
        public double Ask;
        public double Volume;
        public double Spread;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MarketVectorBuffer
    {
        public fixed float Features[64];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MarketStateBuffer
    {
        public fixed byte Symbol[32];
        public long LastUpdatedUtc;
        public double Volatility;
        public double Momentum;
        public double Liquidity;
        public double PriceStructure;
        public double Probability;
        public double Risk;
        public double CurrencyStrength;
        public fixed byte MarketRegime[32];
    }

    public static partial class NativeCoreInterop
    {
        private const string LibName = "nexus_native_core";

        [LibraryImport(LibName, EntryPoint = "nexus_core_create")]
        public static partial IntPtr Create();

        [LibraryImport(LibName, EntryPoint = "nexus_core_destroy")]
        public static partial void Destroy(IntPtr handle);

        [LibraryImport(LibName, EntryPoint = "nexus_core_update_tick")]
        public static partial int UpdateTick(IntPtr handle, in TickData tick);

        [LibraryImport(LibName, EntryPoint = "nexus_core_get_market_vector")]
        public static partial int GetMarketVector(IntPtr handle, out MarketVectorBuffer outVector);

        [LibraryImport(LibName, EntryPoint = "nexus_core_get_market_state")]
        public static partial int GetMarketState(IntPtr handle, out MarketStateBuffer outState);

        [LibraryImport(LibName, EntryPoint = "nexus_core_last_error")]
        public static partial IntPtr GetLastError(IntPtr handle);
    }
}
