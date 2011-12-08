using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;
using System.Threading;
using System.Windows.Media;

namespace MoveField
{
    internal class CodeAction:ICodeAction
    {
        private readonly ICodeActionEditFactory editFactory;
        private readonly IDocument document;
        private readonly FieldDeclarationSyntax field;
        private readonly ClassDeclarationSyntax parentClass;

        public ImageSource Icon { get; private set; }
        public string Description { get; private set; }

        public CodeAction(ICodeActionEditFactory editFactory, IDocument document, FieldDeclarationSyntax member, ClassDeclarationSyntax parent)
        {
            this.editFactory = editFactory;
            this.document = document;
            this.field = member;
            this.parentClass = parent;

            this.Description = "Move member to parent class";
            this.Icon = null;
        }

        public ICodeActionEdit GetEdit(CancellationToken cancellationToken)
        {
            var tree = (SyntaxTree)document.GetSyntaxTree(cancellationToken);
            var semanticModel = (SemanticModel)document.GetSemanticModel(cancellationToken);

            var property = getProperty(field.Parent as ClassDeclarationSyntax,semanticModel);

            // Move member
            var fieldMover = new FieldMover(semanticModel, semanticModel.GetDeclaredSymbol(field), field, property, parentClass);
            //visit the current tree and store the root of the new modified tree
            var newRoot = fieldMover.Visit(tree.Root);

            //transform the code to match the new syntax tree
            return editFactory.CreateTreeTransformEdit(document.Project.Solution, tree, newRoot);
        }

        /// <summary>
        /// Checks if any property in the class given as parameter has a getter for the field we are moving.
        /// </summary>
        /// <param name="node">Syntax node of the class the field belongs to.</param>
        /// <param name="semanticModel">Semantic model for the code we are processing.</param>
        /// <returns></returns>
        private PropertyDeclarationSyntax getProperty(ClassDeclarationSyntax node, SemanticModel semanticModel)
        {
            foreach (var p in node.Members.Where(m => m.Kind == SyntaxKind.PropertyDeclaration))
            {
                var property = p as PropertyDeclarationSyntax;

                var accessors = property.AccessorList.Accessors;
                var getter = accessors.FirstOrDefault(ad => ad.Kind == SyntaxKind.GetAccessorDeclaration);

                var statements = getter.BodyOpt.Statements;
                if (statements.Count != 0)
                {
                    var returnStatement = statements.FirstOrDefault() as ReturnStatementSyntax;
                    if (returnStatement != null && returnStatement.ExpressionOpt != null)
                    {
                        var semanticInfo = document.GetSemanticModel().GetSemanticInfo(returnStatement.ExpressionOpt);
                        var fieldSymbol = semanticInfo.Symbol as FieldSymbol;

                        if (fieldSymbol != null && fieldSymbol == semanticModel.GetDeclaredSymbol(field))
                        {
                            return property;
                        }
                    }
                }
            }

            return null;
        }

        
    }
}
