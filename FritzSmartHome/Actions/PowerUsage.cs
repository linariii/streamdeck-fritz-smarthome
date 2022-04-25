using BarRaider.SdTools;
using FritzSmartHome.Actions.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fritz.HomeAutomation;
using FritzSmartHome.Backend;

namespace FritzSmartHome.Actions
{
    [PluginActionId("com.linariii.powerusage")]
    public class PowerUsage : ActionBase
    {
        private const int FetchCooldownSec = 300; // 5 min
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

            [JsonProperty(PropertyName = "title")]
            public string Title { get; set; }
        }

        public PowerUsage(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Constructor called");
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                _settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
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

        public override void KeyPressed(KeyPayload payload) { }

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
                        await LoadData();
                    }
                }

            }
            finally
            {
                if (locked)
                    Interlocked.Exchange(ref _isRunning, 0);
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
                    var data = await HomeAutomationClientWrapper.Instance.GetSwitchPower(_globalSettings.Sid, _settings.Ain);
                    if (data.HasValue && data.Value >= 0)
                    {
                        var powerUsage = (double)data.Value / 1000;
                        await DrawData(Math.Round(powerUsage, 0));
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
                    var devices = await HomeAutomationClientWrapper.Instance.GetFilteredDevices(_globalSettings.Sid, Functions.EnergyMeter);
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

        private async Task DrawData(double powerUsage)
        {
            const int startingTextY = 21;
            const int currencyBufferY = 21;
            try
            {
                using (var bmp = Tools.GenerateGenericKeyImage(out Graphics graphics))
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

                    var wattStr = $"{powerUsage} W";
                    var fontSizeCurrency = graphics.GetFontSizeWhereTextFitsImage(wattStr, width, fontCurrency, 8);
                    fontCurrency = new Font(fontCurrency.Name, fontSizeCurrency, fontCurrency.Style, GraphicsUnit.Pixel);
                    stringWidth = graphics.GetTextCenter(wattStr, width, fontCurrency);
                    graphics.DrawAndMeasureString(wattStr, fontCurrency, fgBrush, new PointF(stringWidth, stringHeight));

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

        private async Task SaveSettings()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveSettings: {JObject.FromObject(_settings)}");
            await Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }
    }
}