using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Crysis2RemasteredTrainer
{
    internal sealed class ProcessMemory : IDisposable
    {
        private Process _process;
        private IntPtr _handle;

        internal bool IsAttached
        {
            get { return _process != null && !_process.HasExited && _handle != IntPtr.Zero; }
        }

        internal Process Process
        {
            get { return _process; }
        }

        internal int ProcessId
        {
            get { return _process == null ? 0 : _process.Id; }
        }

        internal bool Attach(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return false;
            }

            string normalized = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? processName.Substring(0, processName.Length - 4)
                : processName;

            Process candidate = Process.GetProcessesByName(normalized).FirstOrDefault();
            if (candidate == null)
            {
                Detach();
                return false;
            }

            if (_process != null && _process.Id == candidate.Id && IsAttached)
            {
                return true;
            }

            Detach();
            IntPtr handle = NativeMethods.OpenProcess(NativeMethods.ProcessAllAccess, false, candidate.Id);
            if (handle == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            _process = candidate;
            _handle = handle;
            return true;
        }

        internal void Detach()
        {
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(_handle);
            }

            _handle = IntPtr.Zero;
            _process = null;
        }

        internal IntPtr GetModuleBase(string moduleName, out int moduleSize)
        {
            moduleSize = 0;
            if (!IsAttached)
            {
                return IntPtr.Zero;
            }

            string effectiveModule = string.IsNullOrWhiteSpace(moduleName)
                ? _process.MainModule.ModuleName
                : moduleName;

            foreach (ProcessModule module in _process.Modules)
            {
                if (string.Equals(module.ModuleName, effectiveModule, StringComparison.OrdinalIgnoreCase))
                {
                    moduleSize = module.ModuleMemorySize;
                    return module.BaseAddress;
                }
            }

            return IntPtr.Zero;
        }

        internal byte[] ReadBytes(IntPtr address, int size)
        {
            byte[] buffer = new byte[size];
            IntPtr bytesRead;
            if (!NativeMethods.ReadProcessMemory(_handle, address, buffer, size, out bytesRead))
            {
                throw new Win32Exception();
            }

            return buffer;
        }

        internal byte[] ReadModule(IntPtr baseAddress, int size)
        {
            return ReadBytes(baseAddress, size);
        }

        internal void WriteBytes(IntPtr address, byte[] data)
        {
            uint oldProtect;
            if (!NativeMethods.VirtualProtectEx(_handle, address, (UIntPtr)data.Length, NativeMethods.PageExecuteReadWrite, out oldProtect))
            {
                throw new Win32Exception();
            }

            try
            {
                IntPtr bytesWritten;
                if (!NativeMethods.WriteProcessMemory(_handle, address, data, data.Length, out bytesWritten))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                uint unused;
                NativeMethods.VirtualProtectEx(_handle, address, (UIntPtr)data.Length, oldProtect, out unused);
            }
        }

        internal IntPtr Allocate(int size)
        {
            IntPtr address = NativeMethods.VirtualAllocEx(
                _handle,
                IntPtr.Zero,
                (UIntPtr)size,
                NativeMethods.MemCommit | NativeMethods.MemReserve,
                NativeMethods.PageExecuteReadWrite);

            if (address == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return address;
        }

        internal IntPtr AllocateNear(IntPtr nearAddress, int size)
        {
            long target = nearAddress.ToInt64();
            const long range = 0x70000000;
            const long step = 0x10000;

            for (long offset = 0; offset <= range; offset += step)
            {
                long low = target - offset;
                if (low > 0x10000)
                {
                    IntPtr lowPtr = TryAllocateAt(new IntPtr(low), size);
                    if (lowPtr != IntPtr.Zero)
                    {
                        return lowPtr;
                    }
                }

                if (offset == 0)
                {
                    continue;
                }

                long high = target + offset;
                if (high > 0 && high < 0x00007FFFFFFF0000)
                {
                    IntPtr highPtr = TryAllocateAt(new IntPtr(high), size);
                    if (highPtr != IntPtr.Zero)
                    {
                        return highPtr;
                    }
                }
            }

            IntPtr fallback = TryAllocateAt(IntPtr.Zero, size);
            if (fallback == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return fallback;
        }

        internal void Free(IntPtr address)
        {
            if (address == IntPtr.Zero)
            {
                return;
            }

            if (!NativeMethods.VirtualFreeEx(_handle, address, UIntPtr.Zero, NativeMethods.MemRelease))
            {
                throw new Win32Exception();
            }
        }

        private IntPtr TryAllocateAt(IntPtr address, int size)
        {
            return NativeMethods.VirtualAllocEx(
                _handle,
                address,
                (UIntPtr)size,
                NativeMethods.MemCommit | NativeMethods.MemReserve,
                NativeMethods.PageExecuteReadWrite);
        }

        public void Dispose()
        {
            Detach();
        }
    }
}
