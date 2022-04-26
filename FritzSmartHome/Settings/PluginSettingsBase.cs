using System;
using System.Collections.Generic;
using FritzSmartHome.Models;
using Newtonsoft.Json;

namespace FritzSmartHome.Settings
{
    public class PluginSettingsBase
    {
        [JsonProperty(PropertyName = "devices")]
        public List<Device> Devices { get; set; }

        [JsonProperty(PropertyName = "ain")]
        public string Ain { get; set; }

        [JsonProperty(PropertyName = "lastRefresh")]
        public DateTime LastRefresh { get; set; }
    }
}