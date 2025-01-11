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

        public void Execute(GeneratorExecutionContext context)
        {
            TypeWrapAttributeSyntaxReceiver syntaxReceiver = (TypeWrapAttributeSyntaxReceiver)context.SyntaxReceiver;
            ClassDeclarationSyntax userClass = syntaxReceiver?.ClassToAugment;
            string wrappedType = syntaxReceiver?.WrappedType;
            if (userClass == null || wrappedType == null) return;
            SourceText sourceText = SourceText.From($@"
            public partial struct {userClass.Identifier}
            {{
                public readonly {wrappedType} Value;   
                private void {userClass.Identifier}({wrappedType} rawValue)
                {{
                    this.Value = rawValue;
                }}
            }}", Encoding.UTF8);
            context.AddSource($"{userClass.Identifier.Text}.GeneratedWrapper.cs", sourceText);
        }

        class TypeWrapAttributeSyntaxReceiver : ISyntaxReceiver
        {
            public ClassDeclarationSyntax ClassToAugment { get; private set; }
            public string WrappedType { get; private set; }

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is not ClassDeclarationSyntax cds)
                    return;

                WrappedType = null;
                foreach (var attributeList in cds.AttributeLists)
                {
                    // TODO check if it has more than one Attribute 
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var attributeName = attribute.Name.ToString();
                        if (attributeName is "TypeWrapper" or "TypeWrapperAttribute")
                        {
                            WrappedType = attribute.ArgumentList.Arguments[0].Expression.ToString();
                            break;
                        }
                    }
                }

                if (WrappedType == null)
                    return;

                ClassToAugment = cds;
            }
        }
    }
}