// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;

    internal static class UniversalReferenceConstants
    {
        public const string UniversalReference = "UniversalReference";
        public const string UniversalReferenceYamlMime = YamlMime.YamlMimePrefix + UniversalReference;

        public static class PropertyName
        {
            public const string Returns = "returns";
        }

        public static class ExtensionMemberPrefix
        {
            public const string Parent = Constants.PropertyName.Parent + Constants.PrefixSeparator;
            public const string Children = Constants.PropertyName.Children + Constants.PrefixSeparator;
            public const string Source = Constants.PropertyName.Source + Constants.PrefixSeparator;
            public const string Namespace = Constants.PropertyName.Namespace + Constants.PrefixSeparator;
            public const string Assemblies = Constants.PropertyName.Assemblies + Constants.PrefixSeparator;
            public const string Overridden = Constants.PropertyName.Overridden + Constants.PrefixSeparator;
            public const string Exceptions = Constants.PropertyName.Exceptions + Constants.PrefixSeparator;
            public const string Inheritance = Constants.PropertyName.Inheritance + Constants.PrefixSeparator;
            public const string DerivedClasses = Constants.PropertyName.DerivedClasses + Constants.PrefixSeparator;
            public const string Implements = Constants.PropertyName.Implements + Constants.PrefixSeparator;
            public const string InheritedMembers = Constants.PropertyName.InheritedMembers + Constants.PrefixSeparator;
            public const string ExtensionMethods = Constants.PropertyName.ExtensionMethods + Constants.PrefixSeparator;
            public const string Platform = Constants.PropertyName.Platform + Constants.PrefixSeparator;
            public const string Returns = PropertyName.Returns + Constants.PrefixSeparator;
            public const string Overload = Constants.PropertyName.Overload + Constants.PrefixSeparator;
        }
    }
}
