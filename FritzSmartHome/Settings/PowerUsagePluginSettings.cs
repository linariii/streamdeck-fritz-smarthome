using System;
using Newtonsoft.Json;

namespace FritzSmartHome.Settings
{
    public class PowerUsagePluginSettings : PluginSettingsBase
    {
        public static PowerUsagePluginSettings CreateDefaultSettings()
        {
            var instance = new PowerUsagePluginSettings
            {
                LastRefresh = DateTime.MinValue,
            };
            return instance;
        }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }
    }
}