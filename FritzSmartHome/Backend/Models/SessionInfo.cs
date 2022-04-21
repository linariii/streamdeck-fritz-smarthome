namespace FritzSmartHome.Backend.Models
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    [System.Xml.Serialization.XmlRoot(Namespace = "", IsNullable = false)]
    public partial class SessionInfo
    {
        public string SID { get; set; }

        public string Challenge { get; set; }

        public byte BlockTime { get; set; }

        public SessionInfoRights Rights { get; set; }

        public SessionInfoUsers Users { get; set; }
    }
}