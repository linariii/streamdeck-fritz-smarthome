using System.Collections.Generic;

namespace FritzSmartHome.Backend.Models
{
    [System.Serializable]
    [System.Xml.Serialization.XmlType(AnonymousType = true)]
    public partial class Device
    {
        [System.Xml.Serialization.XmlElement("present")]
        public byte Present { get; set; }

        [System.Xml.Serialization.XmlElement("txbusy")]
        public byte Txbusy { get; set; }

        [System.Xml.Serialization.XmlElement("name")]
        public string Name { get; set; }

        [System.Xml.Serialization.XmlElement("battery")]
        public int Battery { get; set; }

        [System.Xml.Serialization.XmlElement("batterylow")]
        public byte Batterylow { get; set; }

        [System.Xml.Serialization.XmlElement("switch")]
        public DeviceSwitch Switch { get; set; }

        [System.Xml.Serialization.XmlElement("simpleonoff")]
        public DeviceSimpleonoff Simpleonoff { get; set; }

        [System.Xml.Serialization.XmlElement("powermeter")]
        public DevicePowermeter Powermeter { get; set; }

        [System.Xml.Serialization.XmlElement("temperature")]
        public DeviceTemperature Temperature { get; set; }

        [System.Xml.Serialization.XmlElement("humidity")]
        public DeviceHumidity Humidity { get; set; }

        [System.Xml.Serialization.XmlElement("button")]
        public List<DeviceButton> Buttons { get; set; }

        [System.Xml.Serialization.XmlElement("device")]
        public DeviceHkr Hkr { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("identifier")]
        public string Identifier { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("id")]
        public int Id { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("functionbitmask")]
        public string Functionbitmask { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("fwversion")]
        public decimal Fwversion { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute("manufacturer")]
        public string Manufacturer { get; set; }

        [System.Xml.Serialization.XmlAttribute("productname")]
        public string Productname { get; set; }
    }
}