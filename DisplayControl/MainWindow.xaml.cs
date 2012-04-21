using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace DisplayControl
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public class NativeMethods
        {
            [DllImport("user32.dll", EntryPoint = "MonitorFromWindow", SetLastError = true)]
            public static extern IntPtr MonitorFromWindow(
                [In] IntPtr hwnd, uint dwFlags);

            [DllImport("user32.dll", EntryPoint = "MonitorFromPoint", SetLastError = true)]
            public static extern IntPtr MonitorFromPoint(
                [In] NativeStructures.tagPOINT pt, uint dwFlags);

            [DllImport("dxva2.dll", EntryPoint = "GetNumberOfPhysicalMonitorsFromHMONITOR", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(
                IntPtr hMonitor, ref uint pdwNumberOfPhysicalMonitors);

            [DllImport("dxva2.dll", EntryPoint = "GetPhysicalMonitorsFromHMONITOR", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetPhysicalMonitorsFromHMONITOR(
                IntPtr hMonitor,
                uint dwPhysicalMonitorArraySize,
                [Out] NativeStructures.PHYSICAL_MONITOR[] pPhysicalMonitorArray);

            [DllImport("dxva2.dll", EntryPoint = "DestroyPhysicalMonitors", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DestroyPhysicalMonitors(
                uint dwPhysicalMonitorArraySize, [Out] NativeStructures.PHYSICAL_MONITOR[] pPhysicalMonitorArray);

            [DllImport("gdi32.dll", EntryPoint = "DDCCIGetCapabilitiesStringLength", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DDCCIGetCapabilitiesStringLength(
                [In] IntPtr hMonitor, ref uint pdwLength);

            [DllImport("gdi32.dll", EntryPoint = "DDCCIGetCapabilitiesString", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DDCCIGetCapabilitiesString(
                [In] IntPtr hMonitor, StringBuilder pszString, uint dwLength);


            [DllImport("gdi32.dll", EntryPoint = "DDCCIGetVCPFeature", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DDCCIGetVCPFeature(
                [In] IntPtr hMonitor, [In] uint dwVCPCode, uint pvct, ref uint pdwCurrentValue, ref uint pdwMaximumValue);

            [DllImport("dxva2.dll", EntryPoint = "GetVCPFeatureAndVCPFeatureReply", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetVCPFeatureAndVCPFeatureReply(
                [In] IntPtr hMonitor, [In] uint dwVCPCode, uint pvct, ref uint pdwCurrentValue, ref uint pdwMaximumValue);


            [DllImport("dxva2.dll", EntryPoint = "SetVCPFeature", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetVCPFeature(
                [In] IntPtr hMonitor, uint dwVCPCode, uint dwNewValue);
        }

        public class NativeConstants
        {
            public const int MONITOR_DEFAULTTOPRIMARY = 1;

            public const int MONITOR_DEFAULTTONEAREST = 2;

            public const int MONITOR_DEFAULTTONULL = 0;
        }

        public class NativeStructures
        {
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct PHYSICAL_MONITOR
            {
                public IntPtr hPhysicalMonitor;

                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
                public string szPhysicalMonitorDescription;
            }

            public struct tagPOINT {
              public int x;
              public int y;
            };
        }

        private NativeStructures.PHYSICAL_MONITOR[] getMonitors()
        {
            WindowInteropHelper helper = new WindowInteropHelper(this);

            // Get the right monitor
            NativeStructures.tagPOINT right = new NativeStructures.tagPOINT();
            right.x = 2000;
            right.y = 0;
            //IntPtr hMonitor = NativeMethods.MonitorFromWindow(helper.Handle, NativeConstants.MONITOR_DEFAULTTONULL);
            IntPtr hMonitor = NativeMethods.MonitorFromPoint(right, NativeConstants.MONITOR_DEFAULTTONULL);
            int lastWin32Error = Marshal.GetLastWin32Error();

            uint pdwNumberOfPhysicalMonitors = 0u;
            bool numberOfPhysicalMonitorsFromHmonitor = NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(
                hMonitor, ref pdwNumberOfPhysicalMonitors);
            lastWin32Error = Marshal.GetLastWin32Error();

            NativeStructures.PHYSICAL_MONITOR[] pPhysicalMonitorArray =
                new NativeStructures.PHYSICAL_MONITOR[pdwNumberOfPhysicalMonitors];
            bool physicalMonitorsFromHmonitor = NativeMethods.GetPhysicalMonitorsFromHMONITOR(
                hMonitor, pdwNumberOfPhysicalMonitors, pPhysicalMonitorArray);
            lastWin32Error = Marshal.GetLastWin32Error();

            return pPhysicalMonitorArray;
        }

        private bool cleanupMonitors(uint pdwNumberOfPhysicalMonitors, NativeStructures.PHYSICAL_MONITOR[] pPhysicalMonitorArray)
        {
            return NativeMethods.DestroyPhysicalMonitors(pdwNumberOfPhysicalMonitors, pPhysicalMonitorArray);
        }

        private void Detect_Click(object sender, RoutedEventArgs e)
        {
            var monitors = getMonitors();
            var acer = monitors[0].hPhysicalMonitor;

            uint capabilitiesStringLength = 0u;
            var capabilitiesStringLengthSuccess = NativeMethods.DDCCIGetCapabilitiesStringLength(acer, ref capabilitiesStringLength);

            var capabilitiesString = new StringBuilder((int)capabilitiesStringLength + 1);
            var capabilitiesStringSuccess = NativeMethods.DDCCIGetCapabilitiesString(acer, capabilitiesString, (uint)capabilitiesString.Capacity);

            this.Description.Content = monitors[0].szPhysicalMonitorDescription;
            this.Capabilities.Text = capabilitiesString.ToString();

            cleanupMonitors((uint)monitors.Length, monitors);
        }

        private void SetInput_Click(object sender, RoutedEventArgs e)
        {

            var monitors = getMonitors();
            var acer = monitors[0].hPhysicalMonitor;

            // 0x60 = Set Input
            var success = NativeMethods.SetVCPFeature(acer, 0x60, uint.Parse(this.Input.Text));

            cleanupMonitors((uint)monitors.Length, monitors);
        }
    }
}

