using Newtonsoft.Json;

namespace FritzSmartHome.Actions.Models
{
    public class Device
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ain")]
        public string Ain { get; set; }
    }
}
