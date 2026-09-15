using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace csharpOvWorksClient_1._0._0
{
    internal class OvWorksvmsXML
    {
    }
    [XmlRoot(ElementName = "link")]
    public class Link
    {
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }
        [XmlAttribute(AttributeName = "rel")]
        public string Rel { get; set; }
    }

    [XmlRoot(ElementName = "actions")]
    public class Actions
    {
        [XmlElement(ElementName = "link")]
        public List<Link> Link { get; set; }
    }

    [XmlRoot(ElementName = "boot_menu")]
    public class Boot_menu
    {
        [XmlElement(ElementName = "enabled")]
        public string Enabled { get; set; }
    }

    [XmlRoot(ElementName = "bios")]
    public class Bios
    {
        [XmlElement(ElementName = "boot_menu")]
        public Boot_menu Boot_menu { get; set; }
        [XmlElement(ElementName = "type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "topology")]
    public class Topology
    {
        [XmlElement(ElementName = "cores")]
        public string Cores { get; set; }
        [XmlElement(ElementName = "sockets")]
        public string Sockets { get; set; }
        [XmlElement(ElementName = "threads")]
        public string Threads { get; set; }
    }

    [XmlRoot(ElementName = "cpu")]
    public class Cpu
    {
        [XmlElement(ElementName = "architecture")]
        public string Architecture { get; set; }
        [XmlElement(ElementName = "topology")]
        public Topology Topology { get; set; }
    }

    [XmlRoot(ElementName = "certificate")]
    public class Certificate
    {
        [XmlElement(ElementName = "content")]
        public string Content { get; set; }
        [XmlElement(ElementName = "organization")]
        public string Organization { get; set; }
        [XmlElement(ElementName = "subject")]
        public string Subject { get; set; }
    }

    [XmlRoot(ElementName = "display")]
    public class Display
    {
        [XmlElement(ElementName = "address")]
        public string Address { get; set; }
        [XmlElement(ElementName = "allow_override")]
        public string Allow_override { get; set; }
        [XmlElement(ElementName = "certificate")]
        public Certificate Certificate { get; set; }
        [XmlElement(ElementName = "copy_paste_enabled")]
        public string Copy_paste_enabled { get; set; }
        [XmlElement(ElementName = "disconnect_action")]
        public string Disconnect_action { get; set; }
        [XmlElement(ElementName = "file_transfer_enabled")]
        public string File_transfer_enabled { get; set; }
        [XmlElement(ElementName = "monitors")]
        public string Monitors { get; set; }
        [XmlElement(ElementName = "port")]
        public string Port { get; set; }
        [XmlElement(ElementName = "secure_port")]
        public string Secure_port { get; set; }
        [XmlElement(ElementName = "smartcard_enabled")]
        public string Smartcard_enabled { get; set; }
        [XmlElement(ElementName = "type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "high_availability")]
    public class High_availability
    {
        [XmlElement(ElementName = "enabled")]
        public string Enabled { get; set; }
        [XmlElement(ElementName = "priority")]
        public string Priority { get; set; }
    }

    [XmlRoot(ElementName = "io")]
    public class Io
    {
        [XmlElement(ElementName = "threads")]
        public string Threads { get; set; }
    }

    [XmlRoot(ElementName = "large_icon")]
    public class Large_icon
    {
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "memory_policy")]
    public class Memory_policy
    {
        [XmlElement(ElementName = "ballooning")]
        public string Ballooning { get; set; }
        [XmlElement(ElementName = "guaranteed")]
        public string Guaranteed { get; set; }
        [XmlElement(ElementName = "max")]
        public string Max { get; set; }
    }

    [XmlRoot(ElementName = "migration")]
    public class Migration
    {
        [XmlElement(ElementName = "auto_converge")]
        public string Auto_converge { get; set; }
        [XmlElement(ElementName = "compressed")]
        public string Compressed { get; set; }
        [XmlElement(ElementName = "encrypted")]
        public string Encrypted { get; set; }
    }

    [XmlRoot(ElementName = "devices")]
    public class Devices
    {
        [XmlElement(ElementName = "device")]
        public List<string> Device { get; set; }
    }

    [XmlRoot(ElementName = "boot")]
    public class Boot
    {
        [XmlElement(ElementName = "devices")]
        public Devices Devices { get; set; }
    }

    [XmlRoot(ElementName = "os")]
    public class Os
    {
        [XmlElement(ElementName = "boot")]
        public Boot Boot { get; set; }
        [XmlElement(ElementName = "type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "small_icon")]
    public class Small_icon
    {
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "method")]
    public class Method
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "methods")]
    public class Methods
    {
        [XmlElement(ElementName = "method")]
        public Method Method { get; set; }
    }

    [XmlRoot(ElementName = "sso")]
    public class Sso
    {
        [XmlElement(ElementName = "methods")]
        public Methods Methods { get; set; }
    }

    [XmlRoot(ElementName = "time_zone")]
    public class Time_zone
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
    }

    [XmlRoot(ElementName = "usb")]
    public class Usb
    {
        [XmlElement(ElementName = "enabled")]
        public string Enabled { get; set; }
    }

    [XmlRoot(ElementName = "cluster")]
    public class Cluster
    {
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "cpu_profile")]
    public class Cpu_profile
    {
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "quota")]
    public class Quota
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "version")]
    public class Version
    {
        [XmlElement(ElementName = "build")]
        public string Build { get; set; }
        [XmlElement(ElementName = "full_version")]
        public string Full_version { get; set; }
        [XmlElement(ElementName = "major")]
        public string Major { get; set; }
        [XmlElement(ElementName = "minor")]
        public string Minor { get; set; }
        [XmlElement(ElementName = "revision")]
        public string Revision { get; set; }
    }

    [XmlRoot(ElementName = "kernel")]
    public class Kernel
    {
        [XmlElement(ElementName = "version")]
        public Version Version { get; set; }
    }

    [XmlRoot(ElementName = "guest_operating_system")]
    public class Guest_operating_system
    {
        [XmlElement(ElementName = "architecture")]
        public string Architecture { get; set; }
        [XmlElement(ElementName = "codename")]
        public string Codename { get; set; }
        [XmlElement(ElementName = "distribution")]
        public string Distribution { get; set; }
        [XmlElement(ElementName = "family")]
        public string Family { get; set; }
        [XmlElement(ElementName = "kernel")]
        public Kernel Kernel { get; set; }
        [XmlElement(ElementName = "version")]
        public Version Version { get; set; }
    }

    [XmlRoot(ElementName = "guest_time_zone")]
    public class Guest_time_zone
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "utc_offset")]
        public string Utc_offset { get; set; }
    }

    [XmlRoot(ElementName = "original_template")]
    public class Original_template
    {
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "template")]
    public class Template
    {
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "vm")]
    public class Vm
    {
        [XmlElement(ElementName = "actions")]
        public Actions Actions { get; set; }
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "description")]
        public string Description { get; set; }
        [XmlElement(ElementName = "comment")]
        public string Comment { get; set; }
        [XmlElement(ElementName = "link")]
        public List<Link> Link { get; set; }
        [XmlElement(ElementName = "bios")]
        public Bios Bios { get; set; }
        [XmlElement(ElementName = "cpu")]
        public Cpu Cpu { get; set; }
        [XmlElement(ElementName = "cpu_shares")]
        public string Cpu_shares { get; set; }
        [XmlElement(ElementName = "creation_time")]
        public string Creation_time { get; set; }
        [XmlElement(ElementName = "delete_protected")]
        public string Delete_protected { get; set; }
        [XmlElement(ElementName = "display")]
        public Display Display { get; set; }
        [XmlElement(ElementName = "high_availability")]
        public High_availability High_availability { get; set; }
        [XmlElement(ElementName = "io")]
        public Io Io { get; set; }
        [XmlElement(ElementName = "large_icon")]
        public Large_icon Large_icon { get; set; }
        [XmlElement(ElementName = "memory")]
        public string Memory { get; set; }
        [XmlElement(ElementName = "memory_policy")]
        public Memory_policy Memory_policy { get; set; }
        [XmlElement(ElementName = "migration")]
        public Migration Migration { get; set; }
        [XmlElement(ElementName = "migration_downtime")]
        public string Migration_downtime { get; set; }
        [XmlElement(ElementName = "multi_queues_enabled")]
        public string Multi_queues_enabled { get; set; }
        [XmlElement(ElementName = "origin")]
        public string Origin { get; set; }
        [XmlElement(ElementName = "os")]
        public Os Os { get; set; }
        [XmlElement(ElementName = "small_icon")]
        public Small_icon Small_icon { get; set; }
        [XmlElement(ElementName = "sso")]
        public Sso Sso { get; set; }
        [XmlElement(ElementName = "start_paused")]
        public string Start_paused { get; set; }
        [XmlElement(ElementName = "stateless")]
        public string Stateless { get; set; }
        [XmlElement(ElementName = "storage_error_resume_behaviour")]
        public string Storage_error_resume_behaviour { get; set; }
        [XmlElement(ElementName = "time_zone")]
        public Time_zone Time_zone { get; set; }
        [XmlElement(ElementName = "type")]
        public string Type { get; set; }
        [XmlElement(ElementName = "usb")]
        public Usb Usb { get; set; }
        [XmlElement(ElementName = "virtio_scsi_multi_queues_enabled")]
        public string Virtio_scsi_multi_queues_enabled { get; set; }
        [XmlElement(ElementName = "cluster")]
        public Cluster Cluster { get; set; }
        [XmlElement(ElementName = "cpu_profile")]
        public Cpu_profile Cpu_profile { get; set; }
        [XmlElement(ElementName = "quota")]
        public Quota Quota { get; set; }
        [XmlElement(ElementName = "fqdn")]
        public string Fqdn { get; set; }
        [XmlElement(ElementName = "guest_operating_system")]
        public Guest_operating_system Guest_operating_system { get; set; }
        [XmlElement(ElementName = "guest_time_zone")]
        public Guest_time_zone Guest_time_zone { get; set; }
        [XmlElement(ElementName = "next_run_configuration_exists")]
        public string Next_run_configuration_exists { get; set; }
        [XmlElement(ElementName = "run_once")]
        public string Run_once { get; set; }
        [XmlElement(ElementName = "start_time")]
        public string Start_time { get; set; }
        [XmlElement(ElementName = "status")]
        public string Status { get; set; }
        [XmlElement(ElementName = "stop_time")]
        public string Stop_time { get; set; }
        [XmlElement(ElementName = "original_template")]
        public Original_template Original_template { get; set; }
        [XmlElement(ElementName = "template")]
        public Template Template { get; set; }
        [XmlAttribute(AttributeName = "href")]
        public string Href { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }

    [XmlRoot(ElementName = "vms")]
    public class Vms
    {
        [XmlElement(ElementName = "vm")]
        public List<Vm> Vm { get; set; }
    }
}
