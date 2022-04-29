using BarRaider.SdTools;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fritz.HomeAutomation;
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
                var settings = _settings as OutletPluginSettings;
                if (settings == null)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to PluginSettings");
                return settings;
            }
            set => _settings = value;
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
                        if (Settings.State != state.Value)
                        {
                            Settings.State = state.Value;
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
                        await ShouldLoadDevices();
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
            if ((DateTime.Now - _settings.LastRefresh).TotalSeconds > StateFetchCooldownSec
                && !string.IsNullOrWhiteSpace(_globalSettings.Sid)
                && !string.IsNullOrWhiteSpace(_settings.Ain))
            {
                try
                {
                    var data = await HomeAutomationClientWrapper.Instance.GetSwitchState(_globalSettings.Sid, _settings.Ain);
                    if (data.HasValue)
                    {
                        if (Settings.State != data.Value)
                        {
                            Settings.State = data.Value;
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