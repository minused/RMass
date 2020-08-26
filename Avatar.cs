using System;

using Newtonsoft.Json;

namespace RMass
{
    internal class Avatar
    {
        [JsonProperty("uniqueId")]
        public String UniqueId { get; set; }

        [JsonProperty("name")]
        public String Name { get; set; }

        [JsonProperty("figureString")]
        public String FigureString { get; set; }

        [JsonProperty("motto")]
        public String Motto { get; set; }

        [JsonProperty("buildersClubMember")]
        public Boolean BuildersClubMember { get; set; }

        [JsonProperty("habboClubMember")]
        public Boolean HabboClubMember { get; set; }

        [JsonProperty("lastWebAccess")]
        public DateTime LastWebAccess { get; set; }

        [JsonProperty("creationTime")]
        public DateTime CreationTime { get; set; }

        [JsonProperty("banned")]
        public Boolean Banned { get; set; }

        public override String ToString()
        {
            return $"{{\"uniqueId\":\"{UniqueId}\"}}";
        }
    }
}