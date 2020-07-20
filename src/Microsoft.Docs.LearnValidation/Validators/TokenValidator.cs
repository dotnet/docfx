// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.LearnValidation
{
    public class TokenValidator
    {
        private List<IValidateModel> _hierarchyItems;
        private readonly string _docsetFolder;
        private readonly string _dependencyMapFile;
        private readonly string _fallbackFolder;

        public TokenValidator(string dependencyMapFile, List<IValidateModel> hierarchyItems, string docsetFolder, string fallbackFolder)
        {
            _dependencyMapFile = dependencyMapFile;
            _hierarchyItems = hierarchyItems;
            _docsetFolder = docsetFolder;
            _fallbackFolder = fallbackFolder;
        }

        public bool Validate()
        {
            var isValid = true;

            var dependencyItems = DependencyMapHelper.LoadDependentFileInfo(_dependencyMapFile);

            var dependencyMapping = dependencyItems.Where(item => item.DependencyType == "include"
                && item.ToFilePath != item.FromFilePath + ".md" // not fragment 
                && (item.ToFilePath.EndsWith(".md") || item.ToFilePath.EndsWith(".yml")) // token extension 
                && (!item.ToFilePath.StartsWith(_fallbackFolder))) // not fallback
                .GroupBy(item => item.FromFilePath).ToDictionary(key => key.Key, value => value.Select(v=>v.ToFilePath).Distinct().ToList());

            // LearningPath will not check token, required from Bodhi
            // Refer to workitem https://ceapex.visualstudio.com/Engineering/_workitems/edit/64285/
            foreach (var hierarchyItem in _hierarchyItems.Where(hi => !(hi is PathValidateModel)))
            {
                var file = Path.Combine(_docsetFolder, hierarchyItem.SourceRelativePath).Replace('/', '\\');

                if (dependencyMapping.ContainsKey(file))
                {
                    foreach(var tokenDependency in dependencyMapping[file])
                    {
                        if (!File.Exists(tokenDependency))
                        {
                            var tokenRelativePath = tokenDependency.StartsWith(_docsetFolder) ?
                                "~" + tokenDependency.Substring(_docsetFolder.Length).Replace('\\', '/')
                                : tokenDependency;

                            Logger.Log(ErrorLevel.Error, ErrorCode.TripleCrown_Token_NotFound, filePath: hierarchyItem.SourceRelativePath);
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
