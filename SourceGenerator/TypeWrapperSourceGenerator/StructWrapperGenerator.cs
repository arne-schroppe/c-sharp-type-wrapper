using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TypeWrapperSourceGenerator
{
    [Generator]
    internal class StructWrapperGenerator : ISourceGenerator
    {
        private readonly struct WrappedStructDescription
        {
            public readonly StructDeclarationSyntax StructToAugment;
            public readonly string WrappedType;
            public readonly string Namespace;
            public readonly bool IsReadOnly;
            public readonly WrapperFeature Features;

            public WrappedStructDescription(
                StructDeclarationSyntax structToAugment, 
                string wrappedType, string ns,
                bool isReadOnly, WrapperFeature features)
            {
                StructToAugment = structToAugment;
                WrappedType = wrappedType;
                Namespace = ns;
                IsReadOnly = isReadOnly;
                Features = features;
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new TypeWrapAttributeSyntaxReceiver());
        }

        private readonly DiagnosticDescriptor _missingReadOnlyDescriptor =
            new DiagnosticDescriptor("SWGen001", "Missing readonly", "Struct should be readonly", "StructWrapper", DiagnosticSeverity.Warning,
                true);

        public void Execute(GeneratorExecutionContext context)
        {
            TypeWrapAttributeSyntaxReceiver syntaxReceiver = (TypeWrapAttributeSyntaxReceiver)context.SyntaxReceiver;
            if (syntaxReceiver == null) return;

            foreach (WrappedStructDescription structDescription in syntaxReceiver.WrappedStructDescriptions)
            {
                GenerateStructWrapper(structDescription, context);
            }
        }

        private void GenerateStructWrapper(WrappedStructDescription structDescription,
            GeneratorExecutionContext context)
        {
            bool isReadOnly = structDescription.IsReadOnly;
            StructDeclarationSyntax structToAugment = structDescription.StructToAugment;
            string wrappedType = structDescription.WrappedType;
            string structName = structToAugment.Identifier.Text;
            bool hasNewtonSoftJson = (structDescription.Features & WrapperFeature.NewtonSoftJsonConverter) != 0;
            
            if (!isReadOnly)
            {
                context.ReportDiagnostic(Diagnostic.Create(_missingReadOnlyDescriptor,
                    structDescription.StructToAugment.GetLocation()));
            }
            
            string readonlyClause = isReadOnly ? "readonly" : "";
            string namespaceStart =
                structDescription.Namespace == "" ? "" : $"namespace {structDescription.Namespace} {{";
            string namespaceEnd =
                structDescription.Namespace == "" ? "" : "}";
            
            string newtonSoftJsonImport = "";
            string newtonSoftJsonConverterAttribute = "";
            string newtonSoftJsonConverterClass = "";

            if (hasNewtonSoftJson)
            {
                newtonSoftJsonImport = "using Newtonsoft.Json;";
                newtonSoftJsonConverterAttribute = $"[JsonConverter(typeof({structName}.JsonConverter))]";
                newtonSoftJsonConverterClass = $@"
                public class JsonConverter : JsonConverter<{structName}>
                {{
                    public override void WriteJson(JsonWriter writer, {structName} value, JsonSerializer serializer)
                    {{
                        serializer.Serialize(writer, value.Value);
                    }}
        
                    public override {structName} ReadJson(JsonReader reader, Type objectType, {structName} existingValue,
                        bool hasExistingValue,
                        JsonSerializer serializer)
                    {{
                        {wrappedType} val = serializer.Deserialize<{wrappedType}>(reader);
                        return new {structName}(val);
                    }}
                }}";
            }

            SourceText sourceText = SourceText.From($@"
            using System;
            {newtonSoftJsonImport}
            {namespaceStart}

            {newtonSoftJsonConverterAttribute}
            {readonlyClause} partial struct {structName} : IEquatable<{structName}>
            {{
                public readonly {wrappedType} Value;
                public {structName}({wrappedType} rawValue)
                {{
                    this.Value = rawValue;
                }}

                public bool Equals({structName} other)
                {{
                    return Value.Equals(other.Value);
                }}

                public override bool Equals(object obj)
                {{
                    return obj is {structName} other && Equals(other);
                }}

                public override int GetHashCode()
                {{
                    return Value.GetHashCode();
                }}

                public static bool operator ==({structName} left, {structName} right)
                {{
                    return left.Equals(right);
                }}

                public static bool operator !=({structName} left, {structName} right)
                {{
                    return !left.Equals(right);
                }}

                public override string ToString() => $""[{structName}({{Value.ToString()}})]"";

                {newtonSoftJsonConverterClass}

            }}
            {namespaceEnd}
            ", Encoding.UTF8);
            context.AddSource($"{structToAugment.Identifier.Text}.GeneratedWrapper.cs", sourceText);
        }

        class TypeWrapAttributeSyntaxReceiver : ISyntaxReceiver
        {
            public List<WrappedStructDescription> WrappedStructDescriptions { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is not StructDeclarationSyntax s)
                    return;

                var namespaces = string.Join(".",
                    s.SyntaxTree.GetRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>()
                        .Select(ns => ns.Name.ToString()));

                string wrappedType = null;
                WrapperFeature wrapperFeatures = WrapperFeature.None;
                foreach (var attributeList in s.AttributeLists)
                {
                    // TODO check if it has more than one Attribute 
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var attributeName = attribute.Name.ToString();
                        if (attributeName is "TypeWrapper" or "TypeWrapperAttribute")
                        {
                            if (attribute.ArgumentList == null) continue;
                            var firstArg = attribute.ArgumentList.Arguments[0];
                            if (firstArg.Expression is TypeOfExpressionSyntax)
                            {
                                var typeOfExpression = firstArg.Expression as TypeOfExpressionSyntax;
                                wrappedType = typeOfExpression?.Type.ToString();
                            }

                            if (attribute.ArgumentList.Arguments.Count > 1)
                            {
                                var rawFeatures = attribute.ArgumentList.Arguments[1].Expression.GetText(Encoding.UTF8).ToString();
                                if (rawFeatures.Contains(nameof(WrapperFeature.NewtonSoftJsonConverter)))
                                {
                                    wrapperFeatures |= WrapperFeature.NewtonSoftJsonConverter;
                                }
                            }

                            break;
                        }
                    }
                }

                if (wrappedType == null)
                    return;
                
                bool isReadOnly = s.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));

                WrappedStructDescriptions.Add(new WrappedStructDescription(s, wrappedType, namespaces, isReadOnly, wrapperFeatures));
            }
        }
    }
}