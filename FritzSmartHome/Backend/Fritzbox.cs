//https://avm.de/fileadmin/user_upload/Global/Service/Schnittstellen/AHA-HTTP-Interface.pdf

using BarRaider.SdTools;
using FritzSmartHome.Backend.Extensions;
using FritzSmartHome.Backend.Models;
using System.Security.Cryptography;
using System.Text;

namespace FritzSmartHome.Backend
{
    public class Fritzbox
    {
        public string BaseUrl { get; set; }
        private static Fritzbox instance = null;
        private static readonly object objLock = new object();
        public static Fritzbox Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (objLock)
                {
                    if (instance == null)
                    {
                        instance = new Fritzbox();
                    }
                    return instance;
                }
            }
        }

        private Fritzbox(string baseUrl = "http://fritz.box/")
        {
            BaseUrl = baseUrl;
        }

        public string Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            return GetSid(username, password);
        }

        private string GetSid(string username, string password)
        {
            var url = $"{BaseUrl}login_sid.lua?username={username}";
            var result = DownloadString(url);
            if (string.IsNullOrEmpty(result))
                return null;

            var sessionInfo = result.Deserialize<SessionInfo>();
            if (sessionInfo.SID == "0000000000000000")
            {
                var response = sessionInfo.Challenge + "-" + GetMD5Hash(sessionInfo.Challenge + "-" + password);
                url = $"{BaseUrl}login_sid.lua?username={username}&response={response}";
                result = DownloadString(url);
                sessionInfo = result.Deserialize<SessionInfo>();
                if (sessionInfo == null)
                    return null;

                return sessionInfo.SID;
            }
            else
            {
                return sessionInfo.SID;
            }
        }

        public Devicelist GetDevices(string sid)
        {
            if (string.IsNullOrEmpty(sid))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "GetDevices: Sid empty");
                return null;
            }

            var url = $"{BaseUrl}webservices/homeautoswitch.lua?sid={sid}&switchcmd=getdevicelistinfos";
            Logger.Instance.LogMessage(TracingLevel.INFO, $"GetDevices > url: {url}");
            var result = DownloadString(url);
            Logger.Instance.LogMessage(TracingLevel.INFO, $"GetDevices > result: {result}");
            if (string.IsNullOrEmpty(result))
                return null;

            return result.Deserialize<Devicelist>();
        }

        public int GetSwitchState(string sid, string ain)
        {
            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(ain))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "GetSwitchState: Sid or Ain empty");
                return -1;
            }

            var url = $"{BaseUrl}webservices/homeautoswitch.lua?sid={sid}&switchcmd=getswitchstate&ain={ain}";
            var result = DownloadString(url);
            if (string.IsNullOrEmpty(result))
                return -1;

            if (!int.TryParse(result, out var switchState))
                return -1;

            return switchState;
        }

        public int SetSwitchOn(string sid, string ain)
        {
            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(ain))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "SetSwitchOn: Sid or Ain empty");
                return -1;
            }

            var url = $"{BaseUrl}webservices/homeautoswitch.lua?sid={sid}&switchcmd=setswitchon&ain={ain}";
            var result = DownloadString(url);
            if (string.IsNullOrEmpty(result))
                return -1;

            if (!int.TryParse(result, out var switchState))
                return -1;

            return switchState;
        }

        public int SetSwitchOff(string sid, string ain)
        {
            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(ain))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "SetSwitchOff: Sid or Ain empty");
                return -1;
            }

            var url = $"{BaseUrl}webservices/homeautoswitch.lua?sid={sid}&switchcmd=setswitchoff&ain={ain}";
            var result = DownloadString(url);
            if (string.IsNullOrEmpty(result))
                return -1;

            if (!int.TryParse(result, out var switchState))
                return -1;

            return switchState;
        }

        public double GetSwitchPower(string sid, string ain)
        {
            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(ain))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "GetSwitchPower: Sid or Ain empty");
                return -1;
            }

            var url = $"{BaseUrl}webservices/homeautoswitch.lua?sid={sid}&switchcmd=getswitchpower&ain={ain}";
            var result = DownloadString(url);
            if (string.IsNullOrEmpty(result))
                return -1;

            if (result == "0")
                return 0;

            if (!double.TryParse(result, out var power))
                return -1;

            return power / 1000;
        }

        public double GetTemperature(string sid, string ain)
        {
            if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(ain))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "GetTemperature: Sid or Ain empty");
                return -1;
            }

            var url = $"{BaseUrl}webservices/homeautoswitch.lua?sid={sid}&switchcmd=gettemperature&ain={ain}";
            var result = DownloadString(url);
            if (string.IsNullOrEmpty(result))
                return -1;

            if (!double.TryParse(result, out var val))
                return -1;

            return val / 10;
        }

        private string GetMD5Hash(string input)
        {
            var md5Hasher = MD5.Create();
            var data = md5Hasher.ComputeHash(Encoding.Unicode.GetBytes(input));
            var sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("x2"));
            }
            return sb.ToString();
        }

        private string DownloadString(string url)
        {
            using (var client = new System.Net.WebClient())
            {
                return client.DownloadString(url);
            }
        }
    }
}

