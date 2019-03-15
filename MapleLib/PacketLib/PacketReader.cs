﻿using System.IO;
using System.Text;

namespace MapleLib.PacketLib
{
    /// <summary>
    /// Class to handle reading data from a packet
    /// </summary>
    public class PacketReader : AbstractPacket
    {
        /// <summary>
        /// The main reader tool
        /// </summary>
        private readonly BinaryReader _binReader;

        /// <summary>
        /// Amount of data left in the reader
        /// </summary>
        public short Length => (short) _buffer.Length;

        /// <summary>
        /// Creates a new instance of PacketReader
        /// </summary>
        /// <param name="_arrayOfBytes">Starting byte array</param>
        public PacketReader(byte[] _arrayOfBytes)
        {
            _buffer = new MemoryStream(_arrayOfBytes, false);
            _binReader = new BinaryReader(_buffer, Encoding.ASCII);
        }

        /// <summary>
        /// Restart reading from the point specified.
        /// </summary>
        /// <param name="length">The point of the packet to start reading from.</param>
        public void Reset(int length) => _buffer.Seek(length, SeekOrigin.Begin);

        public void Skip(int pLength) => _buffer.Position += pLength;

        /// <summary>
        /// Reads an unsigned byte from the stream
        /// </summary>
        /// <returns> an unsigned byte from the stream</returns>
        public byte ReadByte() => _binReader.ReadByte();

        /// <summary>
        /// Reads a byte array from the stream
        /// </summary>
        /// <param name="count">Amount of bytes</param>
        /// <returns>A byte array</returns>
        public byte[] ReadBytes(int count) => _binReader.ReadBytes(count);

        /// <summary>
        /// Reads a bool from the stream
        /// </summary>
        /// <returns>A bool</returns>
        public bool ReadBool() => _binReader.ReadBoolean();

        /// <summary>
        /// Reads a signed short from the stream
        /// </summary>
        /// <returns>A signed short</returns>
        public short ReadShort() => _binReader.ReadInt16();

        /// <summary>
        /// Reads a signed int from the stream
        /// </summary>
        /// <returns>A signed int</returns>
        public int ReadInt() => _binReader.ReadInt32();

        /// <summary>
        /// Reads a signed long from the stream
        /// </summary>
        /// <returns>A signed long</returns>
        public long ReadLong() => _binReader.ReadInt64();

        /// <summary>
        /// Reads an ASCII string from the stream
        /// </summary>
        /// <param name="pLength">Amount of bytes</param>
        /// <returns>An ASCII string</returns>
        public string ReadString(int pLength) => Encoding.ASCII.GetString(ReadBytes(pLength));

        /// <summary>
        /// Reads a maple string from the stream
        /// </summary>
        /// <returns>A maple string</returns>
        public string ReadMapleString() => ReadString(ReadShort());
    }
}