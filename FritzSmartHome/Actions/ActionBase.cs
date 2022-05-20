using System;
using System.Linq;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Fritz.HomeAutomation.Enums;
using FritzSmartHome.Backend;
using FritzSmartHome.Models;
using FritzSmartHome.Settings;
using Newtonsoft.Json.Linq;

namespace FritzSmartHome.Actions
{
    public abstract class ActionBase : PluginBase
    {
        private protected readonly Functions DeviceFilter;
        private protected readonly GlobalPluginSettings GlobalSettings;
        private protected PluginSettingsBase BaseSettings;
        private protected int IsRunning = 0;
        private protected const int DeviceFetchCooldownSec = 300;
        private protected bool IsInitialized = false;

        protected ActionBase(ISDConnection connection, InitialPayload payload, Functions deviceFilter) : base(connection, payload)
        {
            DeviceFilter = deviceFilter;
            GlobalSettings = GlobalPluginSettings.CreateDefaultSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
            Connection.OnSendToPlugin += Connection_OnSendToPlugin;
        }

        protected ActionBase(ISDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            GlobalSettings = GlobalPluginSettings.CreateDefaultSettings();
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

        private protected virtual async Task SaveGlobalSettings(bool triggerDidReceiveGlobalSettings = true)
        {
            if (GlobalSettings != null)
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveGlobalSettings: {JObject.FromObject(GlobalSettings)}");
#endif
                await Connection.SetGlobalSettingsAsync(JObject.FromObject(GlobalSettings), triggerDidReceiveGlobalSettings);
            }
        }

        private protected virtual async Task SaveSettings()
        {
#if DEBUG
            Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveSettings: {JObject.FromObject(BaseSettings)}");
#endif
            await Connection.SetSettingsAsync(JObject.FromObject(BaseSettings));
        }

        private protected virtual async Task Login()
        {
            try
            {
                var sid = await HomeAutomationClientWrapper.Instance.GetSessionId(GlobalSettings.UserName, GlobalSettings.Password);
                if (!string.IsNullOrWhiteSpace(sid) && sid != "0000000000000000")
                {
                    await Connection.ShowOk();
                    GlobalSettings.Sid = sid;
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

        private protected virtual async Task ShouldLoadDevices()
        {
            if ((DateTime.Now - BaseSettings.LastRefresh).TotalSeconds > DeviceFetchCooldownSec && !string.IsNullOrWhiteSpace(GlobalSettings.Sid))
            {
                await LoadDevices();
            }
        }

        private protected virtual async Task LoadDevices()
        {
            if (!string.IsNullOrWhiteSpace(GlobalSettings.Sid))
            {
                try
                {
                    var devices = await HomeAutomationClientWrapper.Instance.GetFilteredDevices(GlobalSettings.Sid, DeviceFilter);
                    if (devices != null && devices.Any())
                    {
                        BaseSettings.Devices = devices.Select(d => new Device { Ain = d.Ain, Name = d.Name }).ToList(); ;
                    }

                    BaseSettings.LastRefresh = DateTime.Now;
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
                if (settings != null && GlobalSettings != null)
                {
                    var updated = false;
                    if (settings.BaseUrl != GlobalSettings.BaseUrl)
                    {
                        updated = true;
                        GlobalSettings.BaseUrl = settings.BaseUrl;
                        UpdateBaseUrl();
                    }

                    if (settings.Password != GlobalSettings.Password)
                    {
                        updated = true;
                        GlobalSettings.Password = settings.Password;
                    }

                    if (settings.UserName != GlobalSettings.UserName)
                    {
                        updated = true;
                        GlobalSettings.UserName = settings.UserName;
                    }

                    if (settings.Sid != GlobalSettings.Sid)
                    {
                        GlobalSettings.Sid = settings.Sid;
                    }

                    await SaveGlobalSettings(updated);
                }
            }
        }

        private protected virtual async Task ResetSidAndShowAlert()
        {
            await Connection.ShowAlert();
            if (!string.IsNullOrEmpty(GlobalSettings.Sid))
            {
                GlobalSettings.Sid = null;
                await SaveGlobalSettings();
            }
        }

        private protected void UpdateBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(GlobalSettings.BaseUrl))
                HomeAutomationClientWrapper.Instance.BaseUrl = GlobalSettings.BaseUrl;
        }
    }
}