using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.Editor;

namespace MoveField
{
    internal class FieldMover:SyntaxRewriter
    {
        private readonly SemanticModel semanticModel;
        private readonly ClassDeclarationSyntax parent;
        private readonly Symbol fieldSymbol;
        private readonly FieldDeclarationSyntax fds;
        private readonly PropertyDeclarationSyntax prop;

        public FieldMover(SemanticModel semanticModel, Symbol symbol, FieldDeclarationSyntax fds, PropertyDeclarationSyntax prop,  ClassDeclarationSyntax parent)
        {
            this.semanticModel = semanticModel;
            this.parent = parent;
            this.fieldSymbol = symbol;
            this.fds = fds;
            this.prop = prop;
        }

        protected override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax field)
        {
            // Retrieve the symbol for the field
            if (field.Declaration.Variables.Count == 1)
            {
                if (semanticModel.GetDeclaredSymbol(field) == fieldSymbol)
                {
                    //hide field from child class
                    return null;
                }
            }

            return field;
        }

        protected override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node == parent)
            {
                //show a pop-up with how the refactoring will look
                return CodeActionAnnotations.FormattingAnnotation.AddAnnotationTo(
                    MoveField(node));
            }
            return base.VisitClassDeclaration(node);
        }

        protected override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax propertyDeclaration)
        {
            //this will remove the property from the child class
            if (propertyDeclaration == prop)
            {
                return null;
            }

            return base.VisitPropertyDeclaration(propertyDeclaration);
        }

        private ClassDeclarationSyntax MoveField(ClassDeclarationSyntax node)
        { 

            //SyntaxList is immutable, so we need a regular list of members, to add our field to
            var list=node.Members.ToList<MemberDeclarationSyntax>();

            //change visibility to protected so that it can still be used in child classes
            var field = fds.Update(fds.Attributes,
                 Syntax.TokenList(Syntax.Token(SyntaxKind.ProtectedKeyword)),
                 fds.Declaration,
                 fds.SemicolonToken);
            //add field as the first member in the parent class
            list.Insert(0,field);

            //if a property for the field exists, move it to parent class as well
            if (prop!=null)
                list.Add(prop);

            //make a SyntaxList out of the list
            var syntaxlist = Syntax.List<MemberDeclarationSyntax>(list.ToArray<MemberDeclarationSyntax>());

            //"update" the class node - create another syntax tree with a changed node
            var newclass= node.Update( node.Attributes,
                node.Modifiers,
                node.Keyword,
                node.Identifier,
                node.TypeParameterListOpt,
                node.BaseListOpt,
                node.ConstraintClauses,
                node.OpenBraceToken,
                syntaxlist,
                node.CloseBraceToken,
                node.SemicolonTokenOpt);
            return newclass;
        }
    }
}
