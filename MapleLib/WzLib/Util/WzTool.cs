﻿using System;
using System.Collections;
using System.IO;
using MapleLib.MapleCryptoLib;

namespace MapleLib.WzLib.Util
{
    public static class WzTool
    {
        public static Hashtable StringCache = new Hashtable();

        public static uint RotateLeft(uint x, byte n)
        {
            return (x << n) | (x >> (32 - n));
        }

        public static uint RotateRight(uint x, byte n)
        {
            return (x >> n) | (x << (32 - n));
        }

        public static int GetCompressedIntLength(int i)
        {
            if (i > 127 || i < -127)
            {
                return 5;
            }

            return 1;
        }

        public static int GetEncodedStringLength(string s)
        {
            var len = 0;
            if (string.IsNullOrEmpty(s))
            {
                return 1;
            }

            var unicode = false;
            foreach (var c in s)
            {
                unicode |= c > 255;
            }

            if (unicode)
            {
                if (s.Length > 126)
                {
                    len += 5;
                }
                else
                {
                    len += 1;
                }

                len += s.Length * 2;
            }
            else
            {
                if (s.Length > 127)
                {
                    len += 5;
                }
                else
                {
                    len += 1;
                }

                len += s.Length;
            }

            return len;
        }

        public static int GetWzObjectValueLength(string s, byte type)
        {
            var storeName = type + "_" + s;
            if (s.Length > 4 && StringCache.ContainsKey(storeName))
            {
                return 5;
            }

            StringCache[storeName] = 1;
            return 1 + GetEncodedStringLength(s);
        }

        public static T StringToEnum<T>(string name)
        {
            try
            {
                return (T) Enum.Parse(typeof(T), name);
            }
            catch
            {
                return default(T);
            }
        }

        public static byte[] GetIvByMapleVersion(WzMapleVersion ver)
        {
            switch (ver)
            {
                case WzMapleVersion.Ems:
                    return CryptoConstants.WzMseaIv;
                case WzMapleVersion.Gms:
                    return CryptoConstants.WzGmsIv;
                default:
                    return new byte[4];
            }
        }

        private static int GetRecognizedCharacters(string source)
        {
            var result = 0;
            foreach (var c in source)
            {
                if (0x20 <= c && c <= 0x7E)
                {
                    result++;
                }
            }

            return result;
        }

        private static double GetDecryptionSuccessRate(string wzPath, WzMapleVersion encVersion, ref short? version)
        {
            var wzf = version == null
                ? new WzFile(wzPath, encVersion)
                : new WzFile(wzPath, (short) version, encVersion);

            wzf.ParseWzFile();
            if (version == null)
            {
                version = wzf.Version;
            }

            var recognizedChars = 0;
            var totalChars = 0;
            foreach (var wzdir in wzf.WzDirectory.WzDirectories)
            {
                recognizedChars += GetRecognizedCharacters(wzdir.Name);
                totalChars += wzdir.Name.Length;
            }

            foreach (var wzimg in wzf.WzDirectory.WzImages)
            {
                recognizedChars += GetRecognizedCharacters(wzimg.Name);
                totalChars += wzimg.Name.Length;
            }

            wzf.Dispose();
            return recognizedChars / (double) totalChars;
        }

        public static WzMapleVersion DetectMapleVersion(string wzFilePath, out short fileVersion)
        {
            var mapleVersionSuccessRates = new Hashtable();
            short? version = null;
            mapleVersionSuccessRates.Add(WzMapleVersion.Gms,
                GetDecryptionSuccessRate(wzFilePath, WzMapleVersion.Gms, ref version));
            mapleVersionSuccessRates.Add(WzMapleVersion.Ems,
                GetDecryptionSuccessRate(wzFilePath, WzMapleVersion.Ems, ref version));
            mapleVersionSuccessRates.Add(WzMapleVersion.Bms,
                GetDecryptionSuccessRate(wzFilePath, WzMapleVersion.Bms, ref version));
            fileVersion = (short) version;
            var mostSuitableVersion = WzMapleVersion.Gms;
            double maxSuccessRate = 0;
            foreach (DictionaryEntry mapleVersionEntry in mapleVersionSuccessRates)
            {
                if ((double) mapleVersionEntry.Value > maxSuccessRate)
                {
                    mostSuitableVersion = (WzMapleVersion) mapleVersionEntry.Key;
                    maxSuccessRate = (double) mapleVersionEntry.Value;
                }
            }

            if (maxSuccessRate < 0.7 && File.Exists(Path.Combine(Path.GetDirectoryName(wzFilePath), "ZLZ.dll")))
            {
                return WzMapleVersion.GetFromZlz;
            }

            return mostSuitableVersion;
        }

        public const int WzHeader = 0x31474B50; //PKG1

        public static bool IsListFile(string path)
        {
            var reader = new BinaryReader(File.OpenRead(path));
            var result = reader.ReadInt32() != WzHeader;
            reader.Close();
            return result;
        }
    }
}