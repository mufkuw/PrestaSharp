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
    public class CategoryFactory : GenericFactory<category>
    {
        protected override string singularEntityName { get { return "category"; } }
        protected override string pluralEntityName { get { return "categories"; } }

        public CategoryFactory(string BaseUrl, string Account, string SecretKey)
            : base(BaseUrl, Account, SecretKey)
        {
        }
    }
}
