using BarRaider.SdTools;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fritz.HomeAutomation.Enums;
using FritzSmartHome.Backend;
using FritzSmartHome.Settings;

namespace FritzSmartHome.Actions
{
    [PluginActionId("com.linariii.outlet")]
    public class Outlet : ActionBase
    {
        private const int StateFetchCooldownSec = 60; // 1 min

        public Outlet(SDConnection connection, InitialPayload payload) : base(connection, payload, Functions.Outlet)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = OutletPluginSettings.CreateDefaultSettings();
            }
            else
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
#endif
                Settings = payload.Settings.ToObject<OutletPluginSettings>();
            }

            GlobalSettingsManager.Instance.RequestGlobalSettings();
            UpdateBaseUrl();
        }

        protected OutletPluginSettings Settings
        {
            get
            {
                var settings = BaseSettings as OutletPluginSettings;
                if (settings == null)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to PluginSettings");
                return settings;
            }
            set => BaseSettings = value;
        }

        public override async void KeyPressed(KeyPayload payload)
        {
            if (GlobalSettings == null || BaseSettings == null)
                return;
            try
            {
                if (!string.IsNullOrWhiteSpace(GlobalSettings.Sid) && !string.IsNullOrWhiteSpace(BaseSettings.Ain))
                {
                    var state = await HomeAutomationClientWrapper.Instance.SetSwitchToggle(GlobalSettings.Sid, BaseSettings.Ain);
                    if (state.HasValue)
                    {
                        var value = (uint)state.Value;
                        if (Settings.State != value)
                        {
                            Settings.State = value;
                            await Connection.SetStateAsync(value);
                        }
                    }
                    BaseSettings.LastRefresh = DateTime.Now;
                    await SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                await ResetSidAndShowAlert();
            }
        }

        public override async void OnTick()
        {
            if (GlobalSettings == null || BaseSettings == null)
                return;

            if (IsRunning > 0)
                return;

            var locked = false;
            try
            {
                try { }
                finally
                {
                    locked = Interlocked.CompareExchange(ref IsRunning, 1, 0) == 0;
                }

                if (locked)
                {
                    if (string.IsNullOrWhiteSpace(GlobalSettings.Sid)
                        && !string.IsNullOrWhiteSpace(GlobalSettings.UserName)
                        && !string.IsNullOrWhiteSpace(GlobalSettings.Password)
                        && !string.IsNullOrWhiteSpace(GlobalSettings.BaseUrl))
                    {
                        await Login();
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(GlobalSettings.Sid) && (BaseSettings.Devices == null || !BaseSettings.Devices.Any()))
                    {
                        await ShouldLoadDevices();
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(GlobalSettings.Sid) && !string.IsNullOrWhiteSpace(BaseSettings.Ain))
                    {
                        await LoadState();
                    }

                    if (!IsInitialized && Settings.State.HasValue)
                    {
                        IsInitialized = true;
                        await Connection.SetStateAsync((uint)Settings.State.Value);
                    }
                }
            }
            finally
            {
                if (locked)
                    Interlocked.Exchange(ref IsRunning, 0);
            }
        }

        private async Task LoadState()
        {
            if ((DateTime.Now - BaseSettings.LastRefresh).TotalSeconds > StateFetchCooldownSec
                && !string.IsNullOrWhiteSpace(GlobalSettings.Sid)
                && !string.IsNullOrWhiteSpace(BaseSettings.Ain))
            {
                try
                {
                    var data = await HomeAutomationClientWrapper.Instance.GetSwitchState(GlobalSettings.Sid, BaseSettings.Ain);
                    if (data.HasValue)
                    {
                        var value = (uint)data.Value;
                        if (Settings.State != value)
                        {
                            IsInitialized = true;
                            Settings.State = value;
                            await Connection.SetStateAsync(value);
                        }
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
                if (Tools.AutoPopulateSettings(Settings, payload.Settings) > 0)
                {
                    Settings.LastRefresh = DateTime.MinValue;
                    await SaveSettings();
                }
            }
        }
    }
}