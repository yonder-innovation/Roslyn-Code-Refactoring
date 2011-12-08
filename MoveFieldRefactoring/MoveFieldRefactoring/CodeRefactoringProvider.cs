using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Services;
using Roslyn.Services.Editor;

namespace MoveField
{
    [ExportCodeRefactoringProvider("MoveMember", LanguageNames.CSharp)]
    class CodeRefactoringProvider : ICodeRefactoringProvider
    {
        private readonly ICodeActionEditFactory editFactory;

        [ImportingConstructor]
        public CodeRefactoringProvider(ICodeActionEditFactory editFactory)
        {
            this.editFactory = editFactory;
        }

        public CodeRefactoring GetRefactoring(IDocument document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var syntaxTree = (SyntaxTree) document.GetSyntaxTree(cancellationToken);
            var token = syntaxTree.Root.FindToken(textSpan.Start);
            if (token.Parent==null)
            {
                return null;
            }

            var fds = token.Parent.FirstAncestorOrSelf<FieldDeclarationSyntax>();
            
            //check that we have a field
            if (fds == null)
                return null;

            // Retrieve the class declaration of the specified member.
            var theclass = fds.Parent as ClassDeclarationSyntax;

            // Find the parent class
            var parentClass = GetParentClass(syntaxTree, theclass);
            
            //we must have a parent class, otherwise we cannot move the member
            if (parentClass==null)
            {
                return null;
            }

            //trigger the refactoring
            return new CodeRefactoring(
                new[] { new CodeAction(editFactory, document, fds,parentClass) },
                fds.Span);
        }

        private ClassDeclarationSyntax GetParentClass(SyntaxTree tree, ClassDeclarationSyntax theclass)
        {
            if (theclass != null && theclass.BaseListOpt!=null)
            {
                var parent = theclass.BaseListOpt.GetLastToken();
                if (parent != null)
                {
                    var node = tree.Root.DescendentNodes().OfType<ClassDeclarationSyntax>().Where(p => p.Identifier.GetText() == parent.GetText()).First();
                    return node;
                }
            }


            return null;
        }
    }
}
