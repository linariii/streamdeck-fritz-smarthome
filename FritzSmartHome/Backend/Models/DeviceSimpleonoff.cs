namespace FritzSmartHome.Backend.Models
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class DeviceSimpleonoff
    {
        [System.Xml.Serialization.XmlElement("state")]
        public byte State { get; set; }
    }
}
