using System;
using System.Runtime.InteropServices;

namespace Crysis3RemasteredTrainer
{
    internal static class NativeMethods
    {
        internal const uint ProcessAllAccess = 0x001F0FFF;
        internal const uint PageExecuteReadWrite = 0x40;
        internal const uint MemCommit = 0x1000;
        internal const uint MemReserve = 0x2000;
        internal const uint MemRelease = 0x8000;
        internal const int WmHotKey = 0x0312;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(
            IntPtr processHandle,
            IntPtr baseAddress,
            [Out] byte[] buffer,
            int size,
            out IntPtr bytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteProcessMemory(
            IntPtr processHandle,
            IntPtr baseAddress,
            byte[] buffer,
            int size,
            out IntPtr bytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool VirtualProtectEx(
            IntPtr processHandle,
            IntPtr address,
            UIntPtr size,
            uint newProtect,
            out uint oldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr VirtualAllocEx(
            IntPtr processHandle,
            IntPtr address,
            UIntPtr size,
            uint allocationType,
            uint protect);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool VirtualFreeEx(
            IntPtr processHandle,
            IntPtr address,
            UIntPtr size,
            uint freeType);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
