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
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new TypeWrapAttributeSyntaxReceiver());
        }

        private readonly DiagnosticDescriptor _descriptor =
            new DiagnosticDescriptor("TypeWrapperGenMessage", "Test", "{0}", "TypeWrapper", DiagnosticSeverity.Warning, true);

        public void Execute(GeneratorExecutionContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(_descriptor, Location.None, "StructWrapper!"));
            TypeWrapAttributeSyntaxReceiver syntaxReceiver = (TypeWrapAttributeSyntaxReceiver)context.SyntaxReceiver;
            StructDeclarationSyntax userStruct = syntaxReceiver?.StructToAugment;
            string wrappedType = syntaxReceiver?.WrappedType;

            if (userStruct == null || wrappedType == null) return;

            string namespaceName = syntaxReceiver.Namespaces.First().Name.ToString();
            string namespaceClause = namespaceName == "" ? "" : $"namespace {namespaceName};";

            SourceText sourceText = SourceText.From($@"
            using System;
            {namespaceClause}

            partial struct {userStruct.Identifier.Text}
            {{
                public readonly {wrappedType} Value;
                public {userStruct.Identifier.Text}({wrappedType} rawValue)
                {{
                    this.Value = rawValue;
                }}

                public static void Wrap({wrappedType} rawValue) 
                {{
                }}

            }}", Encoding.UTF8);
            context.AddSource($"{userStruct.Identifier.Text}.GeneratedWrapper.cs", sourceText);
        }

        class TypeWrapAttributeSyntaxReceiver : ISyntaxReceiver
        {
            public StructDeclarationSyntax StructToAugment { get; private set; }
            public string WrappedType { get; private set; }
            public List<NamespaceDeclarationSyntax> Namespaces { get; private set; }

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is not StructDeclarationSyntax s)
                    return;

                if (s.Identifier.Text == "WrappedInt")
                {
                    StructToAugment = s;
                }
                else
                {
                    return;
                }
                
                Namespaces = s.SyntaxTree.GetRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToList();

                WrappedType = null;
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
                                WrappedType = typeOfExpression?.Type.ToString();
                            }
                            break;
                        }
                    }
                }

                if (WrappedType == null)
                    return;

                StructToAugment = s;
            }
        }
    }
}