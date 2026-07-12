using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Nexus.Infrastructure.Native
{
    /// <summary>
    /// SafeHandle implementation wrapper for managing the C++ core engine instance pointer.
    /// Prevents handle recycling vulnerabilities and guarantees C++ destructor invocation.
    /// </summary>
    public class NativeCoreSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private NativeCoreSafeHandle() : base(true)
        {
        }

        public NativeCoreSafeHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            if (handle != IntPtr.Zero)
            {
                NativeCoreInterop.Destroy(handle);
                handle = IntPtr.Zero;
                return true;
            }
            return false;
        }
    }
}
