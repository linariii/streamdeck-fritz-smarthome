namespace FritzSmartHome.Backend.Models
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class DeviceHkr
    {
        [System.Xml.Serialization.XmlElement("tist")]
        public byte Tist { get; set; }

        [System.Xml.Serialization.XmlElement("tsoll")]
        public byte Tsoll { get; set; }

        [System.Xml.Serialization.XmlElement("absenk")]
        public byte Absenk { get; set; }

        [System.Xml.Serialization.XmlElement("komfort")]
        public byte Komfort { get; set; }

        [System.Xml.Serialization.XmlElement("lock")]
        public byte Lock { get; set; }

        [System.Xml.Serialization.XmlElement("devicelock")]
        public byte Devicelock { get; set; }

        [System.Xml.Serialization.XmlElement("errorcode")]
        public byte Errorcode { get; set; }

        [System.Xml.Serialization.XmlElement("windowopenactiv")]
        public byte Windowopenactiv { get; set; }

        [System.Xml.Serialization.XmlElement("windowopenactiveendtime")]
        public byte Windowopenactiveendtime { get; set; }

        [System.Xml.Serialization.XmlElement("boostactive")]
        public byte Boostactive { get; set; }

        [System.Xml.Serialization.XmlElement("boostactiveendtime")]
        public byte Boostactiveendtime { get; set; }

        [System.Xml.Serialization.XmlElement("batterylow")]
        public byte Batterylow { get; set; }

        [System.Xml.Serialization.XmlElement("battery")]
        public byte Battery { get; set; }

        [System.Xml.Serialization.XmlElement("nextchange")]
        public DeviceHkrNextchange Nextchange { get; set; }

        [System.Xml.Serialization.XmlElement("summeractive")]
        public byte Summeractive { get; set; }

        [System.Xml.Serialization.XmlElement("holidayactive")]
        public byte Holidayactive { get; set; }
    }
}

