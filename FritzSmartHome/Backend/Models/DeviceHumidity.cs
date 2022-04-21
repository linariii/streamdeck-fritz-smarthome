namespace FritzSmartHome.Backend.Models
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class DeviceHumidity
    {
        [System.Xml.Serialization.XmlElement("rel_humidity")]
        public byte Rel_humidity { get; set; }
    }
}
