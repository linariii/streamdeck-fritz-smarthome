using System;
using System.Threading.Tasks;
using BarRaider.SdTools;
using FritzSmartHome.Backend;
using Newtonsoft.Json.Linq;

namespace FritzSmartHome.Actions
{
    public abstract class ActionBase : PluginBase
    {
        private protected readonly GlobalPluginSettings _globalSettings;
        private protected int _isRunning = 0;
        protected ActionBase(ISDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            _globalSettings = GlobalPluginSettings.CreateDefaultSettings();
            GlobalSettingsManager.Instance.RequestGlobalSettings();
        }

        public override void Dispose() { }

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