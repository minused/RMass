using System;
using System.Net;

namespace com.sulake.habboair
{
    public class HotelEndPoint : IPEndPoint
    {
        private String _host;

        public HotelEndPoint( IPEndPoint endpoint ) : base(endpoint.Address, endpoint.Port) { }

        public HotelEndPoint( Int64 address, Int32 port ) : base(address, port) { }

        public HotelEndPoint( IPAddress address, Int32 port ) : base(address, port) { }

        public HotelEndPoint( IPAddress address, Int32 port, String host ) : base(address, port)
        {
            Host = host;
        }

        public String Host
        {
            get => _host;
            set
            {
                _host = value;
                Hotel = GetHotel(value);
            }
        }

        public HHotel Hotel { get; private set; }

        public static HHotel GetHotel( String value )
        {
            if (String.IsNullOrWhiteSpace(value) || value.Length < 2) return HHotel.Unknown;

            if (value.StartsWith("hh")) value    = value.Substring(2, 2);
            if (value.StartsWith("game-")) value = value.Substring(5, 2);

            switch (value)
            {
                case "us": return HHotel.Com;
                case "br": return HHotel.ComBr;
                case "tr": return HHotel.ComTr;
                default:
                {
                    if (value.Length != 2 && value.Length != 5)
                    {
                        var hostIndex              = value.LastIndexOf("habbo");
                        if (hostIndex != -1) value = value.Substring(hostIndex + 5);

                        var comDotIndex              = value.IndexOf("com.");
                        if (comDotIndex != -1) value = value.Remove(comDotIndex + 3, 1);

                        if (value[0] == '.') value = value.Substring(1);
                        value = value.Substring(0, value.StartsWith("com") ? 5 : 2);
                    }

                    if (Enum.TryParse(value, true, out HHotel hotel)) return hotel;

                    break;
                }
            }

            return HHotel.Unknown;
        }

        public static String GetRegion( HHotel hotel )
        {
            switch (hotel)
            {
                case HHotel.Com:   return "us";
                case HHotel.ComBr: return "br";
                case HHotel.ComTr: return "tr";
                default:           return hotel.ToString().ToLower();
            }
        }

        public static HotelEndPoint GetEndPoint( HHotel hotel )
        {
            var port = hotel == HHotel.Com ? 38101 : 30000;
            var host = $"game-{GetRegion(hotel)}.habbo.com";

            return Parse(host, port);
        }

        public static HotelEndPoint Parse( String host, Int32 port )
        {
            var ips = Dns.GetHostAddresses(host);
            Console.WriteLine(ips[0]);

            return new HotelEndPoint(ips[0], port, host);
        }

        public static Boolean TryParse( String host, Int32 port, out HotelEndPoint endpoint )
        {
            try
            {
                endpoint = Parse(host, port);

                return true;
            }
            catch
            {
                endpoint = null;

                return false;
            }
        }

        public override String ToString()
        {
            if (!String.IsNullOrWhiteSpace(Host)) return $"{Hotel}:{Host}:{Port}";

            return Hotel + ":" + base.ToString();
        }
    }
}