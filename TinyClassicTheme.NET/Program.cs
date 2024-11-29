using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace TinyClassicTheme
{
    class TinyClassicTheme
    {
        static void Main(string[] args)
        {
            if (args.Any(x => x.Contains("help")))
            {
                Console.WriteLine("TinyClassicTheme - Enable or disable classic theme on Windows 10.");
                Console.WriteLine("Usage: TinyClassicTheme [disable]");
                Console.WriteLine("Options:");
                Console.WriteLine("  disable - Disable classic theme.");
                Console.WriteLine("  help - Show this help message.");
                return;
            }
            var disable = args.Any(x => x.Contains("disable"));

            var themeSection = "\\Sessions\\" + Process.GetCurrentProcess().SessionId + "\\Windows\\ThemeSection";
            //Console.WriteLine("Theme section: " + themeSection);
            var sectionHandle = IntPtr.Zero;

            var objectAttributes = InitializeObjectAttributes(themeSection);

            int ntStatus = NtOpenSection(ref sectionHandle, 0x40000, ref objectAttributes);
            if (ntStatus != 0)
            {
                Console.WriteLine("NtOpenSection failed with status: 0x" + ntStatus.ToString("X"));
                if (ntStatus == -1073741790) // STATUS_ACCESS_DENIED, 0xC0000022
                {
                    Console.WriteLine("Please run this program as administrator.");
                }
                return;
            }

            var securityDescriptor = IntPtr.Zero;
            var securityDescriptorSize = 0u;
            var sddl = disable ? "O:BAG:SYD:(A;;CCLCRC;;;IU)(A;;CCDCLCSWRPSDRCWDWO;;;SY)" : "O:BAG:SYD:(A;;RC;;;IU)(A;;DCSWRPSDRCWDWO;;;SY)";

            bool success = ConvertStringSecurityDescriptorToSecurityDescriptor(sddl, 1, ref securityDescriptor, ref securityDescriptorSize);
            if (!success)
            {
                Console.WriteLine("ConvertStringSecurityDescriptorToSecurityDescriptor failed with error: " + Marshal.GetLastWin32Error());
                return;
            }

            success = SetKernelObjectSecurity(sectionHandle, 4, securityDescriptor);
            if (!success)
            {
                Console.WriteLine("SetKernelObjectSecurity failed with error: " + Marshal.GetLastWin32Error());
                return;
            }

            if (disable)
            {
                Console.WriteLine("Classic theme disabled successfully.");
            }
            else
            {
                Console.WriteLine("Classic theme enabled successfully.");
                Console.WriteLine("Run 'TinyClassicTheme.exe disable' to disable the classic theme.");
            }
        }

        // https://github.com/ricardojoserf/TrickDump/blob/f0422998e4bd130b0e42c64abbe1419190a3c8f1/Lock/NT.cs#L78
        public static OBJECT_ATTRIBUTES InitializeObjectAttributes(string dll_name)
        {
            OBJECT_ATTRIBUTES objectAttributes = new OBJECT_ATTRIBUTES();
            objectAttributes.RootDirectory = IntPtr.Zero;
            UNICODE_STRING objectName = new UNICODE_STRING();
            objectName.Buffer = dll_name;
            objectName.Length = (ushort)(dll_name.Length * 2);
            objectName.MaximumLength = (ushort)(dll_name.Length * 2 + 2);
            objectAttributes.ObjectName = Marshal.AllocHGlobal(Marshal.SizeOf(objectName));
            Marshal.StructureToPtr(objectName, objectAttributes.ObjectName, false);
            objectAttributes.SecurityDescriptor = IntPtr.Zero;
            objectAttributes.SecurityQualityOfService = IntPtr.Zero;
            objectAttributes.Length = Convert.ToUInt32(Marshal.SizeOf(objectAttributes));
            return objectAttributes;
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtOpenSection(ref IntPtr SectionHandle, int DesiredAccess, ref OBJECT_ATTRIBUTES ObjectAttributes);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string StringSecurityDescriptor, uint StringSDRevision, ref IntPtr SecurityDescriptor, ref uint SecurityDescriptorSize);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool SetKernelObjectSecurity(IntPtr Handle, int SecurityInformation, IntPtr SecurityDescriptor);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        [MarshalAs(UnmanagedType.LPWStr)] public string Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OBJECT_ATTRIBUTES
    {
        public uint Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }
}