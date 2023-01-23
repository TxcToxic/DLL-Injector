using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class Injector
{
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = false)]
    static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttribute, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

    public static void InjectDLL(int processId, string dllPath)
    {
        IntPtr hProcess = OpenProcess(0x1F0FFF, false, processId);
        if (hProcess == IntPtr.Zero)
        {
            Console.WriteLine("Could not open Process!");
            return;
        }

        IntPtr hKernel = GetModuleHandle("kernel32.dll");
        if (hKernel == IntPtr.Zero)
        {
            Console.WriteLine("Could not get handle for kernel32 (DLL)!");
            return;
        }

        IntPtr loadLibraryAddr = GetProcAddress(hKernel, "LoadLibraryA");
        if (loadLibraryAddr == IntPtr.Zero)
        {
            Console.WriteLine("Could not get address for LoadLibraryA!");
            return;
        }

        IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))), 0x1000, 0x40);
        if (allocMemAddress == IntPtr.Zero)
        {
            Console.WriteLine("Could not allocate memory in the remote process!");
            return;
        }

        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(dllPath);
        UIntPtr bytesWritten;
        if (!WriteProcessMemory(hProcess, allocMemAddress, bytes, (uint)bytes.Length, out bytesWritten))
        {
            Console.WriteLine("Could not write to memory in the remote process!");
            return;
        }

        IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
        if (hThread == IntPtr.Zero)
        {
            Console.WriteLine("Could not create remote thread!");
            return;
        }

        Console.WriteLine("DLL injected successfully!");
    }
}

namespace DLL_Injector
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2) 
            {
                Console.WriteLine($"Usage: {System.AppDomain.CurrentDomain.FriendlyName} <process ID> <DLL Path>");
                return;
            }

            int processId = int.Parse(args[0]);
            string dllPath = args[1];

            Injector.InjectDLL(processId, dllPath);
        }
    }
}
