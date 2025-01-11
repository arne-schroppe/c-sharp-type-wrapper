using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
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

            public WrappedStructDescription(StructDeclarationSyntax structToAugment, string wrappedType, string ns)
            {
                StructToAugment = structToAugment;
                WrappedType = wrappedType;
                Namespace = ns;
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new TypeWrapAttributeSyntaxReceiver());
        }

        private readonly DiagnosticDescriptor _descriptor =
            new DiagnosticDescriptor("TypeWrapperGenMessage", "Test", "{0}", "TypeWrapper", DiagnosticSeverity.Warning,
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
            string namespaceClause =
                structDescription.Namespace == "" ? "" : $"namespace {structDescription.Namespace};";
            StructDeclarationSyntax structToAugment = structDescription.StructToAugment;
            string wrappedType = structDescription.WrappedType;
            string typeName = structToAugment.Identifier.Text;

            SourceText sourceText = SourceText.From($@"
            using System;
            {namespaceClause}

            partial struct {typeName} : IEquatable<{typeName}>
            {{
                public readonly {wrappedType} Value;
                public {typeName}({wrappedType} rawValue)
                {{
                    this.Value = rawValue;
                }}

                public bool Equals({typeName} other)
                {{
                    return Value.Equals(other.Value);
                }}

                public override bool Equals(object obj)
                {{
                    return obj is {typeName} other && Equals(other);
                }}

            }}", Encoding.UTF8);
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

                            break;
                        }
                    }
                }

                if (wrappedType == null)
                    return;

                WrappedStructDescriptions.Add(new WrappedStructDescription(s, wrappedType, namespaces));
            }
        }
    }
}