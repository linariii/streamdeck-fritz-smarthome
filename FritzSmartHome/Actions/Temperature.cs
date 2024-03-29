﻿using BarRaider.SdTools;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fritz.HomeAutomation.Enums;
using FritzSmartHome.Backend;
using FritzSmartHome.Settings;

namespace FritzSmartHome.Actions
{
    [PluginActionId("com.linariii.temperature")]
    public class Temperature : ActionBase
    {
        private const int DataFetchCooldownSec = 300;

        public Temperature(SDConnection connection, InitialPayload payload) : base(connection, payload, Functions.TemperatureSensor)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                Settings = TemperaturePluginSettings.CreateDefaultSettings();
            }
            else
            {
#if DEBUG
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Settings: {payload.Settings}");
#endif
                Settings = payload.Settings.ToObject<TemperaturePluginSettings>();
            }

            GlobalSettingsManager.Instance.RequestGlobalSettings();
            UpdateBaseUrl();
        }

        protected TemperaturePluginSettings Settings
        {
            get
            {
                var settings = BaseSettings as TemperaturePluginSettings;
                if (settings == null)
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to PluginSettings");
                return settings;
            }
            set => BaseSettings = value;
        }

        public override async void OnTick()
        {
            if (GlobalSettings == null || Settings == null)
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
                        await LoadData();
                    }

                    if (!IsInitialized && Settings.Data.HasValue)
                    {
                        await DrawData(Settings.Data.Value);
                    }
                }
            }
            finally
            {
                if (locked)
                    Interlocked.Exchange(ref IsRunning, 0);
            }
        }

        private async Task LoadData()
        {
            if ((DateTime.Now - Settings.LastRefresh).TotalSeconds > DataFetchCooldownSec
                && !string.IsNullOrWhiteSpace(GlobalSettings.Sid)
                && !string.IsNullOrWhiteSpace(Settings.Ain))
            {
                try
                {
                    var data = await HomeAutomationClientWrapper.Instance.GetTemperature(GlobalSettings.Sid, Settings.Ain);
                    if (data.HasValue)
                    {
                        var powerUsage = (double)data.Value / 10;
                        var value = Math.Round(powerUsage, 0);
                        Settings.Data = value;
                        await DrawData(value);
                        await SaveSettings();
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

        private async Task DrawData(double temperature)
        {
            IsInitialized = true;
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

                    var wattStr = $"{temperature} °C";
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
                        Settings.Title = Settings.Devices.FirstOrDefault(d => d.Ain == Settings.Ain)?.Name;
                    }
                    Settings.LastRefresh = DateTime.MinValue;
                    await SaveSettings();
                }
            }
        }
    }
}