using Newtonsoft.Json;

namespace FritzSmartHome.Actions
{
    public class GlobalPluginSettings
    {
        public static GlobalPluginSettings CreateDefaultSettings()
        {
            GlobalPluginSettings instance = new GlobalPluginSettings
            {
                BaseUrl = "http://fritz.box/"
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