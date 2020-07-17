// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TripleCrownValidation
{
    public class TokenValidator
    {
        private List<IValidateModel> _hierarchyItems;
        private string _docsetFolder;
        private string _dependencyMapFile;
        private List<string> _fallbackFolders;

        public TokenValidator(string dependencyMapFile, List<IValidateModel> hierarchyItems, string docsetFolder, List<string> fallbackFolders)
        {
            _dependencyMapFile = dependencyMapFile;
            _hierarchyItems = hierarchyItems;
            _docsetFolder = docsetFolder;
            _fallbackFolders = fallbackFolders;
        }

        public bool Validate()
        {
            bool isValid = true;

            var dependencyItems = DependencyMapHelper.LoadDependentFileInfo(_dependencyMapFile);

            var dependencyMapping = dependencyItems.Where(item => item.DependencyType == "include"
                && item.ToFilePath != item.FromFilePath + ".md" // not fragment 
                && (item.ToFilePath.EndsWith(".md") || item.ToFilePath.EndsWith(".yml")) // token extension 
                && (_fallbackFolders == null || _fallbackFolders.All(ff => !item.ToFilePath.StartsWith(ff)))) // not fallback
                .GroupBy(item => item.FromFilePath).ToDictionary(key => key.Key, value => value.Select(v=>v.ToFilePath).Distinct().ToList());

            // LearningPath will not check token, required from Bodhi
            // Refer to workitem https://ceapex.visualstudio.com/Engineering/_workitems/edit/64285/
            foreach (var hierarchyItem in _hierarchyItems.Where(hi => !(hi is PathValidateModel)))
            {
                var file = Path.Combine(_docsetFolder, hierarchyItem.SourceRelativePath).BackSlashToForwardSlash();

                if (dependencyMapping.ContainsKey(file))
                {
                    foreach(var tokenDependency in dependencyMapping[file])
                    {
                        if (!File.Exists(tokenDependency))
                        {
                            var tokenRelativePath = tokenDependency.StartsWith(_docsetFolder) ?
                                "~" + tokenDependency.Substring(_docsetFolder.Length).ForwardSlashToBackSlash() 
                                : tokenDependency;

                            OPSLogger.LogUserError(LogCode.TripleCrown_Token_NotFound, LogMessageUtility.FormatMessage(LogCode.TripleCrown_Token_NotFound), hierarchyItem.SourceRelativePath);
                            hierarchyItem.IsValid = false;
                            isValid = false;
                        }
                    }
                }
            }

            return isValid;
        }
    }
}
