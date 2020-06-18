using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace HomeState.Launcher.Core
{
    /// <summary>
    /// Used for loading the unmanaged Anit Cheat code.
    /// </summary>
    static class NativeLoader
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess,
            IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        // privileges
        public const int PROCESS_CREATE_THREAD = 0x0002;
        public const int PROCESS_QUERY_INFORMATION = 0x0400;
        public const int PROCESS_VM_OPERATION = 0x0008;
        public const int PROCESS_VM_WRITE = 0x0020;
        public const int PROCESS_VM_READ = 0x0010;

        // used for memory allocation
        public const uint MEM_COMMIT = 0x00001000;
        public const uint MEM_RESERVE = 0x00002000;
        public const uint PAGE_READWRITE = 4;

        public static void Inject(Process process, string dllFile)
        {
            var procHandle = OpenProcess(PROCESS_CREATE_THREAD | 
                                                      PROCESS_QUERY_INFORMATION | 
                                                      PROCESS_VM_OPERATION | 
                                                      PROCESS_VM_WRITE | 
                                                      PROCESS_VM_READ, false, process.Id);

            var loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            var allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, 
                (uint)((dllFile.Length + 1) * Marshal.SizeOf(typeof(char))),
                MEM_COMMIT |
                MEM_RESERVE, PAGE_READWRITE);
                
            UIntPtr bytesWritten;
            WriteProcessMemory(procHandle, allocMemAddress, Encoding.Default.GetBytes(dllFile), (uint)((dllFile.Length + 1) * Marshal.SizeOf(typeof(char))), out bytesWritten);
            IntPtr hThread = CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
        }
    }
}
