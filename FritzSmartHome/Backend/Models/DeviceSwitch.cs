namespace FritzSmartHome.Backend.Models
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class DeviceSwitch
    {
        [System.Xml.Serialization.XmlElement("state")]
        public byte State { get; set; }

        [System.Xml.Serialization.XmlElement("mode")]
        public string Mode { get; set; }

        [System.Xml.Serialization.XmlElement("lock")]
        public byte Lock { get; set; }

        [System.Xml.Serialization.XmlElement("devicelock")]
        public byte Devicelock { get; set; }
    }

}