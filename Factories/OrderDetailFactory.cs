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
    public class OrderDetailFactory : GenericFactory<order_detail>
    {
        protected override string singularEntityName { get { return "order_detail"; } }
        protected override string pluralEntityName { get { return "order_details"; } }

        public OrderDetailFactory(string BaseUrl, string Account, string SecretKey) 
            : base(BaseUrl, Account, SecretKey)
        {
        }
    }
}