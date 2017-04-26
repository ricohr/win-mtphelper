//
//  Copyright (c) 2017 Ricoh Company, Ltd. All Rights Reserved.
//  See LICENSE for more information.
//
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;


namespace MtpHelper
{
    class Program
    {
        static MtpHelper _helper = new MtpHelper();
        static Semaphore _commandLock = new Semaphore(1, 1);
        static String[] _detectedDevices = new String[0];


        static void UpdateDetectedDevices()
        {
            String[] devs;
            try
            {
                _commandLock.WaitOne();
                devs = ((String[])(_helper.DeviceList())["devices"]);
            }
            finally
            {
                _commandLock.Release();
            }

            foreach (String dev in devs)
            {
                Boolean find = false;
                foreach (String i in _detectedDevices)
                {
                    if (dev.Equals(i))
                    {
                        find = true;
                        break;
                    }
                }
                if (!find) Console.Error.WriteLine("{{\"event\":\"DeviceAdded\",\"deviceId\":\"{0}\"}}", dev);
            }
            foreach (String dev in _detectedDevices)
            {
                Boolean find = false;
                foreach (String i in devs)
                {
                    if (dev.Equals(i))
                    {
                        find = true;
                        break;
                    }
                }
                if (!find) Console.Error.WriteLine("{{\"event\":\"DeviceRemoved\",\"deviceId\":\"{0}\"}}", dev);
            }
            _detectedDevices = devs;
        }


        static void Main(string[] args)
        {
            String exe = System.Reflection.Assembly.GetEntryAssembly().Location;
            String jsonPath = exe.Substring(0, exe.Length - 3) + "json";
            String arg = "";
            bool showProps = false;

            foreach (String k in args)
            {
                if (k.CompareTo("-v") == 0)
                {
                    showProps = true;
                }
                else if (k.StartsWith("-conf:"))
                {
                    jsonPath = k.Substring(6);
                }
                else
                {
                    arg += " " + k; 
                }
            }

            try
            {
                MtpHelper.loadConfigFile(jsonPath);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("MtpHelper.loadConfigFile({0}) failed - {1} {2:X}", jsonPath, e.GetType(), e.HResult);
                throw e;
            }
            if (showProps)
            {
                Console.WriteLine("MtpHelper.exe version {0}", Assembly.GetExecutingAssembly().GetName().Version);
                Console.Write("    Supported device properties:");
                foreach (String name in _helper.GetSupportedPropNames())
                {
                    Console.Write(" {0}", name);
                }
                Console.WriteLine("");
                return;
            }


            TextReader input;
            if (arg.Length == 0)
            {
                input = Console.In;
            }
            else
            {
                input = new StringReader(arg);
            }

            UsbNotificationForm form = new UsbNotificationForm();
            form.enableUsbDeviceNotification((String deviceId) =>
            {
                UpdateDetectedDevices();
            }, (String deviceId) =>
            {
                UpdateDetectedDevices();
            });

            Thread interfaceThread = new Thread((object obj) =>
            {
                try
                {
                    while (true)
                    {
                        String s = input.ReadLine();
                        if (s == null) throw new ObjectDisposedException("EOF");
                        ProcessCommand(s);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Do nothing
                }
                finally
                {
                    form.unregisterUsbDeviceEvent();
                    Application.Exit();
                }
            });

            UpdateDetectedDevices();
            interfaceThread.Start();
            Application.Run(form);
        }


        static void ProcessCommand(String line)
        {
            String[] args = line.Split(null as Char[], StringSplitOptions.RemoveEmptyEntries);
            try
            {
                _commandLock.WaitOne();
                if (args.Length == 0) throw new MtpHelperRuntimeException("No commands");
                switch (args[0])
                {
                    case "deviceList":
                        if (args.Length != 1) throw new MtpHelperRuntimeException(String.Format("Invalid parameter count({0} for 0)", args.Length - 1));
                        Console.WriteLine(MtpHelperUtils.ToJSON(_helper.DeviceList()));
                        break;
                    case "deviceInfo":
                        if (args.Length != 2) throw new MtpHelperRuntimeException(String.Format("Invalid parameter count({0} for 1)", args.Length - 1));
                        Console.WriteLine(MtpHelperUtils.ToJSON(_helper.GetDeviceInfo(args[1])));
                        break;
                    case "desc":
                        if (args.Length != 3) throw new MtpHelperRuntimeException(String.Format("Invalid parameter count({0} for 2)", args.Length - 1));
                        Console.WriteLine(MtpHelperUtils.ToJSON(_helper.GetDevicePropDesc(args[1], args[2])));
                        break;
                    case "get":
                        if (args.Length != 3) throw new MtpHelperRuntimeException(String.Format("Invalid parameter count({0} for 2)", args.Length - 1));
                        Console.WriteLine(MtpHelperUtils.ToJSON(_helper.GetDeviceProp(args[1], args[2])));
                        break;
                    case "set":
                        if (args.Length != 4) throw new MtpHelperRuntimeException(String.Format("Invalid parameter count({0} for 3)", args.Length - 1));
                        Console.WriteLine(MtpHelperUtils.ToJSON(_helper.SetDeviceProp(args[1], args[2], args[3])));
                        break;
                    case "sendConfig":
                        if (args.Length != 3) throw new MtpHelperRuntimeException(String.Format("Invalid parameter count({0} for 2)", args.Length - 1));
                        Console.WriteLine(MtpHelperUtils.ToJSON(_helper.SendConfigObject(args[1], args[2], false)));
                        break;
                    case "getConfig":
                        if (args.Length != 3) throw new MtpHelperRuntimeException(String.Format("Invalid parameter count({0} for 2)", args.Length - 1));
                        Console.WriteLine(MtpHelperUtils.ToJSON(_helper.GetConfigObject(args[1], args[2])));
                        break;
                    case "firmwareUpdate":
                        if (args.Length != 3) throw new MtpHelperRuntimeException(String.Format("Invalid parameter count({0} for 2)", args.Length - 1));
                        Console.WriteLine(MtpHelperUtils.ToJSON(_helper.SendConfigObject(args[1], args[2], true)));
                        break;
                    default:
                        throw new MtpHelperRuntimeException("Invalid command");
                }
            }
            catch (MtpHelperRuntimeException e)
            {
                Console.WriteLine("{{\"status\":\"{0}\"}}", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("{{\"status\":\"{0}\"}}", e.Message);
            }
            finally
            {
                _commandLock.Release();
            }
        }
    }
}
