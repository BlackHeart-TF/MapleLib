﻿using System.IO;
using System.Text;
using MapleLib.MapleCryptoLib;

namespace MapleLib.WzLib.Util
{
    public class WzBinaryReader : BinaryReader
    {
        #region Properties
        public WzMutableKey WzKey { get; set; }
        public uint Hash { get; set; }
        public WzHeader Header { get; set; }
        #endregion

        #region Constructors
        public WzBinaryReader(Stream input, byte[] WzIv)
            : base(input)
        {
            WzKey = WzKeyGenerator.GenerateWzKey(WzIv);
        }
        #endregion

        #region Methods

        public string ReadStringAtOffset(long Offset, bool readByte = false)
        {
            var CurrentOffset = BaseStream.Position;
            BaseStream.Position = Offset;
            if (readByte)
            {
                ReadByte();
            }
            var ReturnString = ReadString();
            BaseStream.Position = CurrentOffset;
            return ReturnString;
        }

        public override string ReadString()
        {
            var smallLength = base.ReadSByte();

            if (smallLength == 0)
            {
                return string.Empty;
            }

            int length;
            var retString = new StringBuilder();
            if (smallLength > 0) // Unicode
            {
                ushort mask = 0xAAAA;
                length = smallLength == sbyte.MaxValue ? ReadInt32() : smallLength;
                if (length <= 0)
                {
                    return string.Empty;
                }

                for (var i = 0; i < length; i++)
                {
                    var encryptedChar = ReadUInt16();
                    encryptedChar ^= mask;
                    encryptedChar ^= (ushort)((WzKey[i * 2 + 1] << 8) + WzKey[i * 2]);
                    retString.Append((char)encryptedChar);
                    mask++;
                }
            }
            else
            { // ASCII
                byte mask = 0xAA;
                if (smallLength == sbyte.MinValue)
                {
                    length = ReadInt32();
                }
                else
                {
                    length = -smallLength;
                }
                if (length <= 0)
                {
                    return string.Empty;
                }

                for (var i = 0; i < length; i++)
                {
                    var encryptedChar = ReadByte();
                    encryptedChar ^= mask;
                    encryptedChar ^= WzKey[i];
                    retString.Append((char)encryptedChar);
                    mask++;
                }
            }
            return retString.ToString();
        }

        /// <summary>
        /// Reads an ASCII string, without decryption
        /// </summary>
        /// <param name="length">Length of bytes to read</param>
        public string ReadString(int length)
        {
            return Encoding.ASCII.GetString(ReadBytes(length));
        }

        public string ReadNullTerminatedString()
        {
            var retString = new StringBuilder();
            var b = ReadByte();
            while (b != 0)
            {
                retString.Append((char)b);
                b = ReadByte();
            }
            return retString.ToString();
        }

        public int ReadCompressedInt()
        {
            var sb = base.ReadSByte();
            return sb == sbyte.MinValue ? ReadInt32() : sb;
        }

        public long ReadLong()
        {
            var sb = base.ReadSByte();
            return sb == sbyte.MinValue ? ReadInt64() : sb;
        }

        public uint ReadOffset()
        {
            var offset = (uint)BaseStream.Position;
            offset = (offset - Header.FStart) ^ uint.MaxValue;
            offset *= Hash;
            offset -= CryptoConstants.WzOffsetConstant;
            offset = WzTool.RotateLeft(offset, (byte)(offset & 0x1F));
            var encryptedOffset = ReadUInt32();
            offset ^= encryptedOffset;
            offset += Header.FStart * 2;
            return offset;
        }

        public string DecryptString(char[] stringToDecrypt)
        {
            var outputString = "";
            for (var i = 0; i < stringToDecrypt.Length; i++)
            {
                outputString += (char)(stringToDecrypt[i] ^ ((char)((WzKey[i * 2 + 1] << 8) + WzKey[i * 2])));
            }

            return outputString;
        }

        public string DecryptNonUnicodeString(char[] stringToDecrypt)
        {
            var outputString = "";
            for (var i = 0; i < stringToDecrypt.Length; i++)
            {
                outputString += (char)(stringToDecrypt[i] ^ WzKey[i]);
            }

            return outputString;
        }

        public string ReadStringBlock(uint offset)
        {
            switch (ReadByte())
            {
                case 0:
                case 0x73:
                    return ReadString();
                case 1:
                case 0x1B:
                    return ReadStringAtOffset(offset + ReadInt32());
                default:
                    return "";
            }
        }

        #endregion
    }
}