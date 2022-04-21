namespace FritzSmartHome.Backend.Models
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class DeviceButton
    {
        [System.Xml.Serialization.XmlElement("name")]
        public string name { get; set; }

        [System.Xml.Serialization.XmlElement("lastpressedtimestamp")]
        public string Lastpressedtimestamp { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("identifier")]
        public string Identifier { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("id")]
        public int Id { get; set; }
    }
}