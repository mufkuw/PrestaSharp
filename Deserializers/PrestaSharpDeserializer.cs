﻿using RestSharp;
using RestSharp.Extensions;
using RestSharp.Serialization.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace PrestaSharp.Deserializers
{
    public class PrestaSharpDeserializer : IXmlDeserializer
    {
        //RootElement comes from RestSharp. It's value is taken from Request.RootElement
        public string RootElement { get; set; }
        public string Namespace { get; set; }
        public string DateFormat { get; set; }
        public CultureInfo Culture { get; set; }

        public PrestaSharpDeserializer()
        {
            Culture = CultureInfo.InvariantCulture;
        }

        public virtual T Deserialize<T>(IRestResponse response)
        {
            if (string.IsNullOrEmpty(response.Content))
                return default(T);

            XDocument doc = XDocument.Parse(response.Content.Trim());
            XElement root;

            var objType = typeof(T);
            XElement firstChild = doc.Root.Descendants().FirstOrDefault();

            if (doc.Root == null || firstChild?.Name == null)
            {
                string finalResponseError = "Deserialization problem. Root is null or response has no child.";
                if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
                {
                    finalResponseError += $" Additionnal information is: {response.ErrorMessage}";
                }
                throw new PrestaSharpException(response.Content, finalResponseError, response.StatusCode, response.ErrorException);
            }


            // autodetect xml namespace
            if (!Namespace.HasValue())
            {
                RemoveNamespace(doc);
            }

            var x = Activator.CreateInstance<T>();
            bool isSubclassOfRawGeneric = objType.IsSubclassOfRawGeneric(typeof(List<>));

            if (isSubclassOfRawGeneric)
            {
                x = (T)HandleListDerivative(x, doc.Root, objType.Name, objType);
            }
            else
            {
                root = doc.Root.Element(firstChild.Name.ToString().AsNamespaced(Namespace));
                Map(x, root);
            }

            return x;
        }

        private void RemoveNamespace(XDocument xdoc)
        {
            foreach (XElement e in xdoc.Root.DescendantsAndSelf())
            {
                if (e.Name.Namespace != XNamespace.None)
                {
                    e.Name = XNamespace.None.GetName(e.Name.LocalName);
                }
                if (e.Attributes().Any(a => a.IsNamespaceDeclaration || a.Name.Namespace != XNamespace.None))
                {
                    e.ReplaceAttributes(e.Attributes().Select(a => a.IsNamespaceDeclaration ? null : a.Name.Namespace != XNamespace.None ? new XAttribute(XNamespace.None.GetName(a.Name.LocalName), a.Value) : a));
                }
            }
        }

        public virtual void Map(object x, XElement root)
        {
            var objType = x.GetType();
            var props = objType.GetProperties();

            foreach (var prop in props)
            {
                var type = prop.PropertyType;

                if (!type.IsPublic || !prop.CanWrite)
                    continue;

                var name = prop.Name.AsNamespaced(Namespace);
                var value = GetValueFromXml(root, name, prop);

                if (value == null)
                {
                    // special case for inline list items
                    if (type.IsGenericType)
                    {
                        var genericType = type.GetGenericArguments()[0];
                        var first = GetElementByName(root, genericType.Name);
                        var list = (IList)Activator.CreateInstance(type);

                        if (first != null)
                        {
                            var elements = root.Elements(first.Name);
                            PopulateListFromElements(genericType, elements, list);
                        }

                        prop.SetValue(x, list, null);
                    }
                    continue;
                }

                // check for nullable and extract underlying type
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    // if the value is empty, set the property to null...
                    if (value == null || String.IsNullOrEmpty(value.ToString()))
                    {
                        prop.SetValue(x, null, null);
                        continue;
                    }
                    type = type.GetGenericArguments()[0];
                }

                if (type == typeof(bool))
                {
                    var toConvert = value.ToString().ToLowerInvariant();
                    prop.SetValue(x, XmlConvert.ToBoolean(toConvert), null);
                }
                else if (type.IsPrimitive)
                {
                    if (!String.IsNullOrEmpty(value.ToString()))
                    {
                        prop.SetValue(x, System.Convert.ChangeType(value, type, Culture), null);
                    }
                }
                else if (type.IsEnum)
                {
                    var converted = type.FindEnumValue(value.ToString(), Culture);
                    prop.SetValue(x, converted, null);
                }
                else if (type == typeof(Uri))
                {
                    var uri = new Uri(value.ToString(), UriKind.RelativeOrAbsolute);
                    prop.SetValue(x, uri, null);
                }
                else if (type == typeof(string))
                {
                    prop.SetValue(x, value, null);
                }
                else if (type == typeof(DateTime))
                {
                    if (DateFormat.HasValue())
                    {
                        value = DateTime.ParseExact(value.ToString(), DateFormat, Culture);
                    }
                    else
                    {
                        value = DateTime.Parse(value.ToString(), Culture);
                    }

                    prop.SetValue(x, value, null);
                }
                else if (type == typeof(DateTimeOffset))
                {
                    var toConvert = value.ToString();
                    if (!string.IsNullOrEmpty(toConvert))
                    {
                        DateTimeOffset deserialisedValue;
                        try
                        {
                            deserialisedValue = XmlConvert.ToDateTimeOffset(toConvert);
                            prop.SetValue(x, deserialisedValue, null);
                        }
                        catch (Exception)
                        {
                            object result;
                            if (TryGetFromString(toConvert, out result, type))
                            {
                                prop.SetValue(x, result, null);
                            }
                            else
                            {
                                //fallback to parse
                                deserialisedValue = DateTimeOffset.Parse(toConvert);
                                prop.SetValue(x, deserialisedValue, null);
                            }
                        }
                    }
                }
                else if (type == typeof(Decimal))
                {
                    //Hack for non defined price
                    if (value.Equals(""))
                    {
                        prop.SetValue(x, 0.0m, null);
                    }
                    else
                    {
                        value = Decimal.Parse(value.ToString(), Culture);
                        prop.SetValue(x, value, null);
                    }
                }
                else if (type == typeof(Guid))
                {
                    var raw = value.ToString();
                    value = string.IsNullOrEmpty(raw) ? Guid.Empty : new Guid(value.ToString());
                    prop.SetValue(x, value, null);
                }
                else if (type == typeof(TimeSpan))
                {
                    var timeSpan = XmlConvert.ToTimeSpan(value.ToString());
                    prop.SetValue(x, timeSpan, null);
                }
                else if (type.IsGenericType)
                {
                    var t = type.GetGenericArguments()[0];
                    var list = (IList)Activator.CreateInstance(type);

                    var container = GetElementByName(root, prop.Name.AsNamespaced(Namespace));

                    if (container.HasElements)
                    {
                        var first = container.Elements().FirstOrDefault();
                        var elements = container.Elements(first.Name);
                        PopulateListFromElements(t, elements, list);
                    }

                    prop.SetValue(x, list, null);
                }
                else if (type.IsSubclassOfRawGeneric(typeof(List<>)))
                {
                    // handles classes that derive from List<T>
                    // e.g. a collection that also has attributes
                    var list = HandleListDerivative(x, root, prop.Name, type);
                    prop.SetValue(x, list, null);
                }
                else
                {
                    //fallback to type converters if possible
                    object result;
                    if (TryGetFromString(value.ToString(), out result, type))
                    {
                        prop.SetValue(x, result, null);
                    }
                    else
                    {
                        // nested property classes
                        if (root != null)
                        {
                            var element = GetElementByName(root, name);
                            if (element != null)
                            {
                                var item = CreateAndMap(type, element);
                                prop.SetValue(x, item, null);
                            }
                        }
                    }
                }
            }
        }

        private static bool TryGetFromString(string inputString, out object result, Type type)
        {
#if !SILVERLIGHT && !WINDOWS_PHONE
            var converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(typeof(string)))
            {
                result = (converter.ConvertFromInvariantString(inputString));
                return true;
            }
            result = null;
            return false;
#else
                        result = null;
                        return false;
#endif
        }

        private void PopulateListFromElements(Type t, IEnumerable<XElement> elements, IList list)
        {
            foreach (var element in elements)
            {
                var item = CreateAndMap(t, element);
                list.Add(item);
            }
        }

        private object HandleListDerivative(object x, XElement root, string propName, Type type)
        {
            Type t;

            if (type.IsGenericType)
            {
                t = type.GetGenericArguments()[0];
            }
            else
            {
                t = type.BaseType.GetGenericArguments()[0];
            }


            var list = (IList)Activator.CreateInstance(type);

            //Modified version from RestSharp 
            var elements = root.Elements(t.Name.AsNamespaced(Namespace));

            var name = t.Name;

            if (!elements.Any())
            {
                var lowerName = name.ToLowerInvariant().AsNamespaced(Namespace);
                var firstNode = root.FirstNode;
                if (firstNode != null)
                {
                    elements = ((XElement)firstNode).Elements(lowerName);
                }
            }

            if (!elements.Any())
            {
                var lowerName = name.ToLowerInvariant().AsNamespaced(Namespace);
                elements = root.Descendants(lowerName);
            }

            if (!elements.Any())
            {
                var camelName = name.ToCamelCase(Culture).AsNamespaced(Namespace);
                elements = root.Descendants(camelName);
            }

            if (!elements.Any())
            {
                elements = root.Descendants().Where(e => e.Name.LocalName.RemoveUnderscoresAndDashes() == name);
            }

            if (!elements.Any())
            {
                var lowerName = name.ToLowerInvariant().AsNamespaced(Namespace);
                elements = root.Descendants().Where(e => e.Name.LocalName.RemoveUnderscoresAndDashes() == lowerName);
            }

            PopulateListFromElements(t, elements, list);

            // get properties too, not just list items
            // only if this isn't a generic type
            if (!type.IsGenericType)
            {
                Map(list, root.Element(propName.AsNamespaced(Namespace)) ?? root); // when using RootElement, the heirarchy is different
            }

            return list;
        }

        protected virtual object CreateAndMap(Type t, XElement element)
        {
            object item;
            if (t == typeof(String))
            {
                item = element.Value;
            }
            else if (t.IsPrimitive)
            {
                item = System.Convert.ChangeType(element.Value, t, Culture);
            }
            else
            {
                item = Activator.CreateInstance(t);
                Map(item, element);
            }

            return item;
        }

        protected virtual object GetValueFromXml(XElement root, XName name, PropertyInfo prop)
        {
            object val = null;

            if (root != null)
            {
                var element = GetElementByName(root, name);
                if (element == null)
                {
                    var attribute = GetAttributeByName(root, name);
                    if (attribute != null)
                    {
                        val = attribute.Value;
                    }
                }
                else
                {
                    if (!element.IsEmpty || element.HasElements || element.HasAttributes)
                    {
                        val = element.Value;
                    }
                }
            }

            return val;
        }

        protected virtual XElement GetElementByName(XElement root, XName name)
        {
            var lowerName = name.LocalName.ToLowerInvariant().AsNamespaced(name.NamespaceName);
            var camelName = name.LocalName.ToCamelCase(Culture).AsNamespaced(name.NamespaceName);
            if (root.Element(name) != null)
            {
                return root.Element(name);
            }
            if (root.Element(lowerName) != null)
            {
                return root.Element(lowerName);
            }

            if (root.Element(camelName) != null)
            {
                return root.Element(camelName);
            }

            if (name == "Value".AsNamespaced(name.NamespaceName))
            {
                return root;
            }

            // try looking for element that matches sanitized property name (Order by depth)
            var element = root.Descendants()
                    .OrderBy(d => d.Ancestors().Count())
                    .FirstOrDefault(d => d.Name.LocalName.RemoveUnderscoresAndDashes() == name.LocalName)
                    ?? root.Descendants()
                    .OrderBy(d => d.Ancestors().Count())
                    .FirstOrDefault(d => d.Name.LocalName.RemoveUnderscoresAndDashes() == name.LocalName.ToLowerInvariant());

            if (element != null)
            {
                return element;
            }

            return null;
        }

        protected virtual XAttribute GetAttributeByName(XElement root, XName name)
        {
            var lowerName = name.LocalName.ToLowerInvariant().AsNamespaced(name.NamespaceName);
            var camelName = name.LocalName.ToCamelCase(Culture).AsNamespaced(name.NamespaceName);

            if (root.Attribute(name) != null)
            {
                return root.Attribute(name);
            }

            if (root.Attribute(lowerName) != null)
            {
                return root.Attribute(lowerName);
            }

            if (root.Attribute(camelName) != null)
            {
                return root.Attribute(camelName);
            }

            // try looking for element that matches sanitized property name
            var element = root.Attributes().FirstOrDefault(d => d.Name.LocalName.RemoveUnderscoresAndDashes() == name.LocalName);
            if (element != null)
            {
                return element;
            }

            return null;
        }
    }
}
