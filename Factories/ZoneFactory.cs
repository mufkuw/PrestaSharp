﻿using PrestaSharp.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PrestaSharp.Factories
{
    public class ZoneFactory : GenericFactory<zone>
    {
        protected override string singularEntityName { get { return "zone"; } }
        protected override string pluralEntityName { get { return "zones"; } }

        public ZoneFactory(string BaseUrl, string Account, string SecretKey)
            : base(BaseUrl, Account, SecretKey)
        {
        }  
    }
}
