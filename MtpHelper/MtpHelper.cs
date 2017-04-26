//
//  Copyright (c) 2017 Ricoh Company, Ltd. All Rights Reserved.
//  See LICENSE for more information.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using WpdMtpLib;
using WpdMtpLib.DeviceProperty;


namespace MtpHelper
{
    public class MtpHelper
    {
        private const String USBDEVICEPATH_HEADER = "\\\\?\\";
        private MtpCommand command { get; set; }

        /// <summary>
        /// GetConfigObject, SendConfigObject のoperation code
        /// </summary>
        private enum ConfigObject
        {
            Send = 0x99A3,
            Get
        };

        /// <summary>
        /// GetConfigObject, SendConfigObject の configObject type
        /// </summary>
        private enum ConfigObjectType
        {
            Config = 1,
            Firmware,
        };


        /// <summary>
        /// target friendlyName
        /// </summary>
        static List<String> _friendlyNames = new List<String>() { "RICOH R Development Kit" };

        /// <summary>
        /// supported deviceProperties
        /// </summary>
        static Dictionary<String, UInt16[]> _properties = new Dictionary<String, UInt16[]>() {
            {"WhiteBalance", new UInt16[2]{(UInt16)MtpDevicePropCode.WhiteBalance, (UInt16)DataType.UINT16}},
            {"ExposureBiasCompensation", new UInt16[2]{(UInt16)MtpDevicePropCode.ExposureBiasCompensation, (UInt16)DataType.INT16}}
        };


        public static void loadConfigFile(String jsonPath)
        {
            var cvu16 = new System.ComponentModel.UInt16Converter();
            Func<String, UInt16> u16cv = (String s) => (UInt16)cvu16.ConvertFromString(s);

            try
            {
                FileStream fs = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var jsonReader = JsonReaderWriterFactory.CreateJsonReader(fs, new System.Xml.XmlDictionaryReaderQuotas());
                var root = XElement.Load(jsonReader);

                _friendlyNames = new List<String>();
                foreach (var item in root.XPathSelectElements("//friendlyNames/*"))
                {
                    _friendlyNames.Add(item.Value);
                }
                foreach (var item in root.XPathSelectElements("//properties/*"))
                {
                    if (!item.HasElements) continue;
                    var e_code = item.FirstNode;    //=> "0x500F"
                    if (e_code == null) continue;
                    var e_type = e_code.NextNode;   //=> "UINT16"
                    if (e_type == null) continue;
                    String name = item.Name.ToString();
                    UInt16 code = u16cv(((XElement)e_code).Value);
                    UInt16 type = (UInt16)MtpHelperUtils.TypeToDataType(((XElement)e_type).Value);
                    _properties[name] = new UInt16[2] { code, type };
                }
                fs.Close();
            }
            catch (System.IO.FileNotFoundException e)
            {
                String exe = System.Reflection.Assembly.GetEntryAssembly().Location;
                Console.Error.WriteLine("{0}: {1}", exe, e.Message);
            }
        }


        static MtpDevicePropCode KeyToPropCode(String propName, ref DataType type)
        {
            type = DataType.UNDEF;
            try
            {
                MtpDevicePropCode code = (MtpDevicePropCode)_properties[propName][0];
                type = (DataType)_properties[propName][1];
                return code;
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new MtpHelperRuntimeException(String.Format("Invalid property key({0})", propName));
            }
        }


        static void MtpEventListener(ushort eventCode, object eventValue)
        {
            switch (eventCode)
            {
                case MtpEvent.ObjectAdded:
                    Console.Error.WriteLine("ObjectAdded. ObjectID: {0:X}", (uint)eventValue);
                    break;
                case MtpEvent.DevicePropChanged:
                    Console.Error.WriteLine("DevicePropChanged.");
                    break;
                case MtpEvent.DeviceInfoChanged:
                    Console.Error.WriteLine("DeviceInfoChanged.");
                    break;
                case MtpEvent.StoreFull:
                    Console.Error.WriteLine("StoreFull.");
                    break;
                case MtpEvent.StorageInfoChanged:
                    Console.Error.WriteLine("StorageInfoChanged.");
                    break;
                case MtpEvent.CaptureComplete:
                    Console.Error.WriteLine("CaptureComplete.");
                    break;
                default:
                    Console.Error.WriteLine("Unknown Event({0:X4})", eventCode);
                    break;
            }
        }



        public MtpHelper()
        {
            command = new MtpCommand();
            command.MtpEvent += MtpEventListener;
        }


