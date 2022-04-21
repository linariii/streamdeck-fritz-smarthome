namespace FritzSmartHome.Backend.Models
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class DeviceHkrNextchange
    {
        [System.Xml.Serialization.XmlElement("endperiod")]
        public int Endperiod
        {
            get;set;
        }

        [System.Xml.Serialization.XmlElement("tchange")]
        public byte Tchange
        {
            get;set;
        }
    }
}

