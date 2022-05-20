using System;
using Newtonsoft.Json;

namespace FritzSmartHome.Settings
{
    public class HumiditPluginSettings : PluginSettingsBase
    {
        public static HumiditPluginSettings CreateDefaultSettings()
        {
            var instance = new HumiditPluginSettings
            {
                LastRefresh = DateTime.MinValue
            };
            return instance;
        }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName ="data")]
        public uint? Data { get; set; }
    }
}