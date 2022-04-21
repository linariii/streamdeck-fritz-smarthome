namespace FritzSmartHome.Backend.Models
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class DevicePowermeter
    {
        [System.Xml.Serialization.XmlElement("voltage")]
        public int Voltage { get; set; }

        [System.Xml.Serialization.XmlElement("power")]
        public int Power { get; set; }

        [System.Xml.Serialization.XmlElement("energy")]
        public int Energy { get; set; }
    }
}
