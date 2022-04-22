using BarRaider.SdTools;
using FritzSmartHome.Actions.Models;
using FritzSmartHome.Backend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace FritzSmartHome.Actions
{
    [PluginActionId("com.linariii.switchpowerusage")]
    public partial class SwitchPowerUsage : PluginBase
    {
        private const int FetchCooldownSec = 300; // 5 min
        private readonly string[] _supportedDevices = { "FRITZ!DECT 200", "FRITZ!DECT 210" };
        private PluginSettings _settings;
        private GlobalPluginSettings _globalSettings;

        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                var instance = new PluginSettings()
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

            [JsonProperty(PropertyName = "title")]
            public string Title { get; set; }
        }

        public SwitchPowerUsage(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Constructor called");
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
            UpdateFritzboxBaseUrl();
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public override void KeyPressed(KeyPayload payload) { }

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
                await LoadData();
            }
        }

        private async Task Login()
        {
            try
            {
                var sid = Fritzbox.Instance.Login(_globalSettings.UserName, _globalSettings.Password);
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

        private async Task LoadData()
        {
            if ((DateTime.Now - _settings.LastRefresh).TotalSeconds > FetchCooldownSec
                && !string.IsNullOrWhiteSpace(_globalSettings.Sid)
                && !string.IsNullOrWhiteSpace(_settings.Ain))
            {
                try
                {
                    var data = Fritzbox.Instance.GetSwitchPower(_globalSettings.Sid, _settings.Ain);
                    if (data >= 0)
                    {
                        await DrawData(data);
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
                    var deviseList = Fritzbox.Instance.GetDevices(_globalSettings.Sid);
                    if (deviseList != null && deviseList.Devices != null)
                    {
                        var devices = deviseList.Devices.Where(d => d.Present == 1 && _supportedDevices.Contains(d.Productname))
                        .Select(d => new Device { Ain = d.Identifier, Name = d.Name }).ToList();

                        _settings.Devices = devices;
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

        private async Task DrawData(double data)
        {
            const int startingTextY = 21;
            const int currencyBufferY = 21;
            try
            {
                using (Bitmap bmp = Tools.GenerateGenericKeyImage(out Graphics graphics))
                {
                    var height = bmp.Height;
                    var width = bmp.Width;

                    var fontDefault = new Font("Verdana", 20, FontStyle.Bold, GraphicsUnit.Pixel);
                    var fontCurrency = new Font("Verdana", 32, FontStyle.Bold, GraphicsUnit.Pixel);

                    // Background
                    var bgBrush = new SolidBrush(Color.Black);
                    graphics.FillRectangle(bgBrush, 0, 0, width, height);
                    var fgBrush = new SolidBrush(Color.White);

                    // Top title
                    float stringHeight = startingTextY;
                    var fontSizeDefault = graphics.GetFontSizeWhereTextFitsImage(_settings.Title, width, fontDefault, 8);
                    fontDefault = new Font(fontDefault.Name, fontSizeDefault, fontDefault.Style, GraphicsUnit.Pixel);
                    var stringWidth = graphics.GetTextCenter(_settings.Title, width, fontDefault);

                    stringHeight = graphics.DrawAndMeasureString(_settings.Title, fontDefault, fgBrush, new PointF(stringWidth, stringHeight)) + currencyBufferY;

                    var currStr = $"{data} W";
                    var fontSizeCurrency = graphics.GetFontSizeWhereTextFitsImage(currStr, width, fontCurrency, 8);
                    fontCurrency = new Font(fontCurrency.Name, fontSizeCurrency, fontCurrency.Style, GraphicsUnit.Pixel);
                    stringWidth = graphics.GetTextCenter(currStr, width, fontCurrency);
                    graphics.DrawAndMeasureString(currStr, fontCurrency, fgBrush, new PointF(stringWidth, stringHeight));

                    await Connection.SetImageAsync(bmp);
                    graphics.Dispose();
                    fontDefault.Dispose();
                    fontCurrency.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{GetType()} Error drawing data {ex}");
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
                    if (!string.IsNullOrWhiteSpace(_settings.Ain))
                    {
                        _settings.Title = _settings.Devices.FirstOrDefault(d => d.Ain == _settings.Ain)?.Name;
                    }
                    _settings.LastRefresh = DateTime.MinValue;
                    await SaveSettings();
                }
            }
        }

        public override async void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedGlobalSettings");
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
                        UpdateFritzboxBaseUrl();
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

        private void UpdateFritzboxBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(_globalSettings.BaseUrl))
                Fritzbox.Instance.BaseUrl = _globalSettings.BaseUrl;
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