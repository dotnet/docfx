// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Docfx.DataContracts.Common;

#nullable enable

public static class Constants
{
    public const string ConfigFileName = "docfx.json";
    public const string YamlExtension = ".yml";
    public const string ContentPlaceholder = "*content";
    public const string PrefixSeparator = ".";
    public const string TocYamlFileName = "toc.yml";

    public static class DocumentType
    {
        public const string Conceptual = "Conceptual";
        public const string Toc = "Toc";
        public const string ManagedReference = "ManagedReference";
        public const string Resource = "Resource";
        public const string Redirection = "Redirection";
    }

    public static class PropertyName
    {
        public const string Uid = "uid";
        public const string CommentId = "commentId";
        public const string Id = "id";
        public const string Href = "href";
        public const string Type = "type";
        public const string Source = "source";
        public const string Path = "path";
        public const string DocumentType = "documentType";
        public const string Title = "title";
        public const string TitleOverwriteH1 = "titleOverwriteH1";
        public const string Conceptual = "conceptual";
        public const string Documentation = "documentation";
        public const string Summary = "summary";
        public const string IsEii = "isEii";

        public const string Name = "name";
        public const string DisplayName = "displayName";
        public const string NameWithType = "nameWithType";
        public const string FullName = "fullName";
        public const string Content = "content";
        public const string TocHref = "tocHref";
        public const string TopicHref = "topicHref";
        public const string TopicUid = "topicUid";
        public const string Platform = "platform";
        public const string Parent = "parent";
        public const string Children = "children";
        public const string Namespace = "namespace";
        public const string Assemblies = "assemblies";
        public const string Overridden = "overridden";
        public const string Exceptions = "exceptions";
        public const string Inheritance = "inheritance";
        public const string DerivedClasses = "derivedClasses";
        public const string Implements = "implements";
        public const string InheritedMembers = "inheritedMembers";
        public const string ExtensionMethods = "extensionMethods";
        public const string Overload = "overload";
        public const string Return = "return";
        public const string SeeAlsoContent = "seealsoContent";
        public const string Syntax = "syntax";
        public const string AdditionalNotes = "additionalNotes";
        public const string SystemKeys = "_systemKeys";

        public const string OutputFileName = "outputFileName";

        public const string RedirectUrl = "redirect_url";
    }

    public static class MetadataName
    {
        public const string Version = "version";
    }

    public static class ExtensionMemberPrefix
    {
        public const string NameWithType = PropertyName.NameWithType + PrefixSeparator;
        public const string FullName = PropertyName.FullName + PrefixSeparator;
        public const string Name = PropertyName.Name + PrefixSeparator;
        public const string Spec = "spec" + PrefixSeparator;
        public const string Content = PropertyName.Content + PrefixSeparator;
        public const string Parent = PropertyName.Parent + PrefixSeparator;
        public const string Children = PropertyName.Children + PrefixSeparator;
        public const string Source = PropertyName.Source + PrefixSeparator;
        public const string Namespace = PropertyName.Namespace + PrefixSeparator;
        public const string Assemblies = PropertyName.Assemblies + PrefixSeparator;
        public const string Overridden = PropertyName.Overridden + PrefixSeparator;
        public const string Exceptions = PropertyName.Exceptions + PrefixSeparator;
        public const string Inheritance = PropertyName.Inheritance + PrefixSeparator;
        public const string DerivedClasses = PropertyName.DerivedClasses + PrefixSeparator;
        public const string Implements = PropertyName.Implements + PrefixSeparator;
        public const string InheritedMembers = PropertyName.InheritedMembers + PrefixSeparator;
        public const string ExtensionMethods = PropertyName.ExtensionMethods + PrefixSeparator;
        public const string Platform = PropertyName.Platform + PrefixSeparator;
        public const string Return = PropertyName.Return + PrefixSeparator;
        public const string Overload = PropertyName.Overload + PrefixSeparator;
    }

    public static class DevLang
    {
        public const string CSharp = "csharp";
        public const string VB = "vb";
    }

    public static class TableOfContents
    {
        public const string MarkdownTocFileName = "toc.md";
        public const string YamlTocFileName = "toc.yml";
    }

    public static class JsonSchemas
    {
        public const string Docfx = "schemas/docfx.schema.json";
        public const string Toc = "schemas/toc.schema.json";
        public const string XrefMap = "schemas/xrefmap.schema.json";
        public const string FilterConfig = "schemas/filterconfig.schema.json";
    }

    public static class EnvironmentVariables
    {
#pragma warning disable format
        public const string DOCFX_KEEP_DEBUG_INFO                      = nameof(DOCFX_KEEP_DEBUG_INFO);
        public const string DOCFX_NO_CHECK_CERTIFICATE_REVOCATION_LIST = nameof(DOCFX_NO_CHECK_CERTIFICATE_REVOCATION_LIST);
        public const string DOCFX_SOURCE_BRANCH_NAME                   = nameof(DOCFX_SOURCE_BRANCH_NAME);
#pragma warning restore format

#pragma warning disable format
        public static string? KeepDebugInfo                         => GetValue(DOCFX_KEEP_DEBUG_INFO);
        public static bool NoCheckCertificateRevocationList         => GetBooleanValue(DOCFX_NO_CHECK_CERTIFICATE_REVOCATION_LIST);
        public static string? SourceBranchName                      => GetValue(DOCFX_SOURCE_BRANCH_NAME);
#pragma warning restore format

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string? GetValue(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(value) ? null : value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetBooleanValue(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return bool.TryParse(value, out bool result) && result;
        }
    }

    public static class Switches
    {
        public const string DotnetToolMode = "Docfx.DotnetToolMode";

        public static bool IsDotnetToolsMode => AppContext.TryGetSwitch(DotnetToolMode, out bool isEnabled) && isEnabled;
    }
}
