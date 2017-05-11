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
                if (type is XmlSchemaComplexType complexType)
                    yield return GenerateClassForComplexType(complexType.QualifiedName.Name, complexType, false);
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
                continue;

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
        /// Gets the type name of the given schema type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        TypeSyntax TypeOf(XmlSchemaType type)
        {
            switch (type.TypeCode)
            {
                case XmlTypeCode.String:
                    return TypeSyntax<string>();
                case XmlTypeCode.Int:
                case XmlTypeCode.Integer:
                    return TypeSyntax<int>();
                case XmlTypeCode.None when type is XmlSchemaComplexType complexType:
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
        TypeSyntax TypeOf(XmlSchemaComplexType type)
        {
            return ParseTypeName(type.QualifiedName.Name);
        }

        /// <summary>
        /// Generates a new class for the given schema type.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ClassDeclarationSyntax GenerateClassForComplexType(string identifier, XmlSchemaComplexType type, bool isElement)
        {
            return ClassDeclaration(identifier)
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.PartialKeyword)))
                .WithBaseList(BaseList(SeparatedList<BaseTypeSyntax>()
                    .Add(SimpleBaseType(TypeSyntax<IXmlSerializable>()))))
                .WithAttributeLists(
                    List(GenerateAttributesForType(type, isElement)))
                .WithMembers(
                    List<MemberDeclarationSyntax>()
                        .Add(
                            FieldDeclaration(
                                VariableDeclaration(
                                    TypeSyntax<XmlQualifiedName>(),
                                    SeparatedList<VariableDeclaratorSyntax>().Add(
                                        VariableDeclarator(Identifier("typeName"))
                                            .WithInitializer(EqualsValueClause(
                                                ObjectCreationExpression(
                                                    TypeSyntax<XmlQualifiedName>(),
                                                    ArgumentList()
                                                        .AddArguments(Argument(StringLiteral(type.QualifiedName.Name)))
                                                        .AddArguments(Argument(StringLiteral(type.QualifiedName.Namespace))),
                                                    null))))))
                                .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword))))
                        .AddRange(
                            GenerateClassesForAnonymousTypes(type))
                        .AddRange(
                            GeneratePropertiesForComplexType(type)));
        }

        /// <summary>
        /// Generates the set of attributes for the given complex type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isElement"></param>
        /// <returns></returns>
        IEnumerable<AttributeListSyntax> GenerateAttributesForType(XmlSchemaComplexType type, bool isElement)
        {
            yield return AttributeList(SeparatedList<AttributeSyntax>().Add(GenerateGeneratedCodeAttribute()));
            yield return AttributeList(SeparatedList<AttributeSyntax>().Add(GenerateDebuggerStepThroughAttribute()));
            yield return AttributeList(SeparatedList<AttributeSyntax>().Add(GenerateXmlTypeAttribute(type.QualifiedName)));

            if (isElement)
                yield return AttributeList(SeparatedList<AttributeSyntax>().Add(GenerateXmlRootAttribute(type.QualifiedName)));
        }

        /// <summary>
        /// Generates the set of anonymous types for the given complex type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IEnumerable<ClassDeclarationSyntax> GenerateClassesForAnonymousTypes(XmlSchemaComplexType type)
        {
            yield break;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IEnumerable<PropertyDeclarationSyntax> GeneratePropertiesForComplexType(XmlSchemaComplexType type)
        {
            // generate properties for attributes
            foreach (var attribute in type.Attributes.Cast<XmlSchemaAttribute>())
                yield return GeneratePropertyForAttribute(attribute);

            // generate properties for content
            foreach (var member in GeneratePropertiesForParticle(type.ContentTypeParticle))
                yield return member;
        }

        /// <summary>
        /// Generates a new property that corresponds with the given attribute.
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        PropertyDeclarationSyntax GeneratePropertyForAttribute(XmlSchemaAttribute attribute)
        {
            return GenerateProperty(attribute.Name, attribute.SchemaType);
        }

        /// <summary>
        /// Generates a new property of the given schema type.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        PropertyDeclarationSyntax GenerateProperty(string identifier, XmlSchemaType type)
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
        /// Generates a new property of the given schema type.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        PropertyDeclarationSyntax GeneratePropertyOfSimpleType(string identifier, XmlSchemaSimpleType type)
        {
            return PropertyDeclaration(TypeOf(type), identifier)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithAttributeLists(
                    List(GenerateAttributesForProperty(identifier, type)
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
        PropertyDeclarationSyntax GeneratePropertyOfComplexType(string identifier, XmlSchemaComplexType type)
        {
            return PropertyDeclaration(TypeOf(type), identifier)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithAttributeLists(
                    List(GenerateAttributesForProperty(identifier, type)
                        .Select(i => AttributeList().AddAttributes(i))))
                .WithAccessorList(
                    AccessorList(
                        List<AccessorDeclarationSyntax>()
                            .Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))
                            .Add(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))));
        }

        /// <summary>
        /// Generates new properties for the given XML object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        IEnumerable<PropertyDeclarationSyntax> GeneratePropertiesForObject(XmlSchemaObject obj)
        {
            switch (obj)
            {
                case XmlSchemaParticle p:
                    return GeneratePropertiesForParticle(p);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Generates new properties for the given content type.
        /// </summary>
        /// <param name="particle"></param>
        /// <returns></returns>
        IEnumerable<PropertyDeclarationSyntax> GeneratePropertiesForParticle(XmlSchemaParticle particle)
        {
            switch (particle)
            {
                case XmlSchemaAll all:
                    return GeneratePropertiesForAll(all);
                case XmlSchemaAny any:
                    return GeneratePropertiesForAny(any);
                case XmlSchemaSequence sequence:
                    return GeneratePropertiesForSequence(sequence);
                case XmlSchemaElement element:
                    return GeneratePropertiesForElement(element);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Generates new properties for the given <see cref="XmlSchemaAll"/>.
        /// </summary>
        /// <param name="all"></param>
        /// <returns></returns>
        IEnumerable<PropertyDeclarationSyntax> GeneratePropertiesForAll(XmlSchemaAll all)
        {
            yield break;
        }

        /// <summary>
        /// Generates new properties for the given <see cref="XmlSchemaAny"/>.
        /// </summary>
        /// <param name="any"></param>
        /// <returns></returns>
        IEnumerable<PropertyDeclarationSyntax> GeneratePropertiesForAny(XmlSchemaAny any)
        {
            yield break;
        }

        /// <summary>
        /// Generates new properties for the given element.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        IEnumerable<PropertyDeclarationSyntax> GeneratePropertiesForElement(XmlSchemaElement element)
        {
            yield return GenerateProperty(element.QualifiedName.Name, element.ElementSchemaType)
                .AddAttributeLists(AttributeList().AddAttributes(
                    GenerateXmlElementAttributeForProperty(element.Name)));
        }

        /// <summary>
        /// Generates a new <see cref="XmlElementAttribute"/> for a property.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        AttributeSyntax GenerateXmlElementAttributeForProperty(string identifier)
        {
            return Attribute(IdentifierName(typeof(XmlElementAttribute).FullName),
                AttributeArgumentList());
        }

        /// <summary>
        /// Generates new properties for the given sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        IEnumerable<PropertyDeclarationSyntax> GeneratePropertiesForSequence(XmlSchemaSequence sequence)
        {
            foreach (XmlSchemaObject o in sequence.Items)
                foreach (var m in GeneratePropertiesForObject(o))
                    yield return m;
        }

        /// <summary>
        /// Generates new attributes for a property.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        IEnumerable<AttributeSyntax> GenerateAttributesForProperty(string identifier, XmlSchemaType type)
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
                    AttributeArgument(NameEquals(IdentifierName(nameof(XmlTypeAttribute.Namespace))), null, StringLiteral(qname.Namespace))));
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
