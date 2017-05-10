using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
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
        /// Mapping of XML namespace to CLR namespace.
        /// </summary>
        public IDictionary<XNamespace, string> XmlsToClrNamespace { get; } = new Dictionary<XNamespace, string>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal string GetTypeName(XmlSchemaType type)
        {
            switch (type)
            {
                case XmlSchemaComplexType t:
                    return GetTypeName(t);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the type name for the given complex type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal string GetTypeName(XmlSchemaComplexType type)
        {
            // name of the complex type
            // derived from the name set on the type itself, or its parent nested element
            var partialName = type.QualifiedName ?? (type.Parent is XmlSchemaElement e ? e.QualifiedName : null);

            // global type
            if (type.QualifiedName.IsEmpty == false)
                return ClrNamespaceForXmlns(type.QualifiedName.Namespace) + "." + type.QualifiedName.Name;

            // type nested under element
            if (type.Parent is XmlSchemaElement e)
            {
                if (e.Parent == null)
                var parentType = e.Parent.Recurse(i => i.Parent).OfType<XmlSchemaComplexType>().FirstOrDefault();
                if (parentType != null)
                    return GetTypeName(parentType) + "." + type.QualifiedName.Name;
                else
                    return ClrNamespaceForXmlns(e.QualifiedName.Namespace) + "." + e.QualifiedName.Name;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the type name for the given element.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        internal string GetTypeName(XmlSchemaElement element)
        {
            if (element.Parent is XmlSchema)
                return ClrNamespaceForXmlns(element.QualifiedName.Namespace) + "." + element.QualifiedName.Name;
            else if (element.RefName.IsEmpty == false)
                return GetTypeName(Schemas.GetGlobalElement(element.RefName));
            else
                return GetTypeName(element.ElementSchemaType);
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
        /// Gets the CLR namespace associated with the given XML namespace.
        /// </summary>
        /// <param name="xmlns"></param>
        /// <returns></returns>
        string ClrNamespaceForXmlns(XNamespace xmlns)
        {
            return XmlsToClrNamespace[xmlns];
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
        /// Gets the type of the given schema type.
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
                default:
                    throw new NotImplementedException();
            }

            return ParseTypeName(ClrNamespaceForXmlns(type.QualifiedName.Namespace) + "." + type.QualifiedName.Name);
        }

        /// <summary>
        /// Gets the type of the given element.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        TypeSyntax TypeOf(XmlSchemaElement element)
        {
            if (element.SchemaType != null)
                return TypeOf(element.SchemaType);
            else
                return ParseTypeName(ClrNamespaceForXmlns(element.QualifiedName.Namespace) + "." + element.Name + "Type");
        }

        /// <summary>
        /// Generates a new class for the given element.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public ClassDeclarationSyntax GenerateClassForElement(string identifier, XmlSchemaElement element)
        {
            switch (element.SchemaType)
            {
                case XmlSchemaComplexType complexType:
                    return GenerateClassForElementOfComplexType(identifier, element);
                case XmlSchemaSimpleType simpleType:
                    return GenerateClassForElementOfSimpleType(identifier, element);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Generates a new class for the given element with a complex type.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public ClassDeclarationSyntax GenerateClassForElementOfComplexType(string identifier, XmlSchemaElement element)
        {
            return GenerateClassForComplexType(identifier, (XmlSchemaComplexType)element.SchemaType, true);
        }

        /// <summary>
        /// Generates a new class for the given element with a complex type.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public ClassDeclarationSyntax GenerateClassForElementOfSimpleType(string identifier, XmlSchemaElement element)
        {
            throw new NotImplementedException();
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
                    List<AttributeListSyntax>()
                        .Add(AttributeList(SeparatedList<AttributeSyntax>().Add(GenerateGeneratedCodeAttribute())))
                        .Add(AttributeList(SeparatedList<AttributeSyntax>().Add(GenerateDebuggerStepThroughAttribute())))
                        .Add(AttributeList(SeparatedList<AttributeSyntax>().Add(GenerateXmlRootAttribute(type.QualifiedName, isElement))))
                        .Add(AttributeList(SeparatedList<AttributeSyntax>().Add(GenerateXmlTypeAttribute(type.QualifiedName, isElement)))))
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
                            GeneratePropertiesForComplexType(type)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        IEnumerable<MemberDeclarationSyntax> GeneratePropertiesForComplexType(XmlSchemaComplexType type)
        {
            foreach (XmlSchemaAttribute attribute in type.Attributes)
                yield return GeneratePropertyForAttribute(attribute);

            foreach (MemberDeclarationSyntax member in GeneratePropertiesForParticle(type.ContentTypeParticle))
                yield return member;
        }

        /// <summary>
        /// Generates a new property that corresponds with the given attribute.
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        MemberDeclarationSyntax GeneratePropertyForAttribute(XmlSchemaAttribute attribute)
        {
            return GenerateProperty(attribute.Name, attribute.SchemaType);
        }

        /// <summary>
        /// Generates a new property of the given schema type.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        MemberDeclarationSyntax GenerateProperty(string identifier, XmlSchemaType type)
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
        MemberDeclarationSyntax GeneratePropertyOfSimpleType(string identifier, XmlSchemaSimpleType type)
        {
            return PropertyDeclaration(TypeOf(type), identifier)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
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
        MemberDeclarationSyntax GeneratePropertyOfComplexType(string identifier, XmlSchemaComplexType type)
        {
            return PropertyDeclaration(TypeOf(type), identifier);
        }

        /// <summary>
        /// Generates new properties for the given XML object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        IEnumerable<MemberDeclarationSyntax> GeneratePropertiesForObject(XmlSchemaObject obj)
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
        IEnumerable<MemberDeclarationSyntax> GeneratePropertiesForParticle(XmlSchemaParticle particle)
        {
            switch (particle)
            {
                case XmlSchemaAll all:
                    throw new NotImplementedException();
                case XmlSchemaAny any:
                    throw new NotImplementedException();
                case XmlSchemaSequence sequence:
                    return GeneratePropertiesForSequence(sequence);
                case XmlSchemaElement element:
                    return GeneratePropertiesForElement(element);
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Generates new properties for the given element.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        IEnumerable<MemberDeclarationSyntax> GeneratePropertiesForElement(XmlSchemaElement element)
        {
            yield return GenerateProperty(element.QualifiedName.Name, element.ElementSchemaType);
        }

        /// <summary>
        /// Generates new properties for the given sequence.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        IEnumerable<MemberDeclarationSyntax> GeneratePropertiesForSequence(XmlSchemaSequence sequence)
        {
            foreach (XmlSchemaObject o in sequence.Items)
                foreach (var m in GeneratePropertiesForObject(o))
                    yield return m;
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
        AttributeSyntax GenerateXmlTypeAttribute(XmlQualifiedName qname, bool isElement)
        {
            return Attribute(IdentifierName(typeof(XmlTypeAttribute).FullName),
                AttributeArgumentList().AddArguments(
                    isElement ? AttributeArgument(NameEquals(IdentifierName(nameof(XmlTypeAttribute.TypeName))), null, StringLiteral(qname.Name)) : null,
                    AttributeArgument(NameEquals(IdentifierName(nameof(XmlTypeAttribute.Namespace))), null, StringLiteral(qname.Namespace))));
        }

        /// <summary>
        /// Generates the <see cref="XmlRootAttribute"/> annotation for the given <see cref="XmlQualifiedName"/>.
        /// </summary>
        /// <param name="qname"></param>
        /// <returns></returns>
        AttributeSyntax GenerateXmlRootAttribute(XmlQualifiedName qname, bool isElement)
        {
            return Attribute(IdentifierName(typeof(XmlRootAttribute).FullName),
                AttributeArgumentList().AddArguments(
                    isElement ? AttributeArgument(NameEquals(IdentifierName(nameof(XmlRootAttribute.ElementName))), null, StringLiteral(qname.Name)) : null,
                    AttributeArgument(NameEquals(IdentifierName(nameof(XmlRootAttribute.Namespace))), null, StringLiteral(qname.Namespace))));
        }

    }

}
