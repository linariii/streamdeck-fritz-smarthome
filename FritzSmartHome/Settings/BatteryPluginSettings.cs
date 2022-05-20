using System;
using Newtonsoft.Json;

namespace FritzSmartHome.Settings
{
    public class BatteryPluginSettings : PluginSettingsBase
    {
        public static BatteryPluginSettings CreateDefaultSettings()
        {
            var instance = new BatteryPluginSettings
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