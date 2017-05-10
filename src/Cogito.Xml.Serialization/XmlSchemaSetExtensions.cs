using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Cogito.Xml.Serialization
{

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
            return GetGlobalAttribute(self, new XmlQualifiedName(name, ns));
        }

    }

}
