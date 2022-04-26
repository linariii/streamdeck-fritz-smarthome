using Newtonsoft.Json;

namespace FritzSmartHome.Models
{
    public class Device
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ain")]
        public string Ain { get; set; }
    }
}
