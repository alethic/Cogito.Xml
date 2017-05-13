using System;
using System.Xml;
using System.Xml.Linq;

namespace Cogito.Xml.Linq
{

    /// <summary>
    /// Extension methods for working with <see cref="XName"/> instances.
    /// </summary>
    public static class XNameExtensions
    {

        /// <summary>
        /// Converts the <see cref="XName"/> to a <see cref="XmlQualifiedName"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static XmlQualifiedName AsXmlQualifiedName(this XName name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return new XmlQualifiedName(name.LocalName, name.NamespaceName);
        }

    }

}
