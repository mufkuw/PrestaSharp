using System.Xml.Serialization;

namespace PrestaSharp.Entities
{
    [XmlType(Namespace = "Bukimedia/PrestaSharp/Entities")]
    public class price_range : PrestaShopEntity, IPrestaShopFactoryEntity
    {
        public long? id { get; set; }
        public long? id_carrier { get; set; }
        public float delimiter1 { get; set; }
        public float delimiter2 { get; set; }
        public price_range() { }
    }
}
