// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        private const string Category = "Naming";
        private const string Description = "Validate Error.cs file";
        private const string ShouldBeInterpolatedStringTitle = "ShouldBeInterpolatedStringTitle";
        private const string ShouldBeMemberAccessExpressionTitle = "Parameter should be member access";
        private const string ShouldBePlainStringTitle = "Parameter should be plain string";
        public static readonly DiagnosticDescriptor ShouldBeInterpolatedStringRule = new DiagnosticDescriptor(DiagnosticId, ShouldBeInterpolatedStringTitle, ShouldBeInterpolatedStringTitle, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        public static readonly DiagnosticDescriptor ShouldBeMemberAccessExpressionRule = new DiagnosticDescriptor(DiagnosticId, ShouldBeMemberAccessExpressionTitle, ShouldBeMemberAccessExpressionTitle, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        public static readonly DiagnosticDescriptor ShouldBePlainStringRule = new DiagnosticDescriptor(DiagnosticId, ShouldBePlainStringTitle, ShouldBePlainStringTitle, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(ShouldBeInterpolatedStringRule, ShouldBeMemberAccessExpressionRule, ShouldBePlainStringRule); } }

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
            // TODO only apply on Errors.cs
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
                    if (error.ArgumentList.Arguments[0].Expression is not MemberAccessExpressionSyntax level)
                    {
                        var diagnostic = Diagnostic.Create(ShouldBeMemberAccessExpressionRule, error.ArgumentList.Arguments[0].Expression.GetLocation());

                        context.ReportDiagnostic(diagnostic);
                    }

                    if (error.ArgumentList.Arguments[1].Expression is not LiteralExpressionSyntax code)
                    {
                        var diagnostic = Diagnostic.Create(ShouldBePlainStringRule, error.ArgumentList.Arguments[1].Expression.GetLocation());

                        context.ReportDiagnostic(diagnostic);
                    }

                    if (error.ArgumentList.Arguments[2].Expression is not InterpolatedStringExpressionSyntax msg)
                    {
                        var diagnostic = Diagnostic.Create(ShouldBeInterpolatedStringRule, error.ArgumentList.Arguments[2].Expression.GetLocation());

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
