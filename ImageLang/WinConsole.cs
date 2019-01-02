using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageLang
{
    static class WinConsole
    {
        static public void Initialize()
        {
            if (AllocConsole() != 0)
            {
                InitializeOutStream();
                InitializeInStream();
            }
        }

        static void InitializeOutStream()
        {
            var fs = CreateFileStream("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, FileAccess.Write);

            if (fs != null)
            {
                var writer = new StreamWriter(fs) { AutoFlush = true };
                Console.SetOut(writer);
                Console.SetError(writer);
            }
        }

        static void InitializeInStream()
        {
            var fs = CreateFileStream("CONIN$", GENERIC_READ, FILE_SHARE_READ, FileAccess.Read);

            if (fs != null)
                Console.SetIn(new StreamReader(fs));
        }

        static FileStream CreateFileStream(string name, uint win32DesiredAccess, uint win32ShareMode, FileAccess dotNetFileAccess)
        {
            var file = new SafeFileHandle(CreateFileW(name, win32DesiredAccess, win32ShareMode, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero), true);

            if (file.IsInvalid == false)
                return new FileStream(file, dotNetFileAccess);

            return null;
        }

        #region Win API Functions and Constants
        [DllImport("kernel32.dll",
            EntryPoint = "AllocConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int AllocConsole();

        [DllImport("kernel32.dll",
            EntryPoint = "AttachConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern UInt32 AttachConsole(UInt32 dwProcessId);

        [DllImport("kernel32.dll",
            EntryPoint = "CreateFileW",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateFileW(
              string lpFileName,
              UInt32 dwDesiredAccess,
              UInt32 dwShareMode,
              IntPtr lpSecurityAttributes,
              UInt32 dwCreationDisposition,
              UInt32 dwFlagsAndAttributes,
              IntPtr hTemplateFile
            );

        const UInt32 GENERIC_WRITE = 0x40000000;
        const UInt32 GENERIC_READ = 0x80000000;
        const UInt32 FILE_SHARE_READ = 0x00000001;
        const UInt32 FILE_SHARE_WRITE = 0x00000002;
        const UInt32 OPEN_EXISTING = 0x00000003;
        const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
        const UInt32 ERROR_ACCESS_DENIED = 5;

        const UInt32 ATTACH_PARRENT = 0xFFFFFFFF;
        #endregion
    }
}
