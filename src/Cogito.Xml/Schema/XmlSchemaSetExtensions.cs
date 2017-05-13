using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Cogito.Xml.Schema
{

    /// <summary>
    /// Provides extension methods for working with <see cref="XmlSchemaSet"/> instances.
    /// </summary>
    public static class XmlSchemaSetExtensions
    {

        /// <summary>
        /// Gets the global type with the specified name.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static XmlSchemaType GetGlobalType(this XmlSchemaSet self, XmlQualifiedName name)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return (XmlSchemaType)self.GlobalTypes[name];
        }

        /// <summary>
        /// Gets the global type with the specified name.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static XmlSchemaType GetGlobalType(this XmlSchemaSet self, XName name)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return GetGlobalType(self, new XmlQualifiedName(name.LocalName, name.NamespaceName));
        }

        /// <summary>
        /// Gets the global type with the specified name.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        /// <param name="ns"></param>
        /// <returns></returns>
        public static XmlSchemaType GetGlobalType(this XmlSchemaSet self, string name, string ns)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (ns == null)
                throw new ArgumentNullException(nameof(ns));

            return GetGlobalType(self, new XmlQualifiedName(name, ns));
        }

        /// <summary>
        /// Gets the global element with the specified name.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static XmlSchemaElement GetGlobalElement(this XmlSchemaSet self, XmlQualifiedName name)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return (XmlSchemaElement)self.GlobalElements[name];
        }

        /// <summary>
        /// Gets the global element with the specified name.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static XmlSchemaElement GetGlobalElement(this XmlSchemaSet self, XName name)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return GetGlobalElement(self, new XmlQualifiedName(name.LocalName, name.NamespaceName));
        }

        /// <summary>
        /// Gets the global element with the specified name.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        /// <param name="ns"></param>
        /// <returns></returns>
        public static XmlSchemaElement GetGlobalElement(this XmlSchemaSet self, string name, string ns)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (ns == null)
                throw new ArgumentNullException(nameof(ns));

            return GetGlobalElement(self, new XmlQualifiedName(name, ns));
        }

        /// <summary>
        /// Gets the global attribute with the specified name.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static XmlSchemaAttribute GetGlobalAttribute(this XmlSchemaSet self, XmlQualifiedName name)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return (XmlSchemaAttribute)self.GlobalTypes[name];
        }

        /// <summary>
        /// Gets the global attribute with the specified name.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static XmlSchemaAttribute GetGlobalAttribute(this XmlSchemaSet self, XName name)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return GetGlobalAttribute(self, new XmlQualifiedName(name.LocalName, name.NamespaceName));
        }

        /// <summary>
        /// Gets the global attribute with the specified name.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="name"></param>
        /// <param name="ns"></param>
        /// <returns></returns>
        public static XmlSchemaAttribute GetGlobalAttribute(this XmlSchemaSet self, string name, string ns)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (ns == null)
                throw new ArgumentNullException(nameof(ns));

            return GetGlobalAttribute(self, new XmlQualifiedName(name, ns));
        }

    }

}
