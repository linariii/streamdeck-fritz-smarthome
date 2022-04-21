namespace FritzSmartHome.Backend.Models
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class DeviceTemperature
    {
        [System.Xml.Serialization.XmlElement("celsius")]
        public byte Celsius { get; set; }

        [System.Xml.Serialization.XmlElement("offset")]
        public byte Offset { get; set; }
    }
}