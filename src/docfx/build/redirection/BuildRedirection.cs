// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal static class BuildRedirection
    {
        internal static List<Error> Build(Context context, Document file)
        {
            Debug.Assert(file.ContentType == ContentType.Redirection);

            var errors = new List<Error>();
            var (monikerError, monikers) = context.MonikerProvider.GetFileLevelMonikers(file.FilePath);
            errors.AddIfNotNull(monikerError);

            var publishItem = new PublishItem
            {
                Url = file.SiteUrl,
                SourcePath = file.FilePath.Path,
                Locale = context.LocalizationProvider.Locale,
                RedirectUrl = context.RedirectionProvider.GetRedirectUrl(file.FilePath),
                Monikers = monikers,
                MonikerGroup = MonikerUtility.GetGroup(monikers),
                ConfigMonikerRange = context.MonikerProvider.GetConfigMonikerRange(file.FilePath),
            };

            if (context.Config.Legacy)
            {
                publishItem.Path = context.DocumentProvider.GetOutputPath(file.FilePath, monikers);
            }

            if (context.PublishModelBuilder.TryAdd(file, publishItem) && context.Config.Legacy && !context.Config.DryRun)
            {
                var metadataPath = publishItem.Path.Substring(0, publishItem.Path.Length - ".raw.page.json".Length) + ".mta.json";
                var metadata = new
                {
                    locale = context.LocalizationProvider.Locale,
                    monikers,
                    redirect_url = publishItem.RedirectUrl,
                    is_dynamic_rendering = true,
                };

                // Note: produce an empty output to make publish happy
                context.Output.WriteText("{}", publishItem.Path);
                context.Output.WriteJson(metadata, metadataPath);
            }

            return errors;
        }
    }
}
