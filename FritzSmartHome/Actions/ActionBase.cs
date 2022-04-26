using System;
using System.Linq;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Fritz.HomeAutomation;
using FritzSmartHome.Backend;
using FritzSmartHome.Models;
using FritzSmartHome.Settings;
using Newtonsoft.Json.Linq;

namespace FritzSmartHome.Actions
{
    public abstract class ActionBase : PluginBase
    {
        private protected readonly Functions _deviceFilter;
        private protected readonly GlobalPluginSettings _globalSettings;
        private protected PluginSettingsBase _settings;
        private protected int _isRunning = 0;
        private protected const int DeviceFetchCooldownSec = 300;

        protected ActionBase(ISDConnection connection, InitialPayload payload, Functions deviceFilter) : base(connection, payload)
        {
            _deviceFilter = deviceFilter;
            _globalSettings = GlobalPluginSettings.CreateDefaultSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            Connection.OnSendToPlugin += Connection_OnSendToPlugin;
        }

        private async void Connection_OnSendToPlugin(object sender, BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.SendToPlugin> e)
        {
            var payload = e.Event.Payload;
            try
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, "Received Payload: " + payload);
#endif
                if (payload["property_inspector"] == null)
                    return;

                var lowerInvariant = payload["property_inspector"].ToString().ToLowerInvariant();
                if (lowerInvariant == "reloaddevices")
                {
#if DEBUG
                    Logger.Instance.LogMessage(TracingLevel.INFO, "ReloadDevices called");
#endif
                    await LoadDevices();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{GetType()} OnSendToPlugin exception: {ex}");
            }
        }

        public override void Dispose()
        {
            Connection.OnSendToPlugin -= Connection_OnSendToPlugin;
        }

        public override void KeyPressed(KeyPayload payload) { }

        public override void KeyReleased(KeyPayload payload) { }

        private protected async Task SaveGlobalSettings(bool triggerDidReceiveGlobalSettings = true)
        {
            if (_globalSettings != null)
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveGlobalSettings: {JObject.FromObject(_globalSettings)}");
#endif
                await Connection.SetGlobalSettingsAsync(JObject.FromObject(_globalSettings), triggerDidReceiveGlobalSettings);
            }
        }

        private protected async Task SaveSettings()
        {
#if DEBUG
            Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveSettings: {JObject.FromObject(_settings)}");
#endif
            await Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }

        private protected async Task Login()
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
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                await ResetSidAndShowAlert();
            }
        }

        private protected async Task ShouldLoadDevices()
        {
            if ((DateTime.Now - _settings.LastRefresh).TotalSeconds > DeviceFetchCooldownSec && !string.IsNullOrWhiteSpace(_globalSettings.Sid))
            {
                await LoadDevices();
            }
        }

        private protected async Task LoadDevices()
        {
            if (!string.IsNullOrWhiteSpace(_globalSettings.Sid))
            {
                try
                {
                    var devices = await HomeAutomationClientWrapper.Instance.GetFilteredDevices(_globalSettings.Sid, _deviceFilter);
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

        public override async void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
#if DEBUG
            Logger.Instance.LogMessage(TracingLevel.INFO, "ReceivedGlobalSettings");
#endif
            if (payload.Settings != null && payload.Settings.Count > 0)
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedGlobalSettings: {payload.Settings}");
#endif
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

        private protected async Task ResetSidAndShowAlert()
        {
            await Connection.ShowAlert();
            if (!string.IsNullOrEmpty(_globalSettings.Sid))
            {
                _globalSettings.Sid = null;
                await SaveGlobalSettings();
            }
        }

        private protected void UpdateBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(_globalSettings.BaseUrl))
                HomeAutomationClientWrapper.Instance.BaseUrl = _globalSettings.BaseUrl;
        }
    }
}