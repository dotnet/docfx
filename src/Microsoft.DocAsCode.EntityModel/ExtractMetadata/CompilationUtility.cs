// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.ExtractMetadata
{
    using System;

    using Microsoft.CodeAnalysis;

    using CS = Microsoft.CodeAnalysis.CSharp;
    using VB = Microsoft.CodeAnalysis.VisualBasic;

    internal static class CompilationUtility
    {
        private static readonly Lazy<MetadataReference> MscorlibMetadataReference = new Lazy<MetadataReference>(() => MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        public static Compilation CreateCompilationFromCsharpCode(string code)
        {
            try
            {
                var tree = CS.SyntaxFactory.ParseSyntaxTree(code);
                var compilation = CS.CSharpCompilation.Create(
                    "cs.temp.dll",
                    options: new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    syntaxTrees: new[] { tree },
                    references: new[] { MscorlibMetadataReference.Value });
                return compilation;
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, $"Error generating compilation for C# code {GetAbbreviateString(code)}: {e.Message}. Ignored.");
                return null;
            }
        }

        public static Compilation CreateCompilationFromVBCode(string code)
        {
            try
            {
                var tree = VB.SyntaxFactory.ParseSyntaxTree(code);
                var compilation = VB.VisualBasicCompilation.Create(
                    "vb.temp.dll",
                    options: new VB.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                    syntaxTrees: new[] { tree },
                    references: new[] { MscorlibMetadataReference.Value });
                return compilation;
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, $"Error generating compilation for VB code {GetAbbreviateString(code)}: {e.Message}. Ignored.");
                return null;
            }
        }

        private static string GetAbbreviateString(string input, int length = 20)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= 20) return input;
            return input.Substring(0, length) + "...";
        }
    }
}
