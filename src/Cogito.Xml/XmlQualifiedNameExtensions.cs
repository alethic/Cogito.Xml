using System;
using System.Xml;
using System.Xml.Linq;

namespace Cogito.Xml
{

    /// <summary>
    /// Provides extension methods for <see cref="XmlQualifiedName"/> instances.
    /// </summary>
    public static class XmlQualifiedNameExtensions
    {

        /// <summary>
        /// Gets the <see cref="XmlQualifiedName"/> as a <see cref="XName"/>.
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static XName AsXName(this XmlQualifiedName self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return XName.Get(self.Name, self.Namespace);
        }

    }

}
