using BarRaider.SdTools;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fritz.HomeAutomation;
using FritzSmartHome.Backend;
using FritzSmartHome.Settings;

namespace FritzSmartHome.Actions
{
    [PluginActionId("com.linariii.powerusage")]
    public class PowerUsage : ActionBase
    {
        private const int DataFetchCooldownSec = 300;

        public PowerUsage(SDConnection connection, InitialPayload payload) : base(connection, payload, Functions.EnergyMeter)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = PowerUsagePluginSettings.CreateDefaultSettings();
            }
            else
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
#endif
                Settings = payload.Settings.ToObject<PowerUsagePluginSettings>();
                if (Settings != null)
                    Settings.LastRefresh = DateTime.MinValue;
            }

            GlobalSettingsManager.Instance.RequestGlobalSettings();
            UpdateBaseUrl();
        }

        protected PowerUsagePluginSettings Settings
        {
            get
            {
                var settings = _settings as PowerUsagePluginSettings;
                if (settings == null)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to PluginSettings");
                return settings;
            }
            set => _settings = value;
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
            if ((DateTime.Now - _settings.LastRefresh).TotalSeconds > DataFetchCooldownSec
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
                    var fontSizeDefault = graphics.GetFontSizeWhereTextFitsImage(Settings.Title, width, fontDefault, 8);
                    fontDefault = new Font(fontDefault.Name, fontSizeDefault, fontDefault.Style, GraphicsUnit.Pixel);
                    var stringWidth = graphics.GetTextCenter(Settings.Title, width, fontDefault);

                    stringHeight = graphics.DrawAndMeasureString(Settings.Title, fontDefault, fgBrush, new PointF(stringWidth, stringHeight)) + currencyBufferY;

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
                    if (!string.IsNullOrWhiteSpace(Settings.Ain))
                    {
                        Settings.Title = Settings.Devices.FirstOrDefault(d => d.Ain == _settings.Ain)?.Name;
                    }
                    Settings.LastRefresh = DateTime.MinValue;
                    await SaveSettings();
                }
            }
        }
    }
}