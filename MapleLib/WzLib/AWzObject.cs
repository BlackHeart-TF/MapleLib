﻿using System;
using MapleLib.WzLib.WzProperties;
using System.Drawing;

namespace MapleLib.WzLib
{
	/// <summary>
	/// An interface for wz objects
	/// </summary>
	public abstract class AWzObject : IDisposable
	{

		public abstract void Dispose();

		/// <summary>
		/// The name of the object
		/// </summary>
		public abstract string Name { get; set; }
		/// <summary>
		/// The WzObjectType of the object
		/// </summary>
		public abstract WzObjectType ObjectType { get; }
		/// <summary>
		/// Returns the parent object
		/// </summary>
		public abstract AWzObject Parent { get; internal set; }

        public virtual object WzValue { get; set; }

        public Object Tag { get; set; }

        public string FullPath
        {
            get
            {
                string result = this.Name;
                AWzObject currObj = this;
                while (currObj.Parent != null)
                {
                    currObj = currObj.Parent;
                    result = currObj.Name + @"\" + result;
                }
                return result;
            }
        }

        //public abstract void Remove();

        #region Cast Values
        public static explicit operator float(AWzObject obj)
        {
            return obj.ToFloat(0);
        }

        public static explicit operator int(AWzObject obj)
        {
            return obj.ToInt(0);
        }

        public static explicit operator double(AWzObject obj)
        {
            return obj.ToDouble(0);
        }

        public static explicit operator System.Drawing.Bitmap(AWzObject obj)
        {
            return obj.ToBitmap(null);
        }

        public static explicit operator byte[](AWzObject obj)
        {
            return obj.ToBytes(null);
        }

        public static explicit operator string(AWzObject obj)
        {
            return obj.ToString();
        }

        public static explicit operator ushort(AWzObject obj)
        {
            return obj.ToUnsignedShort(0);
        }

        public static explicit operator System.Drawing.Point(AWzObject obj)
        {
            return obj.ToPoint(0, 0);
        }

        internal virtual float ToFloat(float def = 0)
        {
            return def;
        }

        internal virtual WzPngProperty ToPngProperty(WzPngProperty def = null)
        {
            return def;
        }

        internal virtual int ToInt(int def = 0)
        {
            return def;
        }

        internal virtual double ToDouble(double def = 0)
        {
            return def;
        }

        internal virtual Bitmap ToBitmap(Bitmap def = null)
        {
            return def;
        }

        internal virtual byte[] ToBytes(byte[] def = null)
        {
            return def;
        }

        public override string ToString()
        {
            return WzValue.ToString();
        }

        internal virtual ushort ToUnsignedShort(ushort def = 0)
        {
            return def;
        }

        internal virtual Point ToPoint(int pXDef = 0, int pYDef = 0)
        {
            return new Point(pXDef, pYDef);
        }

        #endregion


	}
}