using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TypeWrapperGenerator
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
            public readonly Feature Features;

            public WrappedStructDescription(
                StructDeclarationSyntax structToAugment, 
                string wrappedType, string ns,
                bool isReadOnly, Feature features)
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
            new ("SWGenW001", "Missing readonly", "Struct should be readonly", "StructWrapper", DiagnosticSeverity.Warning,
                true);
        
        private readonly DiagnosticDescriptor _shouldNotBeReadOnlyDescriptor =
            new ("SWGenW002", "Unexpected readonly", "Struct cannot be readonly if it is also Serializable", "StructWrapper", DiagnosticSeverity.Warning,
                true);
        
        private readonly DiagnosticDescriptor _enclosingClassNotPartial =
            new ("SWGenE001", "Enclosing class not partial", "The enclosing class must be partial: {0}", "StructWrapper", DiagnosticSeverity.Error,
                true);
        
        private readonly DiagnosticDescriptor _infoDescriptor =
            new ("SWGenI000", "Info", "{0}", "StructWrapper", DiagnosticSeverity.Warning, true);

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
            bool hasNewtonSoftJson = (structDescription.Features & Feature.NewtonSoftJsonConverter) != 0;
            bool isSerializable = (structDescription.Features & Feature.UnitySerializable) != 0;
            
            if (!isReadOnly && !isSerializable)
            {
                context.ReportDiagnostic(Diagnostic.Create(_missingReadOnlyDescriptor,
                    structDescription.StructToAugment.GetLocation()));
            }
            
            if (isReadOnly && isSerializable)
            {
                context.ReportDiagnostic(Diagnostic.Create(_shouldNotBeReadOnlyDescriptor,
                    structDescription.StructToAugment.GetLocation()));
            }

            List<ClassDeclarationSyntax> enclosingClasses = GetParentsOfType<ClassDeclarationSyntax>(structToAugment);
            foreach (var enclosingClass in enclosingClasses)
            {
                if (!enclosingClass.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(_enclosingClassNotPartial, enclosingClass.GetLocation(), enclosingClass.Identifier.Text));
                    return;
                }
            }

            bool isGeneric = structToAugment.TypeParameterList != null;
            string typeParametersClause = "";
            if (isGeneric)
            {
                var parameters = structToAugment.TypeParameterList.Parameters.Select(p => p.Identifier.Text).ToList();
                typeParametersClause = "<" + string.Join(", ", parameters) + ">";
            }

            string outerTypeName = $"{structName}{typeParametersClause}";

            string enclosingClassesDeclarationsStart = "";
            string enclosingClassesDeclarationsEnd = "";
            enclosingClasses.Reverse();
            foreach (var enclosingClass in enclosingClasses)
            {
                enclosingClassesDeclarationsStart += $"partial class {enclosingClass.Identifier.Text} {{";
                enclosingClassesDeclarationsEnd += "}";
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
                        serializer.Serialize(writer, value._value);
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
            
            string serializableAttribute = "";
            string serializeFieldAttribute = "";
            string unityEngineImport = "";
            if (isSerializable)
            {
                serializableAttribute = "[Serializable]";
                serializeFieldAttribute = "[SerializeField]";
                unityEngineImport = "using UnityEngine;";
            }
            
            string stringConverterImport = "";
            string stringConverterAttribute = "";
            string stringConverterClass = "";
            bool needsStringConverter = hasNewtonSoftJson;
            if (needsStringConverter)
            {
                stringConverterImport = "using System.ComponentModel;\nusing System.Globalization;";
                stringConverterAttribute = $"[TypeConverter(typeof({structName}.StringTypeConverter))]";
                string convertFromImplementation;
                string convertToImplementation;

                // This prevents additional quotation marks around serialized dictionary keys
                if (wrappedType.ToLowerInvariant() == "string")
                {
                    convertFromImplementation = $@"
                        return JsonConvert.DeserializeObject<{outerTypeName}>($""\""{{(string)value}}\"""");
                    ";
                    convertToImplementation = $@"
                        return (({outerTypeName})value)._value;
                    ";
                }
                else
                {
                    convertFromImplementation = $@"
                        var wrapped = JsonConvert.DeserializeObject<{wrappedType}>((string)value);
                        return new {outerTypeName}(wrapped);
                    ";
                    convertToImplementation = $@"
                        return JsonConvert.SerializeObject(value);
                    ";
                }
                
                stringConverterClass = $@"
                public class StringTypeConverter : TypeConverter 
                {{
                    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) 
                    {{
                        return sourceType == typeof(string);
                    }}

                    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) 
                    {{
                        {convertFromImplementation}
                    }}

                    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {{
                        {convertToImplementation}
                    }}
                }}
                ";
            }
            
            SourceText sourceText = SourceText.From($@"
            #nullable disable
            using System;
            {unityEngineImport}
            {stringConverterImport}
            {newtonSoftJsonImport}
            {namespaceStart}

            {enclosingClassesDeclarationsStart}

            {newtonSoftJsonConverterAttribute}
            {stringConverterAttribute}
            {serializableAttribute}
            {readonlyClause} partial struct {outerTypeName} : IEquatable<{outerTypeName}>
            {{
                {serializeFieldAttribute}
                private {readonlyClause} {wrappedType} _value;
                public {wrappedType} Value => _value;

                partial void OnCreate(ref {wrappedType} newValue);

                public {structName}({wrappedType} rawValue)
                {{
                    OnCreate(ref rawValue);
                    _value = rawValue;
                }}

                public bool Equals({outerTypeName} other)
                {{
                    return _value.Equals(other.Value);
                }}

                public override bool Equals(object obj)
                {{
                    return obj is {outerTypeName} other && Equals(other);
                }}

                public override int GetHashCode()
                {{
                    return _value.GetHashCode();
                }}

                public static bool operator ==({outerTypeName} left, {outerTypeName} right)
                {{
                    return left.Equals(right);
                }}

                public static bool operator !=({outerTypeName} left, {outerTypeName} right)
                {{
                    return !left.Equals(right);
                }}

                public override string ToString() 
                {{
                    return $""[{structName}({{_value.ToString()}})]"";
                }}

                {newtonSoftJsonConverterClass}
                {stringConverterClass}
            }}

            {enclosingClassesDeclarationsEnd}
            {namespaceEnd}
            ", Encoding.UTF8);
            context.AddSource($"{structToAugment.Identifier.Text}.GeneratedWrapper.cs", sourceText);
        }

        private List<T> GetParentsOfType<T>(SyntaxNode node) where T : SyntaxNode
        {
            List<T> parents = new List<T>();
            var parent = node.Parent;
            while (parent != null)
            {
                if (typeof(T).IsAssignableFrom(parent.GetType()))
                {
                    parents.Add((T)parent);
                }
                parent = parent.Parent;
            }

            return parents;
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
                Feature features = Feature.None;
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
                                if (rawFeatures.Contains(nameof(Feature.NewtonSoftJsonConverter)))
                                {
                                    features |= Feature.NewtonSoftJsonConverter;
                                }
                                if (rawFeatures.Contains(nameof(Feature.UnitySerializable)))
                                {
                                    features |= Feature.UnitySerializable;
                                }
                            }

                            break;
                        }
                    }
                }

                if (wrappedType == null)
                    return;
                
                bool isReadOnly = s.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));

                WrappedStructDescriptions.Add(new WrappedStructDescription(s, wrappedType, namespaces, isReadOnly, features));
            }
        }
    }
}