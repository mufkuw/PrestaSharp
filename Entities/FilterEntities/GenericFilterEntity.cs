﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PrestaSharp.Entities.FilterEntities
{
    [XmlType(Namespace = "Bukimedia/PrestaSharp/Entities/FilterEntities")]
    public class GenericFilterEntity:PrestaShopEntity
    {
        public long id { get; set; }
    }
}
