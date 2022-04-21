namespace FritzSmartHome.Backend.Models
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class SessionInfoRights
    {
        public string Name { get; set; }

        public int Access { get; set; }
    }
}