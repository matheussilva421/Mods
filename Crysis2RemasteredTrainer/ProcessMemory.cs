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

        public void Dispose()
        {
            Detach();
        }
    }
}
