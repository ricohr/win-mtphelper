//
//  Copyright (c) 2017 Ricoh Company, Ltd. All Rights Reserved.
//  See LICENSE for more information.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using WpdMtpLib;
using WpdMtpLib.DeviceProperty;


namespace MtpHelper
{
    public class MtpHelperUtils
    {
        /*
        static String EncodeDeviceId(String deviceId)
        {
            MemoryStream ms = new MemoryStream();
            DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress, true);
            byte[] k = System.Text.Encoding.Unicode.GetBytes(deviceId);
            ds.Write(k, 0, k.Length);
            ds.Close();
            return System.Convert.ToBase64String(ms.ToArray());
        }

        static String DecodeDeviceId(String key)
        {
            MemoryStream ms = new MemoryStream(System.Convert.FromBase64String(key));
            DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress, false);
            byte[] k = new byte[256];
            ds.Read(k, 0, 256);
            ds.Close();
            return System.Text.Encoding.Unicode.GetString(k);
        }
         */

        /// <summary>
        /// DataType string to DataType
        /// </summary>
        static Dictionary<String, DataType> _type2datatype = new Dictionary<String, DataType>()
        {
            {"INT8",    DataType.INT8},
            {"INT16",   DataType.INT16},
            {"INT32",   DataType.INT32},
            {"INT64",   DataType.INT64},
            {"INT128",  DataType.INT128},
            {"UINT8",   DataType.UINT8},
            {"UINT16",  DataType.UINT16},
            {"UINT32",  DataType.UINT32},
            {"UINT64",  DataType.UINT64},
            {"UINT128", DataType.UINT128},

            {"AINT8",   DataType.AINT8},
            {"AINT16",  DataType.AINT16},
            {"AINT32",  DataType.AINT32},
            {"AINT64",  DataType.AINT64},
            {"AINT128", DataType.AINT128},
            {"AUINT8",  DataType.AUINT8},
            {"AUINT16", DataType.AUINT16},
            {"AUINT32", DataType.AUINT32},
            {"AUINT64", DataType.AUINT64},
            {"AUINT128",DataType.AUINT128},

            {"STR",     DataType.STR}
        };


        static public DataType TypeToDataType(String name)
        {
            try
            {
                return _type2datatype[name];
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                throw new MtpHelperRuntimeException(String.Format("Invalid type name({0})", name));
            }
        }


        /// <summary>
        /// Hashtable#.ToJSON
        /// </summary>
        public static String ToJSON(Hashtable hash)
        {
            String ret = "";
            foreach (String key in hash.Keys)
            {
                String s;
                var v = hash[key];
                if (v is Hashtable)
                {
                    s = ToJSON((Hashtable)v);
                }
                else if (v is Array)
                {
                    s = ToJSON((Array)v);
                }
                else if (v is String)
                {
                    s = "\"" + v + "\"";
                }
                else if (v == null)
                {
                    s = "null";
                }
                else
                {
                    s = v.ToString();
                }
                if (ret.Length > 0) { ret += ","; }
                ret += "\"" + key + "\":" + s;
            }
            return "{" + ret + "}";
        }


        static String ToJSON(Array array)
        {
            String ret = "";
            foreach (var v in array)
            {
                if (ret.Length > 0) { ret += ","; }
                if (v is String)
                {
                    ret += "\"" + v + "\"";
                }
                else
                {
                    ret += v.ToString();
                }
            }
            return "[" + ret + "]";
        }


        /// <summary>
        /// MTP-Bytes to Generic
        /// </summary>
        public static dynamic BytesToValue(byte[] data, DataType type)
        {
            int pos = 0;
            switch (type)
            {
                case DataType.INT8:
                    return (SByte)data[0];
                case DataType.UINT8:
                    return (Byte)data[0];
                case DataType.INT16:
                    return BitConverter.ToInt16(data, 0);
                case DataType.UINT16:
                    return BitConverter.ToUInt16(data, 0);
                case DataType.INT32:
                    return BitConverter.ToInt32(data, 0);
                case DataType.UINT32:
                    return BitConverter.ToUInt32(data, 0);
                case DataType.INT64:
                    return BitConverter.ToInt64(data, 0);
                case DataType.UINT64:
                    return BitConverter.ToUInt64(data, 0);
                case DataType.STR:
                    return Utils.GetString(data, ref pos);
                case DataType.INT128:
                    break;
                case DataType.UINT128:
                    break;
                case DataType.AINT8:
                    break;
                case DataType.AUINT8:
                    break;
                case DataType.AINT16:
                    break;
                case DataType.AUINT16:
                    return Utils.GetUShortArray(data, ref pos);
                case DataType.AINT32:
                    break;
                case DataType.AUINT32:
                    return Utils.GetUShortArray(data, ref pos);
                case DataType.AINT64:
                    break;
                case DataType.AUINT64:
                    break;
                case DataType.AINT128:
                    break;
                case DataType.AUINT128:
                    break;
                default:
                    break;
            }
            throw new MtpHelperRuntimeException(String.Format("Unsupported data type({0:X4})", (UInt16)type));
        }


        /// <summary>
        /// String to MTP-Bytes
        /// </summary>
        public static byte[] StringToBytes(String value, DataType type)
        {
            switch (type)
            {
                case DataType.INT8:
                    return new byte[1] { (byte)Convert.ToSByte(value) };
                case DataType.UINT8:
                    return new byte[1] { (byte)Convert.ToByte(value) };
                case DataType.INT16:
                    return BitConverter.GetBytes(Convert.ToInt16(value));
                case DataType.UINT16:
                    return BitConverter.GetBytes(Convert.ToUInt16(value));
                case DataType.INT32:
                    return BitConverter.GetBytes(Convert.ToInt32(value));
                case DataType.UINT32:
                    return BitConverter.GetBytes(Convert.ToUInt32(value));
                case DataType.INT64:
                    return BitConverter.GetBytes(Convert.ToInt64(value));
                case DataType.UINT64:
                    return BitConverter.GetBytes(Convert.ToUInt64(value));
                case DataType.STR:
                    return JoinBytes(new byte[1] { (byte)value.Length }, System.Text.Encoding.Unicode.GetBytes(value));
                case DataType.INT128:
                    break;
                case DataType.UINT128:
                    break;
                case DataType.AINT8:
                    break;
                case DataType.AUINT8:
                    break;
                case DataType.AINT16:
                    break;
                case DataType.AUINT16:
                    break;
                case DataType.AINT32:
                    break;
                case DataType.AUINT32:
                    break;
                case DataType.AINT64:
                    break;
                case DataType.AUINT64:
                    break;
                case DataType.AINT128:
                    break;
                case DataType.AUINT128:
                    break;
                default:
                    break;
            }
            throw new MtpHelperRuntimeException(String.Format("Unsupported data type({0:X4})", (UInt16)type));
        }


        /// <summary>
        /// join byte[]
        /// </summary>
        public static byte[] JoinBytes(params byte[][] array)
        {
            var total = 0;
            foreach (byte[] i in array)
            {
                total += i.Length;
            }
            byte[] res = new byte[total];
            var offset = 0;
            foreach (byte[] i in array)
            {
                System.Buffer.BlockCopy(i, 0, res, offset, i.Length);
                offset += i.Length;
            }
            return res;
        }
    }
}
