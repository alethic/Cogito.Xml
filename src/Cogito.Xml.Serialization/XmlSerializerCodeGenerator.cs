using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Cogito.Collections;
using Cogito.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cogito.Xml.Serialization
{

    /// <summary>
    /// Provides methods for generating code from XML schema that utilizes the <see cref="XmlSerializer"/>.
    /// </summary>
    public class XmlSerializerCodeGenerator
    {

        static readonly HashSet<XNamespace> SYSTEM_XMLNS = new HashSet<XNamespace>(new XNamespace[]
        {
            "http://www.w3.org/2001/XMLSchema"
        });

        /// <summary>
        /// Derives the qualified XML name for the given type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static XName GetXNameForType(Type type)
        {
            return type.GetCustomAttributes<XmlTypeAttribute>()
                .Where(i => i.Namespace != null && i.TypeName != null)
                .Select(i => XName.Get(i.TypeName, i.Namespace))
                .FirstOrDefault();
        }

        readonly Dictionary<XName, Type> xmlnToClrType = new Dictionary<XName, Type>();
        readonly Dictionary<XNamespace, string> xmlnsToClrNamespace = new Dictionary<XNamespace, string>();

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="schemas"></param>
        public XmlSerializerCodeGenerator(XmlSchemaSet schemas)
        {
            Schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));
        }

        /// <summary>
        /// Gets the set of schemas for which code is being generated.
        /// </summary>
        public XmlSchemaSet Schemas { get; }

        /// <summary>
        /// Gets all of the usable global types.
        /// </summary>
        /// <returns></returns>
        IEnumerable<XmlSchemaType> GetUserGlobalTypes()
        {
            return Schemas.GlobalTypes.Values
                .Cast<XmlSchemaType>()
                .Where(i => !SYSTEM_XMLNS.Contains(i.QualifiedName.Namespace));
        }

        /// <summary>
        /// Gets all of the usable global elements.
        /// </summary>
        /// <returns></returns>
        IEnumerable<XmlSchemaElement> GetUserGlobalElements()
        {
            return Schemas.GlobalElements.Values
                .Cast<XmlSchemaElement>()
                .Where(i => !SYSTEM_XMLNS.Contains(i.QualifiedName.Namespace));
        }

        /// <summary>
        /// Gets all of the usable global elements.
        /// </summary>
        /// <returns></returns>
        IEnumerable<XmlSchemaAttribute> GetUserGlobalAttributes()
        {
            return Schemas.GlobalAttributes.Values
                .Cast<XmlSchemaAttribute>()
                .Where(i => !SYSTEM_XMLNS.Contains(i.QualifiedName.Namespace));
        }

        /// <summary>
        /// Maps the given XMLNS to the specified CLR namespace.
        /// </summary>
        /// <param name="xmlns"></param>
        /// <param name="ns"></param>
        public void MapNamespace(XNamespace xmlns, string ns)
        {
            xmlnsToClrNamespace[xmlns] = ns;
        }

        /// <summary>
        /// Adds the given <see cref="Type"/> as an existing type.
        /// </summary>
        /// <param name="type"></param>
        public void AddType(Type type)
        {
            var name = GetXNameForType(type);
            if (name != null)
                xmlnToClrType[name] = type;
        }

        /// <summary>
        /// Gets the CLR namespace associated with the given XML namespace.
        /// </summary>
        /// <param name="xmlns"></param>
        /// <returns></returns>
        string ClrNamespaceForXmlns(XNamespace xmlns)
        {
            return xmlnsToClrNamespace.GetOrDefault(xmlns.NamespaceName) ?? xmlns.NamespaceName;
        }

        /// <summary>
        /// Generates the code for the schema.
        /// </summary>
        /// <returns></returns>
        public CompilationUnitSyntax GenerateCode()
        {
            return CompilationUnit()
                .WithMembers(List<MemberDeclarationSyntax>(GenerateNamespaces()));
        }

        /// <summary>
        /// Provides all the unique XML namespaces of global elements.
        /// </summary>
        /// <returns></returns>
        IEnumerable<XNamespace> EnumerateAllXmlNamespaces()
        {
            // iterates out all namespaces of all global objects
            IEnumerable<XNamespace> Iter()
            {
                foreach (var i in GetUserGlobalTypes())
                    yield return i.QualifiedName.Namespace;

                foreach (var i in GetUserGlobalElements())
                    yield return i.QualifiedName.Namespace;

                foreach (var i in GetUserGlobalAttributes())
                    yield return i.QualifiedName.Namespace;
            }

            return Iter().Distinct();
        }

        /// <summary>
        /// Generates the set of unique top level namespaces.
        /// </summary>
        /// <returns></returns>
        IEnumerable<NamespaceDeclarationSyntax> GenerateNamespaces()
        {
            foreach (var xmlns in EnumerateAllXmlNamespaces()
                    .Select(i => ClrNamespaceForXmlns(i))
                    .Distinct())
                yield return GenerateNamespace(xmlns);
        }

        /// <summary>
        /// Generates the code for a given CLR namespace.
        /// </summary>
        /// <param name="xmlns"></param>
        /// <returns></returns>
        NamespaceDeclarationSyntax GenerateNamespace(string ns)
        {
            return NamespaceDeclaration(ParseName(ns))
                .WithMembers(List<MemberDeclarationSyntax>()
                    .AddRange(GenerateClassesForGlobalTypes(ns))
                    .AddRange(GenerateClassesForGlobalElements(ns)));
        }

        /// <summary>
        /// Generates class declarations for all of the global types belonging to the given CLR namespace.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        IEnumerable<ClassDeclarationSyntax> GenerateClassesForGlobalTypes(string ns)
        {
            // all global types that belong in the given CLR namespace
            var types = GetUserGlobalTypes()
                .Where(i => ClrNamespaceForXmlns(i.QualifiedName.Namespace) == ns);

            foreach (var type in types)
                yield return GenerateClassForType(type);
        }

        /// <summary>
        /// Generates the class declarations for all of the global elements belonging to the given CLR namespace.
        /// </summary>
        /// <param name="ns"></param>
        /// <returns></returns>
        IEnumerable<ClassDeclarationSyntax> GenerateClassesForGlobalElements(string ns)
        {
            // all global types that belong in the given CLR namespace
            var elements = GetUserGlobalElements()
                .Cast<XmlSchemaElement>()
                .Where(i => ClrNamespaceForXmlns(i.QualifiedName.Namespace) == ns);

            foreach (var element in elements)
                yield return GenerateClassForElement(element);

            yield break;
        }

        /// <summary>
        /// Returns a new <see cref="LiteralExpressionSyntax"/> representing a string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        LiteralExpressionSyntax StringLiteral(string value)
        {
            return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value));
        }

        /// <summary>
        /// Gets the type name for the given type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        TypeSyntax TypeSyntax(Type type)
        {
            if (type == typeof(string))
                return PredefinedType(Token(SyntaxKind.StringKeyword));
            if (type == typeof(int))
                return PredefinedType(Token(SyntaxKind.IntKeyword));

            return ParseTypeName(type.FullName);
        }

        /// <summary>
        /// Gets the type name for the given type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        TypeSyntax TypeSyntax<T>()
        {
            return TypeSyntax(typeof(T));
        }

        /// <summary>
        /// Gets the type name for the referenced XML entity.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        TypeSyntax TypeOf(XmlQualifiedName name)
        {
            return ParseTypeName(ClrNamespaceForXmlns(name.Namespace) + "." + name.Name);
        }

        /// <summary>
        /// Gets the type name of the given schema type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        TypeSyntax TypeOf(XmlSchemaType type)
        {
            switch (type)
            {
                case XmlSchemaSimpleType simpleType:
                    return TypeOf(simpleType);
                case XmlSchemaComplexType complexType:
                    return TypeOf(complexType);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the type name of the given complex schema type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        TypeSyntax TypeOf(XmlSchemaSimpleType type)
        {
            switch (type.TypeCode)
            {
                case XmlTypeCode.String:
                case XmlTypeCode.NormalizedString:
                case XmlTypeCode.Token:
                case XmlTypeCode.Name:
                case XmlTypeCode.Id:
                case XmlTypeCode.Idref:
                    return TypeSyntax<string>();
                case XmlTypeCode.Byte:
                    return TypeSyntax<byte>();
                case XmlTypeCode.Boolean:
                    return TypeSyntax<bool>();
                case XmlTypeCode.Short:
                    return TypeSyntax<short>();
                case XmlTypeCode.UnsignedShort:
                    return TypeSyntax<ushort>();
                case XmlTypeCode.Int:
                case XmlTypeCode.Integer:
                case XmlTypeCode.PositiveInteger:
                case XmlTypeCode.NegativeInteger:
                case XmlTypeCode.NonNegativeInteger:
                case XmlTypeCode.NonPositiveInteger:
                    return TypeSyntax<int>();
                case XmlTypeCode.UnsignedInt:
                    return TypeSyntax<uint>();
                case XmlTypeCode.Long:
                    return TypeSyntax<long>();
                case XmlTypeCode.UnsignedLong:
                    return TypeSyntax<ulong>();
                case XmlTypeCode.Float:
                    return TypeSyntax<float>();
                case XmlTypeCode.Double:
                    return TypeSyntax<double>();
                case XmlTypeCode.Decimal:
                    return TypeSyntax<decimal>();
                case XmlTypeCode.Date:
                case XmlTypeCode.DateTime:
                    return TypeSyntax<DateTime>();
                case XmlTypeCode.QName:
                    return TypeSyntax<XName>();
                case XmlTypeCode.None:
                    return TypeOf(type.QualifiedName);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the type name of the given complex schema type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        TypeSyntax TypeOf(XmlSchemaComplexType type)
        {
            return TypeOf(type.QualifiedName);
        }

        /// <summary>
        /// Generates a basic class definition with standard attributes.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        ClassDeclarationSyntax GenerateClass(string identifier)
        {
            return ClassDeclaration(identifier)
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.PartialKeyword)))
                .WithAttributeLists(List(
                    GenerateAttributesForType()));
        }

        /// <summary>
        /// Generates a new class for the given schema type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        ClassDeclarationSyntax GenerateClassForType(XmlSchemaType type)
        {
            switch (type)
            {
                case XmlSchemaSimpleType simpleType:
                    return GenerateClassForSimpleType(simpleType);
                case XmlSchemaComplexType complexType:
                    return GenerateClassForComplexType(complexType);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Generates a new class for the given schema type.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ClassDeclarationSyntax GenerateClassForComplexType(XmlSchemaComplexType type)
        {
            // basic class
            var c = GenerateClass(type.QualifiedName.Name ?? "Generated");

            // class has a base type
            if (type.BaseXmlSchemaType != null && !SYSTEM_XMLNS.Contains(type.BaseXmlSchemaType.QualifiedName.Namespace))
                c = c.AddBaseListTypes(SimpleBaseType(TypeOf(type)));

            // named type
            if (type.QualifiedName.IsEmpty == false)
            {
                c = c.AddAttributeLists(AttributeList(SingletonSeparatedList(GenerateXmlTypeAttribute(type.QualifiedName))));
                c = c.AddMembers(GenerateClassTypeNameField(type.QualifiedName));
            }

            // properties of type
            c = c.AddMembers(GenerateClassMembersForComplexType(type).ToArray());

            return c;
        }

        /// <summary>
        /// Generates a new class for the given schema type.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ClassDeclarationSyntax GenerateClassForSimpleType(XmlSchemaSimpleType type)
        {
            var c = GenerateClass(type.QualifiedName.Name ?? "Generated");

            // type inherits from another type
            if (type.BaseXmlSchemaType != null)
            {
                // base type is not a built in type
                if (!SYSTEM_XMLNS.Contains(type.BaseXmlSchemaType.QualifiedName.Namespace))
                    c = c.AddBaseListTypes(SimpleBaseType(TypeOf(type.BaseXmlSchemaType)));
            }

            return c;
        }

        /// <summary>
        /// Generates the static typeName field.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FieldDeclarationSyntax GenerateClassTypeNameField(XmlQualifiedName name)
        {
            return FieldDeclaration(VariableDeclaration(
                    TypeSyntax<XmlQualifiedName>(),
                    SeparatedList<VariableDeclaratorSyntax>().Add(
                        VariableDeclarator(Identifier("typeName"))
                            .WithInitializer(EqualsValueClause(
                                ObjectCreationExpression(
                                    TypeSyntax<XmlQualifiedName>(),
                                    ArgumentList()
                                        .AddArguments(Argument(StringLiteral(name.Name)))
                                        .AddArguments(Argument(StringLiteral(name.Namespace))),
                                    null))))))
                .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword)));
        }

        /// <summary>
        /// Generates a new class for the given element.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public ClassDeclarationSyntax GenerateClassForElement(XmlSchemaElement element)
        {
            // element references another type
            if (element.SchemaType == null && element.SchemaTypeName.IsEmpty == false)
                return GenerateClassForType(element.ElementSchemaType)
                    .WithIdentifier(Identifier(element.Name));

            // element defines  type
            if (element.SchemaType != null && element.SchemaTypeName.IsEmpty)
            {
                var c = GenerateClassForType(element.SchemaType)
                    .WithIdentifier(Identifier(element.Name));

                if (!string.IsNullOrEmpty(element.QualifiedName.Namespace))
                    c = c.AddAttributeLists(AttributeList(SingletonSeparatedList(GenerateXmlRootAttribute(element.QualifiedName))));

                return c;
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Generates the set of attributes standard on any type.
        /// </summary>
        /// <returns></returns>
        IEnumerable<AttributeListSyntax> GenerateAttributesForType()
        {
            yield return AttributeList(SeparatedList<AttributeSyntax>().Add(GenerateGeneratedCodeAttribute()));
            yield return AttributeList(SeparatedList<AttributeSyntax>().Add(GenerateDebuggerStepThroughAttribute()));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IEnumerable<MemberDeclarationSyntax> GenerateClassMembersForComplexType(XmlSchemaComplexType type)
        {
            // generate properties for attributes
            foreach (var attribute in type.Attributes.Cast<XmlSchemaAttribute>())
                foreach (var memeber in GenerateClassMembersForAttribute(attribute))
                    yield return memeber;

            // generate properties for content
            foreach (var member in GenerateClassMembersForParticle(type.ContentTypeParticle))
                yield return member;
        }

        /// <summary>
        /// Generates a new property that corresponds with the given attribute.
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        IEnumerable<PropertyDeclarationSyntax> GenerateClassMembersForAttribute(XmlSchemaAttribute attribute)
        {
            yield return GeneratePropertyOfType(attribute.Name, attribute.SchemaType);
        }

        /// <summary>
        /// Generates a new property of the given schema type.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        PropertyDeclarationSyntax GeneratePropertyOfType(string identifier, XmlSchemaType type)
        {
            switch (type)
            {
                case XmlSchemaSimpleType simpleType:
                    return GeneratePropertyOfSimpleType(identifier, simpleType);
                case XmlSchemaComplexType complexType:
                    return GeneratePropertyOfComplexType(identifier, complexType);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Generates a new property of the given CLR type.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        PropertyDeclarationSyntax GenerateProperty(string identifier, TypeSyntax type)
        {
            return PropertyDeclaration(type, identifier)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithAttributeLists(
                    List(GenerateAttributesForProperty(identifier)
                        .Select(i => AttributeList().AddAttributes(i))))
                .WithAccessorList(
                    AccessorList(
                        List<AccessorDeclarationSyntax>()
                            .Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))
                            .Add(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))));
        }

        /// <summary>
        /// Generates a new property of the given schema type.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        PropertyDeclarationSyntax GeneratePropertyOfSimpleType(string identifier, XmlSchemaSimpleType type)
        {
            return GenerateProperty(identifier, TypeOf(type));
        }

        /// <summary>
        /// Generates a new property of the given schema type.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        PropertyDeclarationSyntax GeneratePropertyOfComplexType(string identifier, XmlSchemaComplexType type)
        {
            return GenerateProperty(identifier, TypeOf(type));
        }

        /// <summary>
        /// Generates new properties for the given XML object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        IEnumerable<MemberDeclarationSyntax> GenerateClassMembersForObject(XmlSchemaObject obj)
        {
            switch (obj)
            {
                case XmlSchemaParticle p:
                    return GenerateClassMembersForParticle(p);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Generates new properties for the given content type.
        /// </summary>
        /// <param name="particle"></param>
        /// <returns></returns>
        IEnumerable<MemberDeclarationSyntax> GenerateClassMembersForParticle(XmlSchemaParticle particle)
        {
            switch (particle)
            {
                case XmlSchemaAll all:
                    return GenerateClassMembersForAllParticle(all);
                case XmlSchemaAny any:
                    return GenerateClassMembersForAnyParticle(any);
                case XmlSchemaSequence sequence:
                    return GenerateClassMembersForSequenceParticle(sequence);
                case XmlSchemaElement element:
                    return GenerateClassMembersForElementParticle(element);
                case XmlSchemaParticle p when p.GetType().Name == "EmptyParticle":
                    return Enumerable.Empty<PropertyDeclarationSyntax>();
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Generates new properties for the given <see cref="XmlSchemaAll"/>.
        /// </summary>
        /// <param name="all"></param>
        /// <returns></returns>
        IEnumerable<MemberDeclarationSyntax> GenerateClassMembersForAllParticle(XmlSchemaAll all)
        {
            yield break;
        }

        /// <summary>
        /// Generates new properties for the given <see cref="XmlSchemaAny"/>.
        /// </summary>
        /// <param name="any"></param>
        /// <returns></returns>
        IEnumerable<MemberDeclarationSyntax> GenerateClassMembersForAnyParticle(XmlSchemaAny any)
        {
            yield break;
        }

        /// <summary>
        /// Generates new properties for the given element.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        IEnumerable<MemberDeclarationSyntax> GenerateClassMembersForElementParticle(XmlSchemaElement element)
        {
            // element is a reference to another global element
            // property type is named after that element
            if (element.RefName.IsEmpty == false)
            {
                yield return GenerateProperty(element.QualifiedName.Name, TypeOf(element.RefName))
                    .AddAttributeLists(AttributeList().AddAttributes(
                        GenerateXmlElementAttributeForProperty(element)));
                yield break;
            }

            // element references an existing schema type
            if (element.SchemaTypeName.IsEmpty == false)
            {
                yield return GeneratePropertyOfType(element.QualifiedName.Name, element.ElementSchemaType)
                    .AddAttributeLists(AttributeList().AddAttributes(
                        GenerateXmlElementAttributeForProperty(element)));
                yield break;
            }

            // element has nested schema type
            if (element.SchemaType != null)
            {
                yield return GenerateClassForElement(element);
                yield return GenerateProperty(element.QualifiedName.Name, ParseTypeName(element.QualifiedName.Name))
                    .AddAttributeLists(AttributeList().AddAttributes(
                        GenerateXmlElementAttributeForProperty(element)));
                yield break;
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Generates a new <see cref="XmlElementAttribute"/> for a property.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        AttributeSyntax GenerateXmlElementAttributeForProperty(XmlSchemaElement element)
        {
            return Attribute(IdentifierName(typeof(XmlElementAttribute).FullName),
                AttributeArgumentList().AddArguments(
                    AttributeArgument(NameEquals(IdentifierName(nameof(XmlElementAttribute.ElementName))), null, StringLiteral(element.QualifiedName.Name)),
                    !string.IsNullOrEmpty(element.QualifiedName.Namespace) ? AttributeArgument(NameEquals(IdentifierName(nameof(XmlElementAttribute.Namespace))), null, StringLiteral(element.QualifiedName.Namespace)) : null));
        }

        /// <summary>
        /// Generates new properties for the given sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        IEnumerable<MemberDeclarationSyntax> GenerateClassMembersForSequenceParticle(XmlSchemaSequence sequence)
        {
            foreach (XmlSchemaObject o in sequence.Items)
                foreach (var m in GenerateClassMembersForObject(o))
                    yield return m;
        }

        /// <summary>
        /// Generates new attributes for a property.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        IEnumerable<AttributeSyntax> GenerateAttributesForProperty(string identifier)
        {
            yield break;
        }

        /// <summary>
        /// Generates the <see cref="DebuggerStepThroughAttribute"/> annotation.
        /// </summary>
        /// <returns></returns>
        AttributeSyntax GenerateDebuggerStepThroughAttribute()
        {
            return Attribute(IdentifierName(typeof(DebuggerStepThroughAttribute).FullName), AttributeArgumentList());
        }

        /// <summary>
        /// Generates the <see cref="GeneratedCodeAttribute"/> annotation.
        /// </summary>
        /// <returns></returns>
        AttributeSyntax GenerateGeneratedCodeAttribute()
        {
            return Attribute(IdentifierName(typeof(GeneratedCodeAttribute).FullName),
                AttributeArgumentList().AddArguments(
                    AttributeArgument(StringLiteral(GetType().Assembly.GetName().Name)),
                    AttributeArgument(StringLiteral(GetType().Assembly.GetName().Version.ToString()))));
        }

        /// <summary>
        /// Generates the <see cref="XmlTypeAttribute"/> annotation for the given <see cref="XmlQualifiedName"/>.
        /// </summary>
        /// <param name="qname"></param>
        /// <returns></returns>
        AttributeSyntax GenerateXmlTypeAttribute(XmlQualifiedName qname)
        {
            return Attribute(IdentifierName(typeof(XmlTypeAttribute).FullName),
                AttributeArgumentList().AddArguments(
                    AttributeArgument(NameEquals(IdentifierName(nameof(XmlTypeAttribute.TypeName))), null, StringLiteral(qname.Name)),
                    !string.IsNullOrWhiteSpace(qname.Namespace) ? AttributeArgument(NameEquals(IdentifierName(nameof(XmlTypeAttribute.Namespace))), null, StringLiteral(qname.Namespace)) : null));
        }

        /// <summary>
        /// Generates the <see cref="XmlRootAttribute"/> annotation for the given <see cref="XmlQualifiedName"/>.
        /// </summary>
        /// <param name="qname"></param>
        /// <returns></returns>
        AttributeSyntax GenerateXmlRootAttribute(XmlQualifiedName qname)
        {
            return Attribute(IdentifierName(typeof(XmlRootAttribute).FullName),
                AttributeArgumentList().AddArguments(
                    AttributeArgument(NameEquals(IdentifierName(nameof(XmlRootAttribute.ElementName))), null, StringLiteral(qname.Name)),
                    AttributeArgument(NameEquals(IdentifierName(nameof(XmlRootAttribute.Namespace))), null, StringLiteral(qname.Namespace))));
        }

    }

}
