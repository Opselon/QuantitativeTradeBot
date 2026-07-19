using System.Reflection;
using System.Runtime.InteropServices;

namespace Nexus.Application.Analytics
{
    public class NativeAnalyticsEngine : INativeAnalyticsEngine
    {
        private static readonly object Lock = new();
        private static bool _resolverRegistered = false;
        private readonly bool _isAvailable;

        public bool IsAvailable => _isAvailable;

        static NativeAnalyticsEngine()
        {
            RegisterResolver();
        }

        public NativeAnalyticsEngine()
        {
            try
            {
                // Simple dry run to check if the library can be loaded and executed.
                double[] values = { 1.0 };
                double[] result = { 0.0 };
                int code = calculate_ema(values, 1, 1, result);
                _isAvailable = (code == 0);
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
                    NativeLibrary.SetDllImportResolver(typeof(NativeAnalyticsEngine).Assembly, ResolveDll);
                    _resolverRegistered = true;
                }
                catch (Exception)
                {
                    // Already registered or not supported
                }
            }
        }

        private static IntPtr ResolveDll(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == "nexus_native" || libraryName == "libnexus_native")
            {
                string filename = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "nexus_native.dll" : "libnexus_native.so";

                // 1. Try AppDomain base directory
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fullPath = Path.Combine(baseDir, filename);
                if (File.Exists(fullPath))
                {
                    if (NativeLibrary.TryLoad(fullPath, out IntPtr handle)) return handle;
                }

                // 2. Try walking up parent directories
                string? currentDir = baseDir;
                while (currentDir != null)
                {
                    string candidate = Path.Combine(currentDir, "native", "Nexus.Native", filename);
                    if (File.Exists(candidate))
                    {
                        if (NativeLibrary.TryLoad(candidate, out IntPtr handle)) return handle;
                    }
                    currentDir = Directory.GetParent(currentDir)?.FullName;
                }

                // 3. System paths fallback
                if (NativeLibrary.TryLoad(filename, out IntPtr fallbackHandle)) return fallbackHandle;
            }

            return IntPtr.Zero;
        }

        [DllImport("nexus_native", EntryPoint = "calculate_ema", CallingConvention = CallingConvention.Cdecl)]
        private static extern int calculate_ema(double[] values, int count, int period, double[] outEma);

        public int CalculateEma(double[] values, int count, int period, double[] outEma)
        {
            if (!_isAvailable)
            {
                throw new InvalidOperationException("Native analytics library is not available.");
            }
            return calculate_ema(values, count, period, outEma);
        }
    }
}
