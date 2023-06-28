// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DocAsCode.Build.Engine;

internal static class Constants
{
    /// <summary>
    /// TODO: how to handle multi-language
    /// </summary>
    public const string DefaultLanguage = "csharp";

    public const string ManifestFileName = "manifest.json";

    public static class OPSEnvironmentVariable
    {
        public const string SystemMetadata = "_op_systemMetadata";
        public const string OpPublishTargetSiteHostName = "_op_publishTargetSiteHostName";
    }
}
