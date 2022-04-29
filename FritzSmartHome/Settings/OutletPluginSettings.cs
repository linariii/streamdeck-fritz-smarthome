using System;
using Newtonsoft.Json;

namespace FritzSmartHome.Settings
{
    public class OutletPluginSettings : PluginSettingsBase
    {
        public static OutletPluginSettings CreateDefaultSettings()
        {
            var instance = new OutletPluginSettings
            {
                LastRefresh = DateTime.MinValue
            };
            return instance;
        }

        [JsonProperty(PropertyName = "state")]
        public int? State { get; set; }
    }
}