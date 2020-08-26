using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace com.sulake.habboair
{
    [DebuggerDisplay("Header: {Header}, Length: {Length} | {ToString()}")]
    public sealed class HMessage
    {
        public enum HDestination
        {
            Client = 0,
            Server = 1
        }

        private static readonly Regex      _valueGrabber;
        private readonly        List<Byte> _body;

        private readonly List<Object> _read;

        private readonly List<Object> _written;

        private UInt16 _header;

        private Int32  _position;
        private Byte[] _toBytesCache, _bodyBuffer;

        private String _toStringCache;

        static HMessage()
        {
            _valueGrabber = new Regex(@"{(?<type>u|s|i|b):(?<value>[^}]*)\}", RegexOptions.IgnoreCase);
        }

        private HMessage()
        {
            _body    = new List<Byte>();
            _read    = new List<Object>();
            _written = new List<Object>();
        }

        public HMessage( UInt16 header, params Object[] values ) : this(Construct(header, values), HDestination.Client)
        {
            _written.AddRange(values);
        }

        public HMessage( String data ) : this(data, HDestination.Client) { }

        public HMessage( String data, HDestination destination ) : this(ToBytes(data), destination) { }

        public HMessage( Byte[] data ) : this(data, HDestination.Client) { }

        public HMessage( Byte[] data, HDestination destination ) : this()
        {
            Destination = destination;
            IsCorrupted = data.Length < 6 || BigEndian.ToInt32(data, 0) != data.Length - 4;

            if (!IsCorrupted)
            {
                Header = BigEndian.ToUInt16(data, 4);

                _bodyBuffer = new Byte[data.Length - 6];
                Buffer.BlockCopy(data, 6, _bodyBuffer, 0, data.Length - 6);
            }
            else
                _bodyBuffer = data;

            _body.AddRange(_bodyBuffer);
        }

        public Int32 Position
        {
            get => _position;
            set => _position = value;
        }

        public UInt16 Header
        {
            get => _header;
            set
            {
                if (!IsCorrupted && _header != value)
                {
                    _header = value;
                    ResetCache();
                }
            }
        }

        public Int32 Readable => _body.Count - Position;
        public Int32 Length   => _body.Count + (!IsCorrupted ? 2 : 0);

        public Boolean               IsCorrupted   { get; }
        public HDestination          Destination   { get; set; }
        public IReadOnlyList<Object> ValuesRead    => _read;
        public IReadOnlyList<Object> ValuesWritten => _written;

        public Int32 ReadableAt( Int32 position )
        {
            return _body.Count - position;
        }

        public Boolean CanReadString()
        {
            return CanReadString(_position);
        }

        public Boolean CanReadString( Int32 position )
        {
            var readable = _body.Count - position;

            if (readable < 2) return false;

            var stringLength = BigEndian.ToUInt16(_bodyBuffer, position);

            return readable >= stringLength + 2;
        }

        private void Refresh()
        {
            ResetCache();
            _bodyBuffer = _body.ToArray();
        }

        private void ResetCache()
        {
            _toBytesCache  = null;
            _toStringCache = null;
        }

        public override String ToString()
        {
            return _toStringCache ?? (_toStringCache = ToString(ToBytes()));
        }

        public static String ToString( Byte[] data )
        {
            var result = Encoding.Default.GetString(data);

            for (var i = 0; i <= 13; i++)
                result = result.Replace(((Char) i).ToString(), "[" + i + "]");

            return result;
        }

        public Byte[] ToBytes()
        {
            if (IsCorrupted)
                _toBytesCache = _bodyBuffer;

            return _toBytesCache ?? (_toBytesCache = Construct(Header, _bodyBuffer));
        }

        public static Byte[] ToBytes( String packet )
        {
            for (var i = 0; i <= 13; i++)
                packet = packet.Replace("[" + i + "]", ((Char) i).ToString());

            var matches = _valueGrabber.Matches(packet);

            foreach (Match match in matches)
            {
                var type  = match.Groups["type"].Value;
                var value = match.Groups["value"].Value;

                Byte[] data = null;

                #region Switch: type

                switch (type)
                {
                    case "s":
                    {
                        data = BigEndian.GetBytes(value);

                        break;
                    }
                    case "u":
                    {
                        UInt16 uValue = 0;
                        UInt16.TryParse(value, out uValue);
                        data = BigEndian.GetBytes(uValue);

                        break;
                    }
                    case "i":
                    {
                        var iValue = 0;
                        Int32.TryParse(value, out iValue);
                        data = BigEndian.GetBytes(iValue);

                        break;
                    }
                    case "b":
                    {
                        Byte bValue = 0;

                        if (!Byte.TryParse(value, out bValue))
                            data  = BigEndian.GetBytes(value.ToLower() == "true");
                        else data = new[] { bValue };

                        break;
                    }
                }

                #endregion

                packet = packet.Replace(match.Value, Encoding.Default.GetString(data));
            }

            if (packet.StartsWith("{l}") && packet.Length >= 5)
            {
                var lengthData = BigEndian.GetBytes(packet.Length - 3);
                packet = Encoding.Default.GetString(lengthData) + packet.Substring(3);
            }

            return Encoding.Default.GetBytes(packet);
        }

        public static Byte[] GetBytes( params Object[] values )
        {
            var buffer = new List<Byte>();

            foreach (var value in values)
                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.Byte:
                        buffer.Add((Byte) value);

                        break;
                    case TypeCode.Boolean:
                        buffer.Add(Convert.ToByte((Boolean) value));

                        break;
                    case TypeCode.Int32:
                        buffer.AddRange(BigEndian.GetBytes((Int32) value));

                        break;
                    case TypeCode.UInt16:
                        buffer.AddRange(BigEndian.GetBytes((UInt16) value));

                        break;

                    default:
                    case TypeCode.String:
                    {
                        if (value is Byte[] data)
                            buffer.AddRange(data);
                        else buffer.AddRange(BigEndian.GetBytes(value.ToString()));

                        break;
                    }
                }

            return buffer.ToArray();
        }

        public static Byte[] Construct( UInt16 header, params Object[] values )
        {
            var body   = GetBytes(values);
            var buffer = new Byte[6 + body.Length];

            var headerData = BigEndian.GetBytes(header);
            var lengthData = BigEndian.GetBytes(2 + body.Length);

            Buffer.BlockCopy(lengthData, 0, buffer, 0, 4);
            Buffer.BlockCopy(headerData, 0, buffer, 4, 2);
            Buffer.BlockCopy(body, 0, buffer, 6, body.Length);

            return buffer;
        }

        #region Read Methods

        public Int32 ReadInteger()
        {
            return ReadInteger(ref _position);
        }

        public Int32 ReadInteger( Int32 position )
        {
            return ReadInteger(ref position);
        }

        public Int32 ReadInteger( ref Int32 position )
        {
            var value = BigEndian.ToInt32(_bodyBuffer, position);
            position += BigEndian.GetSize(value);

            _read.Add(value);

            return value;
        }

        public UInt16 ReadShort()
        {
            return ReadShort(ref _position);
        }

        public UInt16 ReadShort( Int32 position )
        {
            return ReadShort(ref position);
        }

        public UInt16 ReadShort( ref Int32 position )
        {
            var value = BigEndian.ToUInt16(_bodyBuffer, position);
            position += BigEndian.GetSize(value);

            _read.Add(value);

            return value;
        }

        public Boolean ReadBoolean()
        {
            return ReadBoolean(ref _position);
        }

        public Boolean ReadBoolean( Int32 position )
        {
            return ReadBoolean(ref position);
        }

        public Boolean ReadBoolean( ref Int32 position )
        {
            var value = BigEndian.ToBoolean(_bodyBuffer, position);
            position += BigEndian.GetSize(value);

            _read.Add(value);

            return value;
        }

        public String ReadString()
        {
            return ReadString(ref _position);
        }

        public String ReadString( Int32 position )
        {
            return ReadString(ref position);
        }

        public String ReadString( ref Int32 position )
        {
            var value = BigEndian.ToString(_bodyBuffer, position);
            position += BigEndian.GetSize(value);

            _read.Add(value);

            return value;
        }

        public Byte[] ReadBytes( Int32 length )
        {
            return ReadBytes(length, ref _position);
        }

        public Byte[] ReadBytes( Int32 length, Int32 position )
        {
            return ReadBytes(length, ref position);
        }

        public Byte[] ReadBytes( Int32 length, ref Int32 position )
        {
            var value = new Byte[length];
            Buffer.BlockCopy(_bodyBuffer, position, value, 0, length);
            position += length;

            _read.Add(value);

            return value;
        }

        #endregion

        #region Write Methods

        public void WriteInteger( Int32 value )
        {
            WriteInteger(value, _body.Count);
        }

        public void WriteInteger( Int32 value, Int32 position )
        {
            var encoded = BigEndian.GetBytes(value);
            WriteObject(encoded, value, position);
        }

        public void WriteShort( UInt16 value )
        {
            WriteShort(value, _body.Count);
        }

        public void WriteShort( UInt16 value, Int32 position )
        {
            var encoded = BigEndian.GetBytes(value);
            WriteObject(encoded, value, position);
        }

        public void WriteBoolean( Boolean value )
        {
            WriteBoolean(value, _body.Count);
        }

        public void WriteBoolean( Boolean value, Int32 position )
        {
            var encoded = BigEndian.GetBytes(value);
            WriteObject(encoded, value, position);
        }

        public void WriteString( String value )
        {
            WriteString(value, _body.Count);
        }

        public void WriteString( String value, Int32 position )
        {
            if (value == null)
                value = String.Empty;

            var encoded = BigEndian.GetBytes(value);
            WriteObject(encoded, value, position);
        }

        public void WriteBytes( Byte[] value )
        {
            WriteBytes(value, _body.Count);
        }

        public void WriteBytes( Byte[] value, Int32 position )
        {
            WriteObject(value, value, position);
        }

        private void WriteObjects( params Object[] values )
        {
            _written.AddRange(values);
            _body.AddRange(GetBytes(values));

            Refresh();
        }

        private void WriteObject( Byte[] encoded, Object value, Int32 position )
        {
            _written.Add(value);
            _body.InsertRange(position, encoded);

            Refresh();
        }

        #endregion

        #region Remove Methods

        public void RemoveInteger()
        {
            RemoveInteger(_position);
        }

        public void RemoveInteger( Int32 position )
        {
            RemoveBytes(4, position);
        }

        public void RemoveShort()
        {
            RemoveShort(_position);
        }

        public void RemoveShort( Int32 position )
        {
            RemoveBytes(2, position);
        }

        public void RemoveBoolean()
        {
            RemoveBoolean(_position);
        }

        public void RemoveBoolean( Int32 position )
        {
            RemoveBytes(1, position);
        }

        public void RemoveString()
        {
            RemoveString(_position);
        }

        public void RemoveString( Int32 position )
        {
            var readable = _body.Count - position;

            if (readable < 2) return;

            var stringLength = BigEndian.ToUInt16(_bodyBuffer, position);

            if (readable >= stringLength + 2)
                RemoveBytes(stringLength + 2, position);
        }

        public void RemoveBytes( Int32 length )
        {
            RemoveBytes(length, _position);
        }

        public void RemoveBytes( Int32 length, Int32 position )
        {
            _body.RemoveRange(position, length);
            Refresh();
        }

        #endregion

        #region Replace Methods

        public void ReplaceInteger( Int32 value )
        {
            ReplaceInteger(value, _position);
        }

        public void ReplaceInteger( Int32 value, Int32 position )
        {
            RemoveInteger(position);
            WriteInteger(value, position);
        }

        public void ReplaceShort( UInt16 value )
        {
            ReplaceShort(value, _position);
        }

        public void ReplaceShort( UInt16 value, Int32 position )
        {
            RemoveShort(position);
            WriteShort(value, position);
        }

        public void ReplaceBoolean( Boolean value )
        {
            ReplaceBoolean(value, _position);
        }

        public void ReplaceBoolean( Boolean value, Int32 position )
        {
            RemoveBoolean(position);
            WriteBoolean(value, position);
        }

        public void ReplaceString( String value )
        {
            ReplaceString(value, _position);
        }

        public void ReplaceString( String value, Int32 position )
        {
            var oldLength = Length;

            RemoveString(position);
            WriteString(value, position);

            if (position < _position)
                _position += (oldLength - Length) * -1;
        }

        #endregion
    }
}