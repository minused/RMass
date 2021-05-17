using System.Collections.Generic;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

namespace RMass
{
    public static class Header
    {
        private static readonly HeaderModel _headers;

        static Header()
        {
            _headers ??= JsonConvert.DeserializeObject<HeaderModel>(File.ReadAllText("messages.json"));
        }

        private static ushort GetHeader(string headerName, bool incoming)
        {
            if (incoming) return (ushort) _headers.Incoming.First(x => x.Name == headerName).Id;

            return (ushort) _headers.Outgoing.First(x => x.Name == headerName).Id;
        }

        public static ushort GetIncomingHeader(string headerName)
        {
            return GetHeader(headerName, true);
        }

        public static ushort GetOutgoingHeader(string headerName)
        {
            return GetHeader(headerName, false);
        }
    }

    public class Incoming
    {
        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }
    }

    public class Outgoing
    {
        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }
    }

    public class HeaderModel
    {
        [JsonProperty("Incoming")]
        public List<Incoming> Incoming { get; set; }

        [JsonProperty("Outgoing")]
        public List<Outgoing> Outgoing { get; set; }
    }
}