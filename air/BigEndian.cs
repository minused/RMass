using System;
using System.Text;

namespace com.sulake.habboair
{
    /// <summary>
    ///     Converts base data types to an array of bytes in the big-endian byte order, and an array of bytes in the big-endian
    ///     byte order to base data types.
    /// </summary>
    public static class BigEndian
    {
        public static Int32 GetSize( Int32 value )
        {
            return 4;
        }

        public static Int32 GetSize( Boolean value )
        {
            return 1;
        }

        public static Int32 GetSize( UInt16 value )
        {
            return 2;
        }

        public static Int32 GetSize( String value )
        {
            return Encoding.UTF8.GetByteCount(value) + 2;
        }

        /// <summary>
        ///     Returns the specified 32-bit signed integer value as an array of bytes in the big-endian byte order.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns></returns>
        public static Byte[] GetBytes( Int32 value )
        {
            var buffer = new Byte[4];
            buffer[0] = (Byte) (value >> 24);
            buffer[1] = (Byte) (value >> 16);
            buffer[2] = (Byte) (value >> 8);
            buffer[3] = (Byte) value;

            return buffer;
        }

        public static Byte[] GetBytes( Boolean value )
        {
            var buffer = new Byte[1] { 0 };
            buffer[0] = (Byte) (value ? 1 : 0);

            return buffer;
        }

        /// <summary>
        ///     Returns the specified 16-bit unsigned integer value as an array of bytes in the big-endian byte order.
        /// </summary>
        /// <param name="value">The number to convert.</param>
        /// <returns></returns>
        public static Byte[] GetBytes( UInt16 value )
        {
            var buffer = new Byte[2];
            buffer[0] = (Byte) (value >> 8);
            buffer[1] = (Byte) value;

            return buffer;
        }

        public static Byte[] GetBytes( String value )
        {
            var stringData = Encoding.UTF8.GetBytes(value);
            var lengthData = GetBytes((UInt16) stringData.Length);

            var buffer = new Byte[lengthData.Length + stringData.Length];
            Buffer.BlockCopy(lengthData, 0, buffer, 0, lengthData.Length);
            Buffer.BlockCopy(stringData, 0, buffer, lengthData.Length, stringData.Length);

            return buffer;
        }

        /// <summary>
        ///     Returns a 32-bit signed integer converted from four bytes at a specified position in a byte array using the
        ///     big-endian byte order.
        /// </summary>
        /// <param name="value">An array of bytes in the big-endian byte order.</param>
        /// <param name="startIndex">The starting position within the value.</param>
        /// <returns></returns>
        public static Int32 ToInt32( Byte[] value, Int32 startIndex )
        {
            var result = value[startIndex++] << 24;
            result += value[startIndex++] << 16;
            result += value[startIndex++] << 8;
            result += value[startIndex];

            return result;
        }

        public static Boolean ToBoolean( Byte[] value, Int32 startIndex )
        {
            return value[startIndex] == 1;
        }

        /// <summary>
        ///     Returns a 16-bit unsigned integer converted from two bytes at a specified position in a byte array using the
        ///     big-endian byte order.
        /// </summary>
        /// <param name="value">An array of bytes in the big-endian byte order.</param>
        /// <param name="startIndex">The starting position within the value.</param>
        /// <returns></returns>
        public static UInt16 ToUInt16( Byte[] value, Int32 startIndex )
        {
            var result = value[startIndex++] << 8;
            result += value[startIndex];

            return (UInt16) result;
        }

        public static String ToString( Byte[] value, Int32 startIndex )
        {
            var stringLength = ToUInt16(value, startIndex);

            var result = Encoding.UTF8.GetString(value, startIndex + 2, stringLength);

            return result;
        }
    }
}