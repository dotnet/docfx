// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Linq;

    using Microsoft.DocAsCode.Common;

    public class IncrementalBuildContext
    {
        private DocumentBuildParameters _parameters;

        public string BaseDir { get; set; }

        public BuildVersionInfo CurrentBuildVersionInfo { get; set; }

        public BuildVersionInfo LastBuildVersionInfo { get; set; }

        public bool CanVersionIncremental { get; }

        public string Version
        {
            get
            {
                return _parameters.VersionName;
            }
        }

        public IncrementalBuildContext(DocumentBuildParameters parameters, BuildVersionInfo cbv, BuildVersionInfo lbv, bool canBuildInfoIncremental)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            _parameters = parameters;
            CurrentBuildVersionInfo = cbv;
            LastBuildVersionInfo = lbv;
            CanVersionIncremental = canBuildInfoIncremental && GetCanVersionIncremental();
        }

        private bool GetCanVersionIncremental()
        {
            if (LastBuildVersionInfo == null)
            {
                Logger.LogVerbose($"Cannot build incrementally because last build didn't contain version {Version}.");
                return false;
            }
            if (CurrentBuildVersionInfo.ConfigHash != LastBuildVersionInfo.ConfigHash)
            {
                Logger.LogVerbose("Cannot build incrementally because config changed.");
                return false;
            }
            if (_parameters.ForceRebuild)
            {
                Logger.LogVerbose($"Disable incremental build by force rebuild option.");
                return false;
            }
            return true;
        }
    }
}
