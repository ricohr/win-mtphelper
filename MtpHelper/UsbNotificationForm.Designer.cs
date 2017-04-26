//
//  Copyright (c) 2017 Ricoh Company, Ltd. All Rights Reserved.
//  See LICENSE for more information.
//
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;


internal static class UsbNotification
{
    public const int DBT_DEVICEARRIVAL = 0x8000;            // system detected a new device        
    public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;     // device is gone      
    public const int WM_DEVICECHANGE = 0x0219;              // device change event      
    private const int DBT_DEVTYP_DEVICEINTERFACE = 5;       // class of devices, => DEV_BROADCAST_DEVICEINTERFACE
    private static readonly Guid GuidDevinterfaceUSBDevice = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED"); // USB devices
    private static IntPtr notificationHandle;

    /// <summary>
    /// Registers a window to receive notifications when USB devices are plugged or unplugged.
    /// </summary>
    /// <param name="windowHandle">Handle to the window receiving notifications.</param>
    public static void RegisterUsbDeviceNotification(IntPtr windowHandle)
    {
        DevBroadcastDeviceinterface0 dbi0 = new DevBroadcastDeviceinterface0
        {
            DeviceType = DBT_DEVTYP_DEVICEINTERFACE,
            Reserved = 0,
            ClassGuid = GuidDevinterfaceUSBDevice,
            Name = 0
        };

        dbi0.Size = Marshal.SizeOf(dbi0);
        IntPtr buffer = Marshal.AllocHGlobal(dbi0.Size);
        Marshal.StructureToPtr(dbi0, buffer, true);

        notificationHandle = RegisterDeviceNotification(windowHandle, buffer, 0);
    }

    /// <summary>
    /// Unregisters the window for USB device notifications
    /// </summary>
    public static void UnregisterUsbDeviceNotification()
    {
        UnregisterDeviceNotification(notificationHandle);
    }

    private static DevBroadcastHdr GetDevBroadcastHdr(Message m)
    {
        return (DevBroadcastHdr)m.GetLParam(typeof(DevBroadcastHdr));
    }

    public static DevBroadcastDeviceinterface GetDevBroadcastDeviceinterface(Message m)
    {
        DevBroadcastHdr hdr = GetDevBroadcastHdr(m);
        if (hdr.DeviceType == DBT_DEVTYP_DEVICEINTERFACE)
        {
            DevBroadcastDeviceinterface0 dbi0 = (DevBroadcastDeviceinterface0)m.GetLParam(typeof(DevBroadcastDeviceinterface0));
            DevBroadcastDeviceinterface dbi = new DevBroadcastDeviceinterface() { DeviceType = dbi0.DeviceType, ClassGuid = dbi0.ClassGuid };
            Byte[] bytes = new Byte[hdr.Size];
            Marshal.Copy((IntPtr)m.LParam, bytes, 0, hdr.Size);
            int offset = Marshal.SizeOf(dbi0) - 2;
            dbi.Name = System.Text.Encoding.Unicode.GetString(bytes, offset, bytes.Length - offset);
            return dbi;
        }
        return new DevBroadcastDeviceinterface();
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

    [DllImport("user32.dll")]
    private static extern bool UnregisterDeviceNotification(IntPtr handle);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevBroadcastDeviceinterface0
    {
        internal int Size;
        internal int DeviceType;
        internal int Reserved;
        internal Guid ClassGuid;
        internal short Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DevBroadcastHdr
    {
        internal int Size;
        internal int DeviceType;
        internal int Reserved;
    }

    public struct DevBroadcastDeviceinterface
    {
        internal int DeviceType;
        internal Guid ClassGuid;
        internal string Name;
    }
}



namespace MtpHelper
{
    partial class UsbNotificationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        private Action<String> onUsbDeviceAdded { get; set; }
        private Action<String> onUsbDeviceRemoved { get; set; }


        public void enableUsbDeviceNotification(Action<String> onAdded, Action<String> onRemoved)
        {
            UsbNotification.RegisterUsbDeviceNotification(this.Handle);
            onUsbDeviceAdded = onAdded;
            onUsbDeviceRemoved = onRemoved;
        }


        public void unregisterUsbDeviceEvent()
        {
            UsbNotification.UnregisterUsbDeviceNotification();
        }


        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == UsbNotification.WM_DEVICECHANGE)
            {
                UsbNotification.DevBroadcastDeviceinterface dbi;
                switch ((int)m.WParam)
                {
                    case UsbNotification.DBT_DEVICEARRIVAL:
                        dbi = UsbNotification.GetDevBroadcastDeviceinterface(m);
                        onUsbDeviceAdded(dbi.Name);
                        break;
                    case UsbNotification.DBT_DEVICEREMOVECOMPLETE:
                        dbi = UsbNotification.GetDevBroadcastDeviceinterface(m);
                        onUsbDeviceRemoved(dbi.Name);
                        break;
                }
            }
        }


        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // UsbNotificationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 74);
            this.ControlBox = false;
            this.Name = "UsbNotificationForm";
            this.ShowInTaskbar = false;
            this.Text = "UsbNotificationForm";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.ResumeLayout(false);

        }

        #endregion
    }
}