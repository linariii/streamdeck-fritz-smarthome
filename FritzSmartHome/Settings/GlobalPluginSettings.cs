using Newtonsoft.Json;

namespace FritzSmartHome.Settings
{
    public class GlobalPluginSettings
    {
        public static GlobalPluginSettings CreateDefaultSettings()
        {
            var instance = new GlobalPluginSettings
            {
                BaseUrl = "http://fritz.box"
            };
            return instance;
        }

        [JsonProperty(PropertyName = "baseUrl")]
        public string BaseUrl { get; set; }

        [JsonProperty(PropertyName = "userName")]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; }

        [JsonProperty(PropertyName = "sid")]
        public string Sid { get; set; }
    }
}