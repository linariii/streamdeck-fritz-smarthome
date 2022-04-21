using System.Collections.Generic;
namespace FritzSmartHome.Backend.Models
{
    [System.Serializable]
    [System.Xml.Serialization.XmlRoot("devicelist")]
    public partial class Devicelist
    {
        [System.Xml.Serialization.XmlElementAttribute("device")]
        public List<Device> Devices
        {
            get; set;
        }

        [System.Xml.Serialization.XmlAttributeAttribute("version")]
        public int Version
        {
            get; set;
        }

        [System.Xml.Serialization.XmlAttributeAttribute("fwversion")]
        public decimal Fwversion
        {
            get; set;
        }
    }
}
