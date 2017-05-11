using System.Xml;
using System.Xml.Linq;

namespace Cogito.Xml.Serialization
{

    public static class XmlQualifiedNameExtensions
    {

        /// <summary>
        /// Gets the <see cref="XmlQualifiedName"/> as a <see cref="XName"/>.
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static XName AsXName(this XmlQualifiedName self)
        {
            return XName.Get(self.Name, self.Namespace);
        }

    }

}
