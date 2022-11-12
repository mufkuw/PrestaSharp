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
    public class ProductOptionValueFactory : GenericFactory<product_option_value>
    {
        protected override string singularEntityName { get { return "product_option_value"; } }
        protected override string pluralEntityName { get { return "product_option_values"; } }

        public ProductOptionValueFactory(string BaseUrl, string Account, string SecretKey)
            : base(BaseUrl, Account, SecretKey)
        {
        }
    }
}