        void Connect(String deviceId, Action block)
        {
            bool find = false;
            foreach (String did in deviceList())
            {
                if (deviceId.Equals(did))
                {
                    find = true;
                    break;
                }
            }
            if (!find)
            {
                throw new MtpHelperRuntimeException("Device not found");
            }

            deviceId = USBDEVICEPATH_HEADER + deviceId;
            try
            {
                command.Open(deviceId);
            }
            catch (Exception e) // System.IO.FileNotFoundException, System.ArgumentException
            {
                Console.Error.WriteLine("# onConnect: {0}", e.GetType().ToString());
                throw new MtpHelperRuntimeException("Device not found");
            }

            try
            {
                block();
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                // System.Runtime.InteropServices.COMException (0x802A0002)
                Console.Error.WriteLine("# onBlock: {0}", e.GetType().ToString());
                throw new MtpHelperRuntimeException("Device not found");
            }
            finally {
                try
                {
                    if (command != null)
                    {
                        command.Close();
                    }
                    else
                    {
                        command = new MtpCommand();
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("# onClose: {0}", e.GetType().ToString());
                    command = new MtpCommand();
                }
            }
        }


        /// <summary>
        /// "deviceList"
        /// </summary>
        public Hashtable DeviceList()
        {
            Hashtable ret = new Hashtable();
            ret["status"] = "No Devices";
            ret["devices"] = deviceList();
            if (((String[])ret["devices"]).Length > 0)
            {
                ret["status"] = "OK";
            }

            return ret;
        }


        String[] deviceList()
        {
            // 接続されているデバイスIDを取得する
            String[] deviceIds = command.GetDeviceIds();
            if (deviceIds.Length == 0)
            {
                return new String[0];
            }

            // 対象デバイスを取得する
            List<String> dl = new List<String>();
            foreach (String deviceId in deviceIds)
            {
                if (_friendlyNames.Contains(command.GetDeviceFriendlyName(deviceId)))
                {
                    dl.Add(deviceId.Substring(USBDEVICEPATH_HEADER.Length));
                }
            }
            return dl.ToArray();
        }


        /// <summary>
        /// "deviceInfo DEVICE-ID"
        /// </summary>
        public Hashtable GetDeviceInfo(String deviceId)
        {
            Hashtable ret = new Hashtable();
            ret["status"] = "FAILED";

            Connect(deviceId, () =>
            {
                MtpResponse res = command.Execute(MtpOperationCode.GetDeviceInfo, null, null);
                if (res.ResponseCode == WpdMtpLib.MtpResponseCode.OK)
                {
                    DeviceInfo deviceInfo = new DeviceInfo(res.Data);
                    ret["VenderExtensionId"] = deviceInfo.MtpVenderExtensionID;
                    ret["VenderExtensionVersion"] = deviceInfo.MtpVersion;
                    ret["DeviceVersion"] = deviceInfo.DeviceVersion;
                    ret["SerialNumber"] = deviceInfo.SerialNumber;
                    ret["FunctionalMode"] = deviceInfo.FunctionalMode;
                    ret["Manufacturer"] = deviceInfo.Manufacturer;
                    ret["Model"] = deviceInfo.Model;
                    ret["StandardVersion"] = deviceInfo.StandardVersion;
                    ret["status"] = "OK";
                }
                else
                {
                    ret["status"] = String.Format("FAILED({0:X4})", (UInt16)res.ResponseCode);
                }
            });
            return ret;
        }


        /// <summary>
        /// "desc DEVICE-ID propName"
        /// </summary>
        public Hashtable GetDevicePropDesc(String deviceId, String propName)
        {
            Hashtable ret = new Hashtable();
            ret["status"] = "FAILED";

            DataType type = DataType.UNDEF;
            MtpDevicePropCode code = KeyToPropCode(propName, ref type);

            Connect(deviceId, () =>
            {
                MtpResponse res = command.Execute(MtpOperationCode.GetDevicePropDesc, new uint[1] { (uint)code }, null);
                if (res.ResponseCode == WpdMtpLib.MtpResponseCode.OK)
                {
                    DevicePropDesc dpd = new DevicePropDesc(res.Data);
                    ret["current"] = dpd.CurrentValue;
                    ret["factory_default_value"] = dpd.FactoryDefaultValue;
                    ret["get_set"] = dpd.GetSet;
                    if (dpd.FormFlag == 0x01)
                    {
                        // 範囲
                        ret["min"] = dpd.Form[0];
                        ret["max"] = dpd.Form[1];
                        ret["step"] = dpd.Form[2];
                    }
                    else if (dpd.FormFlag == 0x02)
                    {
                        // 配列
                        ret["values"] = dpd.Form;
                    }
                    else
                    {
                        ret["values"] = null;
                    }
                    ret["status"] = "OK";
                }
                else
                {
                    ret["status"] = String.Format("FAILED({0:X4})", (UInt16)res.ResponseCode);
                }
            });
            return ret;
        }


        /// <summary>
        /// "get DEVICE-ID propName"
        /// </summary>
        public Hashtable GetDeviceProp(String deviceId, String propName)
        {
            Hashtable ret = new Hashtable();
            ret["status"] = "FAILED";

            DataType type = DataType.UNDEF;
            MtpDevicePropCode code = KeyToPropCode(propName, ref type);

            Connect(deviceId, () =>
            {
                MtpResponse res = command.Execute(MtpOperationCode.GetDevicePropValue, new uint[1] { (uint)code }, null);
                if (res.ResponseCode == WpdMtpLib.MtpResponseCode.OK)
                {
                    ret["current"] = MtpHelperUtils.BytesToValue(res.Data, type);
                    ret["status"] = "OK";
                }
                else
                {
                    ret["status"] = String.Format("FAILED({0:X4})", (UInt16)res.ResponseCode);
                }
            });
            return ret;
        }


        /// <summary>
        /// "set DEVICE-ID propName value"
        /// </summary>
        public Hashtable SetDeviceProp(String deviceId, String propName, String propValue)
        {
            Hashtable ret = new Hashtable();
            ret["status"] = "FAILED";

            DataType type = DataType.UNDEF;
            MtpDevicePropCode code = KeyToPropCode(propName, ref type);
            byte[] val = MtpHelperUtils.StringToBytes(propValue, type);

            Connect(deviceId, () =>
            {
                MtpResponse res = command.Execute(MtpOperationCode.SetDevicePropValue, new uint[1] { (uint)code }, val);
                if (res.ResponseCode == WpdMtpLib.MtpResponseCode.OK)
                {
                    ret["status"] = "OK";
                }
                else
                {
                    ret["status"] = String.Format("FAILED({0:X4})", (UInt16)res.ResponseCode);
                }
            });
            return ret;
        }


        /// <summary>
        /// "sendConfig DEVICE-ID fileName"
        /// </summary>
        static bool checkFileName(String fileName, ref UInt32 version)
        {
            fileName = Path.GetFileName(fileName);
            Regex re = new Regex(@"\A..\d_v(\d{3})\.frm\z", RegexOptions.IgnoreCase);
            Match m = re.Match(fileName);
            if (m.Groups.Count == 2 && UInt32.TryParse(m.Groups[1].ToString(), out version)) {
                return true;
            }
            return false;
        }

        public Hashtable SendConfigObject(String deviceId, String fileName, bool isFirmware)
        {
            Hashtable ret = new Hashtable();
            ret["status"] = "FAILED";
            ConfigObjectType configType = isFirmware ? ConfigObjectType.Firmware : ConfigObjectType.Config;
            UInt32 version = 0;

            if (isFirmware && !checkFileName(fileName, ref version))
            {
                ret["status"] = "Invalid file name";
                return ret;
            }

            FileStream fs = new FileStream(fileName, FileMode.Open);
            byte[] data = new byte[fs.Length];
            fs.Read(data, 0, (int)fs.Length);
            uint dataLength = (uint)fs.Length;
            fs.Close();

            if (dataLength == 0)
            {
                ret["status"] = "Invalid file content";
                return ret;
            }

            Connect(deviceId, () =>
            {
                MtpResponse res = command.Execute((UInt16)ConfigObject.Send, DataPhase.DataWritePhase, new uint[3] { (uint)configType, dataLength, version }, data);
                if (res.ResponseCode == WpdMtpLib.MtpResponseCode.OK)
                {
                    ret["status"] = "OK";
                }
                else
                {
                    ret["status"] = String.Format("FAILED({0:X4})", (UInt16)res.ResponseCode);
                }
            });
            return ret;
        }


        /// <summary>
        /// "getConfig DEVICE-ID fileName"
        /// </summary>
        public Hashtable GetConfigObject(String deviceId, String fileName)
        {
            Hashtable ret = new Hashtable();
            ret["status"] = "FAILED";

            Connect(deviceId, () =>
            {
                MtpResponse res = command.Execute((UInt16)ConfigObject.Get, DataPhase.DataReadPhase, new uint[1] { (uint)ConfigObjectType.Config});
                if (res.ResponseCode == WpdMtpLib.MtpResponseCode.OK)
                {
                    FileStream fs = new FileStream(fileName, FileMode.Create);
                    fs.Write(res.Data, 0, res.Data.Length);
                    fs.Close();
                    ret["status"] = "OK";
                }
                else
                {
                    ret["status"] = String.Format("FAILED({0:X4})", (UInt16)res.ResponseCode);
                }
            });
            return ret;
        }


        /// <summary>
        /// サポートする propName 一覧
        /// </summary>
        public Dictionary<String, UInt16[]>.KeyCollection GetSupportedPropNames()
        {
            return _properties.Keys;
        }
    }
}
