using BarRaider.SdTools;
using FritzSmartHome.Actions.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fritz.HomeAutomation;
using FritzSmartHome.Backend;

namespace FritzSmartHome.Actions
{
    [PluginActionId("com.linariii.outlet")]
    public class Outlet : ActionBase
    {
        private const int FetchCooldownSec = 60; // 1 min
        private readonly PluginSettings _settings;

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
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                _settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
#endif
                _settings = payload.Settings.ToObject<PluginSettings>();
                if (_settings != null)
                    _settings.LastRefresh = DateTime.MinValue;
            }

            GlobalSettingsManager.Instance.RequestGlobalSettings();
            UpdateBaseUrl();
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
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                await ResetSidAndShowAlert();
            }
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override async void OnTick()
        {
            if (_globalSettings == null || _settings == null)
                return;

            if (_isRunning > 0)
                return;

            var locked = false;
            try
            {
                try { }
                finally
                {
                    locked = Interlocked.CompareExchange(ref _isRunning, 1, 0) == 0;
                }

                if (locked)
                {
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
            }
            finally
            {
                if (locked)
                    Interlocked.Exchange(ref _isRunning, 0);
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
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                    await ResetSidAndShowAlert();
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
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                    await ResetSidAndShowAlert();
                }
            }
        }

        public override async void ReceivedSettings(ReceivedSettingsPayload payload)
        {
#if DEBUG
            Logger.Instance.LogMessage(TracingLevel.INFO, "ReceivedSettings");
#endif
            if (payload.Settings != null && payload.Settings.Count > 0)
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedSettings: {payload.Settings}");
#endif
                if (Tools.AutoPopulateSettings(_settings, payload.Settings) > 0)
                {
                    _settings.LastRefresh = DateTime.MinValue;
                    await SaveSettings();
                }
            }
        }

        private async Task SaveSettings()
        {
#if DEBUG
            Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveSettings: {JObject.FromObject(_settings)}");
#endif
            await Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }
    }
}