// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    public class IncrementalBuildContext
    {
        private DocumentBuildParameters _parameters;

        private Dictionary<string, Dictionary<string, BuildPhase?>> _modelLoadInfo = new Dictionary<string, Dictionary<string, BuildPhase?>>();

        public string BaseDir { get; set; }

        public string LastBaseDir { get; set; }

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

        internal IReadOnlyDictionary<string, Dictionary<string, BuildPhase?>> ModelLoadInfo
        {
            get
            {
                return _modelLoadInfo;
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

        #region Model Load Info

        /// <summary>
        /// report model load info
        /// </summary>
        /// <param name="hostService">host service</param>
        /// <param name="file">the model's LocalPathFromRoot</param>
        /// <param name="phase">the buildphase that the model was loaded at</param>
        internal void ReportModelLoadInfo(HostService hostService, string file, BuildPhase? phase)
        {
            Dictionary<string, BuildPhase?> mi = null;
            string name = hostService.Processor.Name;
            if (!_modelLoadInfo.TryGetValue(name, out mi))
            {
                _modelLoadInfo[name] = mi = new Dictionary<string, BuildPhase?>();
            }
            mi[file] = phase;
        }

        /// <summary>
        /// report model load info
        /// </summary>
        /// <param name="hostService">host service</param>
        /// <param name="files">models' LocalPathFromRoot</param>
        /// <param name="phase">the buildphase that the model was loaded at</param>
        internal void ReportModelLoadInfo(HostService hostService, IEnumerable<string> files, BuildPhase? phase)
        {
            foreach (var f in files)
            {
                ReportModelLoadInfo(hostService, f, phase);
            }
        }

        internal IReadOnlyDictionary<string, BuildPhase?> GetModelLoadInfo(HostService hostService)
        {
            string name = hostService.Processor.Name;
            Dictionary<string, BuildPhase?> mi;
            if (ModelLoadInfo.TryGetValue(name, out mi))
            {
                return mi;
            }
            return new Dictionary<string, BuildPhase?>();
        }

        #endregion

        #region Intermediate Files
        internal ModelManifest GetCurrentIntermediateModelManifest(HostService hostService)
        {
            return CurrentBuildVersionInfo?.Processors?.Find(p => p.Name == hostService.Processor.Name)?.IntermediateModelManifest;
        }

        internal ModelManifest GetLastIntermediateModelManifest(HostService hostService)
        {
            return LastBuildVersionInfo?.Processors?.Find(p => p.Name == hostService.Processor.Name)?.IntermediateModelManifest;
        }

        #endregion

        #region Private Methods

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

        #endregion
    }
}
