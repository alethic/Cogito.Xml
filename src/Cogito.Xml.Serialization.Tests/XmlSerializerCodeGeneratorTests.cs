using System.CodeDom;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.Xml.Serialization.Tests
{

    [TestClass]
    public class XmlSerializerCodeGeneratorTests
    {

        static readonly XNamespace ns = "http://tempuri.org/SimpleSchema";
        static readonly XDocument SimpleSchemaText = XDocument.Parse(@"
<xs:schema
    xmlns:xs='http://www.w3.org/2001/XMLSchema'
    xmlns='http://tempuri.org/SimpleSchema'
    targetNamespace='http://tempuri.org/SimpleSchema'>
    <xs:complexType name='Person'>
        <xs:sequence>
            <xs:element name='Address' type='Address' minOccurs='0' maxOccurs='1' />
        </xs:sequence>
    </xs:complexType>
    <xs:complexType name='Address'>
        <xs:sequence>
            <xs:element name='Street' type='xs:string' minOccurs='0' maxOccurs='1' />
            <xs:element name='City' type='xs:string' minOccurs='0' maxOccurs='1' />
        </xs:sequence>
    </xs:complexType>
    <xs:complexType name='WithAddress'>
        <xs:sequence>
            <xs:element ref='AddressElement' />
        </xs:sequence>
    </xs:complexType>
    <xs:element name='AddressElement' type='Address' />
    <xs:element name='WithNested'>
        <xs:complexType>
            <xs:sequence>
                <xs:element name='Nested'>
                    <xs:complexType>
                        <xs:sequence>
                            <xs:element name='Element1' type='xs:string' />
                        </xs:sequence>
                    </xs:complexType>
                </xs:element>
            </xs:sequence>
        </xs:complexType>
    </xs:element>
</xs:schema>
");

        /// <summary>
        /// Loads an individual schema.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        static XmlSchema LoadSchema(XDocument schema)
        {
            return XmlSchema.Read(schema.CreateReader(), Validation);
        }

        /// <summary>
        /// Loads the schema set.
        /// </summary>
        /// <returns></returns>
        static XmlSchemaSet LoadSchemaSet()
        {
            var s = new XmlSchemaSet();
            s.Add(LoadSchema(SimpleSchemaText));
            s.Compile();
            return s;
        }

        static XmlSchemas LoadSchemas()
        {
            var s = new XmlSchemas();
            foreach (XmlSchema i in LoadSchemaSet().Schemas())
                s.Add(i);
            s.Compile(Validation, true);
            return s;
        }

        /// <summary>
        /// Simple XML validation that fails if an exception is raised.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void Validation(object sender, ValidationEventArgs args)
        {
            if (args.Exception != null)
                throw args.Exception;
        }

        [TestMethod]
        public void Test_can_get_type_name_for_global_type()
        {
            var g = new XmlSerializerCodeGenerator(LoadSchemaSet());
            g.XmlsToClrNamespace[ns] = "GeneratedCode";
            var n = g.GetTypeName(g.Schemas.GetGlobalType(ns + "Person"));
            Assert.IsTrue(n == "GeneratedCode.Person");
        }

        [TestMethod]
        public void Test_can_get_type_name_for_global_element()
        {
            var g = new XmlSerializerCodeGenerator(LoadSchemaSet());
            g.XmlsToClrNamespace[ns] = "GeneratedCode";
            var n = g.GetTypeName(g.Schemas.GetGlobalElement(ns + "AddressElement"));
            Assert.IsTrue(n == "GeneratedCode.AddressElement");
        }

        [TestMethod]
        public void Test_can_get_type_name_for_particle_element_with_complex_type()
        {
            var g = new XmlSerializerCodeGenerator(LoadSchemaSet());
            g.XmlsToClrNamespace[ns] = "GeneratedCode";
            var e = (XmlSchemaElement)((XmlSchemaSequence)((XmlSchemaComplexType)g.Schemas.GetGlobalType(ns + "Person")).Particle).Items[0];
            var n = g.GetTypeName(e);
            Assert.IsTrue(n == "GeneratedCode.Address");
        }

        [TestMethod]
        public void Test_can_get_type_name_for_particle_element_with_element_ref()
        {
            var g = new XmlSerializerCodeGenerator(LoadSchemaSet());
            g.XmlsToClrNamespace[ns] = "GeneratedCode";
            var e = (XmlSchemaElement)((XmlSchemaSequence)((XmlSchemaComplexType)g.Schemas.GetGlobalType(ns + "WithAddress")).Particle).Items[0];
            var n = g.GetTypeName(e);
            Assert.IsTrue(n == "GeneratedCode.AddressElement");
        }

        [TestMethod]
        public void Test_can_get_type_name_for_nested_element_with_nested_complex_type()
        {
            var g = new XmlSerializerCodeGenerator(LoadSchemaSet());
            g.XmlsToClrNamespace[ns] = "GeneratedCode";
            var e = (XmlSchemaElement)((XmlSchemaSequence)((XmlSchemaComplexType)g.Schemas.GetGlobalElement(ns + "WithNested").ElementSchemaType).Particle).Items[0];
            var n = g.GetTypeName(e);
            Assert.IsTrue(n == "GeneratedCode.AddressElement");
        }

    }

}
