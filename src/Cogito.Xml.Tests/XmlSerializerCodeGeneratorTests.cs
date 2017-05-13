using System.IO;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Cogito.Xml.Serialization;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.Xml.Tests
{

    [TestClass]
    public class XmlSerializerCodeGeneratorTests
    {

        [XmlType(TypeName = "FooType", Namespace = "http://tempuri.org/SimpleSchema")]
        class FooType
        {


        }


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
    <xs:simpleType name='Age'>
        <xs:restriction base='xs:integer'>
            <xs:minInclusive value='0' />
            <xs:maxInclusive value='120' />
        </xs:restriction>
    </xs:simpleType>
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
        public void Test_genericode()
        {
            var s = new XmlSchemaSet();
            s.Add(XmlSchema.Read(File.OpenRead(@"xsd\genericode\xml.xsd"), Validation));
            s.Add(XmlSchema.Read(File.OpenRead(@"xsd\genericode\genericode.xsd"), Validation));
            s.Compile();
            var g = new XmlSerializationCodeGenerator(s);
            g.MapNamespace("http://docs.oasis-open.org/codelist/ns/genericode/1.0/", "Genericode");
            var c = g.GenerateCode();
            var t = c.NormalizeWhitespace().ToFullString();
        }

        [TestMethod]
        public void Test_can_get_qname_for_type()
        {
            Assert.IsTrue(XmlSerializationCodeGenerator.GetXNameForType(typeof(FooType)) == ns + "FooType");
        }

        [TestMethod]
        public void Test_can_generate_code()
        {
            var g = new XmlSerializationCodeGenerator(LoadSchemaSet());
            g.MapNamespace(ns, "GeneratedCode");
            var c = g.GenerateCode();
            var t = c.NormalizeWhitespace().ToFullString();
        }

    }

}
