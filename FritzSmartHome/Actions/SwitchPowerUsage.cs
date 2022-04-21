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
        private const int FETCH_COOLDOWN_SEC = 300; // 5 min
        private readonly string[] SupportedDevices = { "FRITZ!DECT 200", "FRITZ!DECT 201" };
        private PluginSettings settings;
        private GlobalPluginSettings globalSettings;

        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings()
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
            if (payload.Settings == null || payload.Settings.Count == 0) // Called the first time you drop a new action into the Stream Deck
            {
                globalSettings = GlobalPluginSettings.CreateDefaultSettings();
                settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
                globalSettings = GlobalPluginSettings.CreateDefaultSettings();
                settings = payload.Settings.ToObject<PluginSettings>();
                settings.LastRefresh = DateTime.MinValue;
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

        public async override void OnTick()
        {
            if (globalSettings == null || settings == null)
                return;

            if (string.IsNullOrWhiteSpace(globalSettings.Sid)
                && !string.IsNullOrWhiteSpace(globalSettings.UserName)
                && !string.IsNullOrWhiteSpace(globalSettings.Password)
                && !string.IsNullOrWhiteSpace(globalSettings.BaseUrl))
            {
                await Login();
                return;
            }

            if (!string.IsNullOrWhiteSpace(globalSettings.Sid) && (settings.Devices == null || !settings.Devices.Any()))
            {
                await LoadDevices();
                return;
            }

            if (!string.IsNullOrWhiteSpace(globalSettings.Sid) && !string.IsNullOrWhiteSpace(settings.Ain))
            {
                await LoadData();
            }
        }

        private async Task Login()
        {
            try
            {
                var sid = Fritzbox.Instance.Login(globalSettings.UserName, globalSettings.Password);
                if (!string.IsNullOrWhiteSpace(sid) && sid != "0000000000000000")
                {
                    await Connection.ShowOk();
                    globalSettings.Sid = sid;
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
                if (!string.IsNullOrEmpty(globalSettings.Sid))
                {
                    globalSettings.Sid = null;
                    await SaveGlobalSettings();
                }
            }
        }

        private async Task LoadData()
        {
            if ((DateTime.Now - settings.LastRefresh).TotalSeconds > FETCH_COOLDOWN_SEC
                && !string.IsNullOrWhiteSpace(globalSettings.Sid)
                && !string.IsNullOrWhiteSpace(settings.Ain))
            {
                try
                {
                    var data = Fritzbox.Instance.GetSwitchPower(globalSettings.Sid, settings.Ain);
                    if (data >= 0)
                    {
                        await DrawData(data);
                    }
                    settings.LastRefresh = DateTime.Now;
                    await SaveSettings();
                }
                catch (Exception ex)
                {
                    await Connection.ShowAlert();
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                    if (!string.IsNullOrEmpty(globalSettings.Sid))
                    {
                        globalSettings.Sid = null;
                        await SaveGlobalSettings();
                    }
                }
            }
        }

        private async Task LoadDevices()
        {
            if ((DateTime.Now - settings.LastRefresh).TotalSeconds > FETCH_COOLDOWN_SEC && !string.IsNullOrWhiteSpace(globalSettings.Sid))
            {
                try
                {
                    var deviseList = Fritzbox.Instance.GetDevices(globalSettings.Sid);
                    if (deviseList != null && deviseList.Devices != null)
                    {
                        var devices = deviseList.Devices.Where(d => d.Present == 1 && SupportedDevices.Contains(d.Productname))
                        .Select(d => new Device { Ain = d.Identifier, Name = d.Name }).ToList();

                        settings.Devices = devices;
                    }

                    settings.LastRefresh = DateTime.Now;
                    await SaveSettings();
                }
                catch (Exception ex)
                {
                    await Connection.ShowAlert();
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error loading data: {ex}");
                    if (!string.IsNullOrEmpty(globalSettings.Sid))
                    {                        
                        globalSettings.Sid = null;
                        await SaveGlobalSettings();
                    }
                }
            }
        }

        private async Task DrawData(double data)
        {
            const int STARTING_TEXT_Y = 21;
            const int CURRENCY_BUFFER_Y = 21;
            try
            {
                using (Bitmap bmp = Tools.GenerateGenericKeyImage(out Graphics graphics))
                {
                    int height = bmp.Height;
                    int width = bmp.Width;

                    var fontDefault = new Font("Verdana", 20, FontStyle.Bold, GraphicsUnit.Pixel);
                    var fontCurrency = new Font("Verdana", 32, FontStyle.Bold, GraphicsUnit.Pixel);

                    // Background
                    var bgBrush = new SolidBrush(Color.Black);
                    graphics.FillRectangle(bgBrush, 0, 0, width, height);
                    var fgBrush = new SolidBrush(Color.White);

                    // Top title
                    float stringHeight = STARTING_TEXT_Y;
                    float fontSizeDefault = graphics.GetFontSizeWhereTextFitsImage(settings.Title, width, fontDefault, 8);
                    fontDefault = new Font(fontDefault.Name, fontSizeDefault, fontDefault.Style, GraphicsUnit.Pixel);
                    float stringWidth = graphics.GetTextCenter(settings.Title, width, fontDefault);

                    stringHeight = graphics.DrawAndMeasureString(settings.Title, fontDefault, fgBrush, new PointF(stringWidth, stringHeight)) + CURRENCY_BUFFER_Y;

                    string currStr = $"{data} W";
                    float fontSizeCurrency = graphics.GetFontSizeWhereTextFitsImage(currStr, width, fontCurrency, 8);
                    fontCurrency = new Font(fontCurrency.Name, fontSizeCurrency, fontCurrency.Style, GraphicsUnit.Pixel);
                    stringWidth = graphics.GetTextCenter(currStr, width, fontCurrency);
                    stringHeight = graphics.DrawAndMeasureString(currStr, fontCurrency, fgBrush, new PointF(stringWidth, stringHeight));

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

        public async override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedSettings");
            if (payload.Settings != null && payload.Settings.Count > 0)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedSettings: {payload.Settings}");
                if (Tools.AutoPopulateSettings(settings, payload.Settings) > 0)
                {
                    if (!string.IsNullOrWhiteSpace(settings.Ain))
                    {
                        settings.Title = settings.Devices.FirstOrDefault(d => d.Ain == settings.Ain)?.Name;
                    }
                    settings.LastRefresh = DateTime.MinValue;
                    await SaveSettings();
                }
            }
        }

        public async override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedGlobalSettings");
            if (payload.Settings != null && payload.Settings.Count > 0)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"ReceivedGlobalSettings: {payload.Settings}");
                var settings = payload.Settings.ToObject<GlobalPluginSettings>();
                if (settings != null && globalSettings != null)
                {
                    var updated = false;
                    if (settings.BaseUrl != globalSettings.BaseUrl)
                    {
                        updated = true;
                        globalSettings.BaseUrl = settings.BaseUrl;
                        UpdateFritzboxBaseUrl();
                    }

                    if (settings.Password != globalSettings.Password)
                    {
                        updated = true;
                        globalSettings.Password = settings.Password;
                    }

                    if (settings.UserName != globalSettings.UserName)
                    {
                        updated = true;
                        globalSettings.UserName = settings.UserName;
                    }

                    if (settings.Sid != globalSettings.Sid)
                    {
                        globalSettings.Sid = settings.Sid;
                    }

                    await SaveGlobalSettings(updated);
                }
            }
        }

        private void UpdateFritzboxBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(globalSettings.BaseUrl))
                Fritzbox.Instance.BaseUrl = globalSettings.BaseUrl;
        }

        private async Task SaveSettings()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveSettings: {JObject.FromObject(settings)}");
            await Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private async Task SaveGlobalSettings(bool triggerDidReceiveGlobalSettings = true)
        {
            if (globalSettings != null)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"SaveGlobalSettings: {JObject.FromObject(globalSettings)}");
                await Connection.SetGlobalSettingsAsync(JObject.FromObject(globalSettings), triggerDidReceiveGlobalSettings);
            }
            return;
        }
    }
}