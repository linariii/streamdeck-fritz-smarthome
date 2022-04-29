using System;
using Newtonsoft.Json;

namespace FritzSmartHome.Settings
{
    public class TemperaturePluginSettings : PluginSettingsBase
    {
        public static TemperaturePluginSettings CreateDefaultSettings()
        {
            var instance = new TemperaturePluginSettings
            {
                LastRefresh = DateTime.MinValue
            };
            return instance;
        }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty("data")]
        public double? Data { get; set; }
    }
}