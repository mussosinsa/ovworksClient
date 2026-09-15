using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace csharpOvWorksClient_1._0._0
{
    internal class OvWorksReportedDevicesXML
    {
    }
    // using System.Xml.Serialization;
    // XmlSerializer serializer = new XmlSerializer(typeof(ReportedDevices));
    // using (StringReader reader = new StringReader(xml))
    // {
    //    var test = (ReportedDevices)serializer.Deserialize(reader);
    // }

    [XmlRoot(ElementName = "ip")]
    public class Ip
    {

        [XmlElement(ElementName = "address")]
        public string Address { get; set; }

        [XmlElement(ElementName = "version")]
        public string Version { get; set; }
    }

    [XmlRoot(ElementName = "ips")]
    public class Ips
    {

        [XmlElement(ElementName = "ip")]
        public List<Ip> Ip { get; set; }
    }

    [XmlRoot(ElementName = "mac")]
    public class Mac
    {

        [XmlElement(ElementName = "address")]
        public string Address { get; set; }
    }
    /*
    [XmlRoot(ElementName = "vm")]
    public class Vm
    {

        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }
    */
    [XmlRoot(ElementName = "reported_device")]
    public class ReportedDevice
    {

        [XmlElement(ElementName = "name")]
        public string Name { get; set; }

        [XmlElement(ElementName = "description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "ips")]
        public Ips Ips { get; set; }

        [XmlElement(ElementName = "mac")]
        public Mac Mac { get; set; }

        [XmlElement(ElementName = "type")]
        public string Type { get; set; }

        [XmlElement(ElementName = "vm")]
        public Vm Vm { get; set; }

        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "reported_devices")]
    public class ReportedDevices
    {

        [XmlElement(ElementName = "reported_device")]
        public ReportedDevice ReportedDevice { get; set; }
    }
}
