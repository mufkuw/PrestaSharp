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
    public class CustomerMessageFactory : GenericFactory<customer_message>
    {
        protected override string singularEntityName { get { return "customer_message"; } }
        protected override string pluralEntityName { get { return "customer_messages"; } }

        public CustomerMessageFactory(string BaseUrl, string Account, string SecretKey)
            : base(BaseUrl, Account, SecretKey)
        {
        }
    }
}
