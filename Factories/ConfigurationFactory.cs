using PrestaSharp.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PrestaSharp.Factories
{
    public class ConfigurationFactory : GenericFactory<configuration>
    {
        protected override string singularEntityName { get { return "configuration"; } }
        protected override string pluralEntityName { get { return "configurations"; } }

        public ConfigurationFactory(string BaseUrl, string Account, string SecretKey)
            : base(BaseUrl, Account, SecretKey)
        {
        }
    }
}
