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
    public class LanguageFactory : GenericFactory<language>
    {
        protected override string singularEntityName { get { return "language"; } }
        protected override string pluralEntityName { get { return "languages"; } }

        public LanguageFactory(string BaseUrl, string Account, string SecretKey)
            : base(BaseUrl, Account, SecretKey)
        {
        }
    }
}