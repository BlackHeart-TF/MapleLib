﻿using System;
using System.Collections.Generic;
using System.IO;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib
{
    /// <summary>
    /// An interface for wz img properties
    /// </summary>
    public abstract class WzImageProperty : WzObject
    {
        #region Virtual\Abstrcat Members

        public virtual List<WzImageProperty> WzProperties => null;

        public new virtual WzImageProperty this[string name]
        {
            get => null;
            set => throw new NotImplementedException();
        }

        public virtual WzImageProperty GetFromPath(string path) => null;

        public abstract WzPropertyType PropertyType { get; }

        /// <summary>
        /// The image that this property is contained in
        /// </summary>
        public WzImage ParentImage
        {
            get
            {
                var parent = Parent;
                while (parent != null)
                {
                    if (parent is WzImage)
                    {
                        return (WzImage) parent;
                    }

                    parent = parent.Parent;
                }

                return null;
            }
        }

        public override WzObjectType ObjectType => WzObjectType.Property;

        public abstract void WriteValue(WzBinaryWriter writer);

        public abstract WzImageProperty DeepClone();

        public abstract void SetValue(object value);

        public override void Remove()
        {
            ((IPropertyContainer) Parent).RemoveProperty(this);
        }

        public virtual void ExportXml(StreamWriter writer, int level)
        {
            writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag(PropertyType.ToString(), Name, true));
            writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.CloseTag(PropertyType.ToString()));
        }

        public override WzFile WzFileParent => ParentImage.WzFileParent;

        #endregion

        #region Extended Properties Parsing

        internal static void WritePropertyList(WzBinaryWriter writer, List<WzImageProperty> properties)
        {
            writer.Write((ushort) 0);
            writer.WriteCompressedInt(properties.Count);
            foreach (var property in properties)
            {
                writer.WriteStringValue(property.Name, 0x00, 0x01);
                if (property is WzExtended extended)
                {
                    WriteExtendedValue(writer, extended);
                }
                else
                {
                    property.WriteValue(writer);
                }
            }
        }

        internal static void DumpPropertyList(StreamWriter writer, int level, List<WzImageProperty> properties)
        {
            foreach (var prop in properties)
            {
                prop.ExportXml(writer, level + 1);
            }
        }

        internal static List<WzImageProperty> ParsePropertyList(uint offset, WzBinaryReader reader, WzObject parent,
            WzImage parentImg)
        {
            var entryCount = reader.ReadCompressedInt();
            var properties = new List<WzImageProperty>(entryCount);
            for (var i = 0; i < entryCount; i++)
            {
                var name = reader.ReadStringBlock(offset);
                switch (reader.ReadByte())
                {
                    case 0:
                        properties.Add(new WzNullProperty(name) {Parent = parent});
                        break;
                    case 11:
                    case 2:
                        properties.Add(new WzShortProperty(name, reader.ReadInt16()) {Parent = parent});
                        break;
                    case 3:
                    case 19:
                        properties.Add(new WzIntProperty(name, reader.ReadCompressedInt()) {Parent = parent});
                        break;
                    case 20:
                        properties.Add(new WzLongProperty(name, reader.ReadLong()) {Parent = parent});
                        break;
                    case 4:
                        var type = reader.ReadByte();
                        if (type == 0x80)
                        {
                            properties.Add(new WzFloatProperty(name, reader.ReadSingle()) {Parent = parent});
                        }
                        else if (type == 0)
                        {
                            properties.Add(new WzFloatProperty(name, 0f) {Parent = parent});
                        }

                        break;
                    case 5:
                        properties.Add(new WzDoubleProperty(name, reader.ReadDouble()) {Parent = parent});
                        break;
                    case 8:
                        properties.Add(new WzStringProperty(name, reader.ReadStringBlock(offset)) {Parent = parent});
                        break;
                    case 9:
                        var eob = (int) (reader.ReadUInt32() + reader.BaseStream.Position);
                        WzImageProperty exProp = ParseExtendedProp(reader, offset, eob, name, parent, parentImg);
                        properties.Add(exProp);
                        reader.BaseStream.Position = eob;
                        break;
                    default:
                        throw new Exception("Unknown property type at ParsePropertyList");
                }
            }

            return properties;
        }

        internal static WzExtended ParseExtendedProp(WzBinaryReader reader, uint offset, int endOfBlock, string name,
            WzObject parent, WzImage imgParent)
        {
            switch (reader.ReadByte())
            {
                case 0x01:
                case 0x1B:
                    return ExtractMore(reader, offset, endOfBlock, name,
                        reader.ReadStringAtOffset(offset + reader.ReadInt32()), parent, imgParent);
                case 0x00:
                case 0x73:
                    return ExtractMore(reader, offset, endOfBlock, name, "", parent, imgParent);
                default:
                    throw new Exception("Invlid byte read at ParseExtendedProp");
            }
        }

        internal static WzExtended ExtractMore(WzBinaryReader reader, uint offset, int eob, string name, string iname,
            WzObject parent, WzImage imgParent)
        {
            if (iname == "")
            {
                iname = reader.ReadString();
            }

            switch (iname)
            {
                case "Property":
                    var subProp = new WzSubProperty(name) {Parent = parent};
                    reader.BaseStream.Position += 2; // Reserved?
                    subProp.AddProperties(ParsePropertyList(offset, reader, subProp, imgParent));
                    return subProp;
                case "Canvas":
                    var canvasProp = new WzCanvasProperty(name) {Parent = parent};
                    reader.BaseStream.Position++;
                    if (reader.ReadByte() == 1)
                    {
                        reader.BaseStream.Position += 2;
                        canvasProp.AddProperties(ParsePropertyList(offset, reader, canvasProp, imgParent));
                    }

                    canvasProp.PngProperty = new WzPngProperty(reader, imgParent.parseEverything) {Parent = canvasProp};
                    return canvasProp;
                case "Shape2D#Vector2D":
                    var vecProp = new WzVectorProperty(name) {Parent = parent};
                    vecProp.X = new WzIntProperty("X", reader.ReadCompressedInt()) {Parent = vecProp};
                    vecProp.Y = new WzIntProperty("Y", reader.ReadCompressedInt()) {Parent = vecProp};
                    return vecProp;
                case "Shape2D#Convex2D":
                    var convexProp = new WzConvexProperty(name) {Parent = parent};
                    var convexEntryCount = reader.ReadCompressedInt();
                    convexProp.WzProperties.Capacity = convexEntryCount;
                    for (var i = 0; i < convexEntryCount; i++)
                    {
                        convexProp.AddProperty(ParseExtendedProp(reader, offset, 0, name, convexProp, imgParent));
                    }

                    return convexProp;
                case "Sound_DX8":
                    var soundProp = new WzSoundProperty(name, reader, imgParent.parseEverything) {Parent = parent};
                    return soundProp;
                case "UOL":
                    reader.BaseStream.Position++;
                    switch (reader.ReadByte())
                    {
                        case 0:
                            return new WzUOLProperty(name, reader.ReadString()) {Parent = parent};
                        case 1:
                            return new WzUOLProperty(name, reader.ReadStringAtOffset(offset + reader.ReadInt32()))
                                {Parent = parent};
                    }

                    throw new Exception("Unsupported UOL type");
                default:
                    throw new Exception("Unknown iname: " + iname);
            }
        }

        internal static void WriteExtendedValue(WzBinaryWriter writer, WzExtended property)
        {
            writer.Write((byte) 9);
            var beforePos = writer.BaseStream.Position;
            writer.Write(0); // Placeholder
            property.WriteValue(writer);
            var len = (int) (writer.BaseStream.Position - beforePos);
            var newPos = writer.BaseStream.Position;
            writer.BaseStream.Position = beforePos;
            writer.Write(len - 4);
            writer.BaseStream.Position = newPos;
        }

        #endregion
    }
}