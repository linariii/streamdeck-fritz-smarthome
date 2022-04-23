using BarRaider.SdTools;
using FritzSmartHome.Actions.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fritz.HomeAutomation;
using FritzSmartHome.Backend;

namespace FritzSmartHome.Actions
{
    [PluginActionId("com.linariii.outlet")]
    public class Outlet : PluginBase
    {
        private const int FetchCooldownSec = 60; // 1 min
        private readonly PluginSettings _settings;
        private readonly GlobalPluginSettings _globalSettings;

        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                var instance = new PluginSettings
                {
                    LastRefresh = DateTime.MinValue,
                };
                return instance;
            }

            [JsonProperty(PropertyName = "devices")]
            public List<Device> Devices { get; set; }

            [JsonProperty(PropertyName = "ain")]
            public string Ain { get; set; }

            [JsonProperty(PropertyName = "lastRefresh")]
            public DateTime LastRefresh { get; set; }

            [JsonProperty(PropertyName = "state")]
            public int State { get; set; }
        }

        public Outlet(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Constructor called");
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                _globalSettings = GlobalPluginSettings.CreateDefaultSettings();
                _settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
                _globalSettings = GlobalPluginSettings.CreateDefaultSettings();
                _settings = payload.Settings.ToObject<PluginSettings>();
                if (_settings != null)
                    _settings.LastRefresh = DateTime.MinValue;
            }

            GlobalSettingsManager.Instance.RequestGlobalSettings();
            UpdateBaseUrl();
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        public override async void KeyPressed(KeyPayload payload)
        {
            if (_globalSettings == null || _settings == null)
                return;
            try
            {
                if (!string.IsNullOrWhiteSpace(_globalSettings.Sid) && !string.IsNullOrWhiteSpace(_settings.Ain))
                {
                    var state = await HomeAutomationClientWrapper.Instance.SetSwitchToggle(_globalSettings.Sid, _settings.Ain);
                    if (state.HasValue)
                    {
                        if (_settings.State != state.Value)
                        {
                            _settings.State = state.Value;
                            await Connection.SetStateAsync((uint)state.Value);
                        }
                    }
                    _settings.LastRefresh = DateTime.Now;
                    await SaveSettings();
                }
            }
            catch (Exception ex)
            {
                await Connection.ShowAlert();
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                if (!string.IsNullOrEmpty(_globalSettings.Sid))
                {
                    _globalSettings.Sid = null;
                    await SaveGlobalSettings();
                }
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override async void OnTick()
        {
            if (_globalSettings == null || _settings == null)
                return;

            if (string.IsNullOrWhiteSpace(_globalSettings.Sid)
                && !string.IsNullOrWhiteSpace(_globalSettings.UserName)
                && !string.IsNullOrWhiteSpace(_globalSettings.Password)
                && !string.IsNullOrWhiteSpace(_globalSettings.BaseUrl))
            {
                await Login();
                return;
            }

            if (!string.IsNullOrWhiteSpace(_globalSettings.Sid) && (_settings.Devices == null || !_settings.Devices.Any()))
            {
                await LoadDevices();
                return;
            }

            if (!string.IsNullOrWhiteSpace(_globalSettings.Sid) && !string.IsNullOrWhiteSpace(_settings.Ain))
            {
                await LoadState();
            }
        }

        private async Task Login()
        {
            try
            {
                var sid = await HomeAutomationClientWrapper.Instance.GetSid(_globalSettings.UserName, _globalSettings.Password);
                if (!string.IsNullOrWhiteSpace(sid) && sid != "0000000000000000")
                {
                    await Connection.ShowOk();
                    _globalSettings.Sid = sid;
                    await SaveGlobalSettings();
                }
                else
                {
                    await Connection.ShowAlert();
                }
            }
            catch (Exception ex)
            {
                await Connection.ShowAlert();
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                if (!string.IsNullOrEmpty(_globalSettings.Sid))
                {
                    _globalSettings.Sid = null;
                    await SaveGlobalSettings();
                }
            }
        }

        private async Task LoadState()
        {
            if ((DateTime.Now - _settings.LastRefresh).TotalSeconds > FetchCooldownSec
                && !string.IsNullOrWhiteSpace(_globalSettings.Sid)
                && !string.IsNullOrWhiteSpace(_settings.Ain))
            {
                try
                {
                    var data = await HomeAutomationClientWrapper.Instance.GetSwitchState(_globalSettings.Sid, _settings.Ain);
                    if (data.HasValue)
                    {
                        if (_settings.State != data.Value)
                        {
                            _settings.State = data.Value;
                            await Connection.SetStateAsync((uint)data.Value);
                        }
                    }
                    _settings.LastRefresh = DateTime.Now;
                    await SaveSettings();
                }
                catch (Exception ex)
                {
                    await Connection.ShowAlert();
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                    if (!string.IsNullOrEmpty(_globalSettings.Sid))
                    {
                        _globalSettings.Sid = null;
                        await SaveGlobalSettings();
                    }
                }
            }
        }

        private async Task LoadDevices()
        {
            if ((DateTime.Now - _settings.LastRefresh).TotalSeconds > FetchCooldownSec && !string.IsNullOrWhiteSpace(_globalSettings.Sid))
            {
                try
                {
                    var devices = await HomeAutomationClientWrapper.Instance.GetFilteredDevices(_globalSettings.Sid, Functions.Outlet);
                    if (devices != null && devices.Any())
                    {
                        _settings.Devices = devices.Select(d => new Device { Ain = d.Identifier, Name = d.Name }).ToList(); ;
                    }

                    _settings.LastRefresh = DateTime.Now;
                    await SaveSettings();
                }
                catch (Exception ex)
                {
                    await Connection.ShowAlert();
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                    if (!string.IsNullOrEmpty(_globalSettings.Sid))
                    {
                        _globalSettings.Sid = null;
                        await SaveGlobalSettings();
                    }
                }
            }
        }

        public override async void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedSettings");
            if (payload.Settings != null && payload.Settings.Count > 0)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedSettings: {payload.Settings}");
                if (Tools.AutoPopulateSettings(_settings, payload.Settings) > 0)
                {
                    _settings.LastRefresh = DateTime.MinValue;
                    await SaveSettings();
                }
            }
        }

        public override async void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "ReceivedGlobalSettings");
            if (payload.Settings != null && payload.Settings.Count > 0)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedGlobalSettings: {payload.Settings}");
                var settings = payload.Settings.ToObject<GlobalPluginSettings>();
                if (settings != null && _globalSettings != null)
                {
                    var updated = false;
                    if (settings.BaseUrl != _globalSettings.BaseUrl)
                    {
                        updated = true;
                        _globalSettings.BaseUrl = settings.BaseUrl;
                        UpdateBaseUrl();
                    }

                    if (settings.Password != _globalSettings.Password)
                    {
                        updated = true;
                        _globalSettings.Password = settings.Password;
                    }

                    if (settings.UserName != _globalSettings.UserName)
                    {
                        updated = true;
                        _globalSettings.UserName = settings.UserName;
                    }

                    if (settings.Sid != _globalSettings.Sid)
                    {
                        _globalSettings.Sid = settings.Sid;
                    }

                    await SaveGlobalSettings(updated);
                }
            }
        }

        private void UpdateBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(_globalSettings.BaseUrl))
                HomeAutomationClientWrapper.Instance.BaseUrl = _globalSettings.BaseUrl;
        }

        private async Task SaveSettings()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveSettings: {JObject.FromObject(_settings)}");
            await Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }

        private async Task SaveGlobalSettings(bool triggerDidReceiveGlobalSettings = true)
        {
            if (_globalSettings != null)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveGlobalSettings: {JObject.FromObject(_globalSettings)}");
                await Connection.SetGlobalSettingsAsync(JObject.FromObject(_globalSettings), triggerDidReceiveGlobalSettings);
            }
        }
    }
}