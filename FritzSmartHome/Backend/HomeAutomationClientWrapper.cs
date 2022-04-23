using Fritz.HomeAutomation;

namespace FritzSmartHome.Backend
{
    public static class HomeAutomationClientWrapper
    {
        private static HomeAutomationClient _instance;
        private static readonly object LockObject = new object();

        public static HomeAutomationClient Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                lock (LockObject)
                {
                    return _instance ?? (_instance = new HomeAutomationClient());
                }
            }
        }
    }
}