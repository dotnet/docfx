using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Docs.Build.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BuildErrorsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "BuildErrorsAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.MustBeExpressionTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.MustBeExpressionFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor ParameterRule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(ParameterRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxTreeAction(AnalyzeTree);
        }

        private void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetRoot();
            var errorClasses = from c in root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                               where c.Identifier.ValueText != "Errors" // exclude root class
                               select c;
            foreach (var errorClass in errorClasses)
            {
                var classDict = new Dictionary<string, Dictionary<string, string>>();
                var newErrors = from newError in errorClass.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                                where newError.Type.ToString() == "Error"
                                select newError;
                foreach (var error in newErrors)
                {
                    if (error.ArgumentList.Arguments[1].Expression is not InterpolatedStringExpressionSyntax msg)
                    {
                        var diagnostic = Diagnostic.Create(ParameterRule, error.ArgumentList.Arguments[1].Expression.GetLocation());

                        context.ReportDiagnostic(diagnostic);

                        continue;
                    }
                }
            }
        }
    }
}
