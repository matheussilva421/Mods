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

            Process[] candidates = Process.GetProcessesByName(normalized);
            Process candidate = ChooseBestCandidate(candidates);
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

        internal string GetProcessPath()
        {
            if (_process == null)
            {
                return string.Empty;
            }

            try
            {
                return _process.MainModule == null ? string.Empty : _process.MainModule.FileName;
            }
            catch
            {
                return string.Empty;
            }
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

        internal IntPtr GetModuleBase(string moduleName, out int moduleSize, out string resolvedModuleName)
        {
            moduleSize = 0;
            resolvedModuleName = string.Empty;
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
                    resolvedModuleName = module.ModuleName;
                    return module.BaseAddress;
                }
            }

            try
            {
                if (_process.MainModule != null)
                {
                    moduleSize = _process.MainModule.ModuleMemorySize;
                    resolvedModuleName = _process.MainModule.ModuleName;
                    return _process.MainModule.BaseAddress;
                }
            }
            catch
            {
            }

            return IntPtr.Zero;
        }

        internal byte[] ReadBytes(IntPtr address, int size)
        {
            byte[] buffer;
            int bytesRead;
            string error;
            if (!TryReadBytes(address, size, out buffer, out bytesRead, out error))
            {
                throw new Win32Exception(error);
            }

            if (bytesRead != size)
            {
                throw new InvalidOperationException("Partial memory read at 0x" + address.ToInt64().ToString("X") + ". Expected " + size + " bytes, got " + bytesRead + ".");
            }

            return buffer;
        }

        internal byte[] ReadModule(IntPtr baseAddress, int size)
        {
            byte[] buffer = new byte[size];
            const int chunkSize = 0x1000;
            int readableChunks = 0;

            for (int offset = 0; offset < size; offset += chunkSize)
            {
                int bytesToRead = Math.Min(chunkSize, size - offset);
                byte[] chunk;
                int chunkBytesRead;
                string error;
                if (!TryReadBytes(IntPtr.Add(baseAddress, offset), bytesToRead, out chunk, out chunkBytesRead, out error))
                {
                    continue;
                }

                if (chunkBytesRead <= 0)
                {
                    continue;
                }

                Buffer.BlockCopy(chunk, 0, buffer, offset, chunkBytesRead);
                readableChunks++;
            }

            if (readableChunks == 0)
            {
                throw new InvalidOperationException("Could not read any memory from target module.");
            }

            return buffer;
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

        private bool TryReadBytes(IntPtr address, int size, out byte[] buffer, out int bytesRead, out string error)
        {
            buffer = new byte[size];
            bytesRead = 0;
            error = string.Empty;

            IntPtr nativeBytesRead;
            if (!NativeMethods.ReadProcessMemory(_handle, address, buffer, size, out nativeBytesRead))
            {
                error = new Win32Exception().Message;
                return false;
            }

            bytesRead = nativeBytesRead.ToInt32();
            return true;
        }

        private static Process ChooseBestCandidate(Process[] candidates)
        {
            if (candidates == null || candidates.Length == 0)
            {
                return null;
            }

            return candidates
                .OrderByDescending(candidate => SafeGet(() => candidate.MainWindowHandle != IntPtr.Zero, false))
                .ThenByDescending(candidate => SafeGet(() => candidate.WorkingSet64, 0L))
                .ThenByDescending(candidate => SafeGet(() => candidate.StartTime.Ticks, 0L))
                .FirstOrDefault();
        }

        private static T SafeGet<T>(Func<T> getter, T fallback)
        {
            try
            {
                return getter();
            }
            catch
            {
                return fallback;
            }
        }

        public void Dispose()
        {
            Detach();
        }
    }
}
