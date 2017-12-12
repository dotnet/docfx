// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.UniversalReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.UniversalReference;
    using Microsoft.DocAsCode.Plugins;

    using AutoMapper;

    public static class ModelConverter
    {
        public static ApiBuildOutput ToApiBuildOutput(PageViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            if (model.Items == null || model.Items.Count == 0)
            {
                throw new ArgumentException($"{nameof(model)} must contain at least one item");
            }
            if (model.Items[0].SupportedLanguages == null || model.Items[0].SupportedLanguages.Length == 0)
            {
                throw new ArgumentException($"{nameof(ItemViewModel.SupportedLanguages)} must contain at least one language");
            }

            var supportedLanguages = model.Items[0].SupportedLanguages;

            // map references to ApiNames
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile(new ApiNamesProfile(supportedLanguages));
            });
            Mapper.Configuration.AssertConfigurationIsValid();
            Dictionary<string, ApiNames> references = null;
            if (model.References != null)
            {
                references = new Dictionary<string, ApiNames>();
                foreach (var reference in model.References
                    .Where(r => !string.IsNullOrEmpty(r.Uid))
                    .Select(Mapper.Map<ReferenceViewModel, ApiNames>))
                {
                    references[reference.Uid] = reference;
                }
            }

            // map references of children to ApiBuildOutput
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile(new ApiBuildOutputProfile(supportedLanguages, model.Metadata, references));
            });
            Mapper.Configuration.AssertConfigurationIsValid();
            var childUids = model.Items[0].Children ?? Enumerable.Empty<string>()
                .Concat(model.Items[0].ChildrenInDevLangs != null
                    ? model.Items[0].ChildrenInDevLangs.SelectMany(kv => kv.Value)
                    : Enumerable.Empty<string>())
                .Distinct();
            var children = new Dictionary<string, ApiBuildOutput>();
            if (model.References != null)
            {
                foreach (var reference in model.References
                    .Where(r => !string.IsNullOrEmpty(r.Uid) && childUids.Contains(r.Uid))
                    .Select(Mapper.Map<ReferenceViewModel, ApiBuildOutput>))
                {
                    children[reference.Uid] = reference;
                }
            }

            // map items to ApiBuildOutput
            var items = Mapper.Map<List<ItemViewModel>, List<ApiBuildOutput>>(model.Items);
            var result = items[0];
            foreach (var child in items.Skip(1).Where(r => !string.IsNullOrEmpty(r.Uid)))
            {
                children[child.Uid] = child;
            }

            // fill in children
            if (model.Items[0].Children != null || model.Items[0].ChildrenInDevLangs.Count > 0)
            {
                result.Children = ToApiListInDevLangs(model.Items[0].Children, model.Items[0].ChildrenInDevLangs, supportedLanguages)
                    ?.Select(pair =>
                    {
                        return new ApiLanguageValuePair<List<ApiBuildOutput>>
                        {
                            Language = pair.Language,
                            Value = pair.Value.Select(item => ResolveApiBuildOutput(item, children)).ToList()
                        };
                    }).ToList();
            }

            return result;
        }

        public static List<ApiLanguageValuePair<ApiNames>> ToApiListInDevLangsResolvingApiNames(string defaultValue, SortedList<string, string> values, string[] supportedLanguages, IReadOnlyDictionary<string, ApiNames> references)
        {
            if (defaultValue == null || supportedLanguages == null || supportedLanguages.Length == 0)
            {
                return null;
            }
            return ToApiListInDevLangs(defaultValue, values, supportedLanguages)
                ?.Select(pair =>
                {
                    return new ApiLanguageValuePair<ApiNames>
                    {
                        Language = pair.Language,
                        Value = ResolveApiNames(pair.Value, supportedLanguages, references)
                    };
                }).ToList();
        }

        public static List<ApiLanguageValuePair<List<ApiInheritanceTreeBuildOutput>>> ToApiListInDevlangsResolvingApiNames(List<InheritanceTree> defaultValue, SortedList<string, List<InheritanceTree>> values, string[] supportedLanguages, IReadOnlyDictionary<string, ApiNames> references)
        {
            if (defaultValue == null || supportedLanguages == null || supportedLanguages.Length == 0)
            {
                return null;
            }
            return ToApiListInDevLangs(defaultValue, values, supportedLanguages)
                ?.Select(pair =>
                {
                    return new ApiLanguageValuePair<List<ApiInheritanceTreeBuildOutput>>
                    {
                        Language = pair.Language,
                        Value = pair.Value.Select(item => ResolveInheritanceTree(item, supportedLanguages, references)).ToList()
                    };
                }).ToList();
        }

        public static List<ApiLanguageValuePair<List<ApiExceptionInfoBuildOutput>>> ToApiListInDevlangsResolvingApiNames(List<ExceptionInfo> defaultValue, SortedList<string, List<ExceptionInfo>> values, string[] supportedLanguages, IReadOnlyDictionary<string, ApiNames> references)
        {
            if (defaultValue == null || supportedLanguages == null || supportedLanguages.Length == 0 || references == null)
            {
                return null;
            }
            return ToApiListInDevLangs(defaultValue, values, supportedLanguages)
                ?.Select(pair =>
                {
                    return new ApiLanguageValuePair<List<ApiExceptionInfoBuildOutput>>
                    {
                        Language = pair.Language,
                        Value = pair.Value.Select(item =>
                        {
                            return new ApiExceptionInfoBuildOutput
                            {
                                Type = ResolveApiNames(item.Type, supportedLanguages, references),
                                Description = item.Description,
                                Metadata = item.Metadata
                            };
                        }).ToList()
                    };
                }).ToList();
        }

        public static List<ApiLanguageValuePair<ApiParameterBuildOutput>> ToApiListInDevLangsResolvingApiNames(ApiParameter defaultValue, SortedList<string, ApiParameter> values, string[] supportedLanguages, IReadOnlyDictionary<string, ApiNames> references)
        {
            if (defaultValue == null || supportedLanguages == null || supportedLanguages.Length == 0)
            {
                return null;
            }
            return ToApiListInDevLangs(defaultValue, values, supportedLanguages)
                ?.Select(pair =>
                {
                    return new ApiLanguageValuePair<ApiParameterBuildOutput>
                    {
                        Language = pair.Language,
                        Value = new ApiParameterBuildOutput
                        {
                            Name = pair.Value.Name,
                            Type = pair.Value.Type?.Select(item => ResolveApiNames(item, supportedLanguages, references)).ToList(),
                            Description = pair.Value.Description,
                            Optional = pair.Value.Optional,
                            DefaultValue = pair.Value.DefaultValue,
                            Metadata = pair.Value.Metadata
                        }
                    };
                }).ToList();
        }

        public static List<ApiLanguageValuePair<List<ApiNames>>> ToApiListInDevLangsResolvingApiNames(List<string> defaultValue, SortedList<string, List<string>> values, string[] supportedLanguages, IReadOnlyDictionary<string, ApiNames> references)
        {
            if (defaultValue == null || supportedLanguages == null || supportedLanguages.Length == 0 || references == null)
            {
                return null;
            }
            return ToApiListInDevLangs(defaultValue, values, supportedLanguages)
                ?.Select(pair =>
                {
                    return new ApiLanguageValuePair<List<ApiNames>>
                    {
                        Language = pair.Language,
                        Value = pair.Value.Select(item => ResolveApiNames(item, supportedLanguages, references)).ToList()
                    };
                }).ToList();
        }

        public static List<ApiLanguageValuePair<T>> ToApiListInDevLangs<T>(T defaultValue, SortedList<string, T> values, string[] supportedLanguages)
        {
            if (defaultValue == null || supportedLanguages == null || supportedLanguages.Length == 0)
            {
                return null;
            }

            var result = new List<ApiLanguageValuePair<T>>();
            values = values ?? new SortedList<string, T>();
            foreach (var language in supportedLanguages)
            {
                result.Add(new ApiLanguageValuePair<T>
                {
                    Language = language,
                    Value = values.ContainsKey(language) ? values[language] : defaultValue,
                });
            }

            return result;
        }

        public static ApiNames ResolveApiNames(string uid, string[] supportedLanguages, IReadOnlyDictionary<string, ApiNames> references)
        {
            if (references != null && references.TryGetValue(uid, out ApiNames result))
            {
                return result;
            }
            return new ApiNames
            {
                Uid = uid,
                Spec = supportedLanguages.Select(s => new ApiLanguageValuePair<string>
                {
                    Language = s,
                    Value = GetXref(uid)
                }).ToList()
            };
        }

        public static string GetXref(string uid, string text = null, string alt = null)
        {
            var result = $"<xref uid=\"{HttpUtility.HtmlEncode(uid)}\"";
            if (!string.IsNullOrEmpty(text))
            {
                result += $" text=\"{HttpUtility.HtmlEncode(text)}\"";
            }
            else
            {
                result += " displayProperty=\"name\"";
            }
            if (!string.IsNullOrEmpty(alt))
            {
                result += $" alt=\"{HttpUtility.HtmlEncode(alt)}\"";
            }
            else
            {
                result += " altProperty=\"fullName\"";
            }
            result += "/>";
            return result;
        }

        private static ApiInheritanceTreeBuildOutput ResolveInheritanceTree(InheritanceTree tree, string[] supportedLanguages, IReadOnlyDictionary<string, ApiNames> references)
        {
            return new ApiInheritanceTreeBuildOutput
            {
                Type = ResolveApiNames(tree.Type, supportedLanguages, references),
                Inheritance = tree.Inheritance?.Select(item => ResolveInheritanceTree(item, supportedLanguages, references)).ToList(),
                Metadata = tree.Metadata
            };
        }

        private static ApiBuildOutput ResolveApiBuildOutput(string uid, IReadOnlyDictionary<string, ApiBuildOutput> children)
        {
            if (!children.TryGetValue(uid, out ApiBuildOutput result))
            {
                var message = $"Can't find {uid} in items or references";
                Logger.LogError(message);
                throw new DocumentException(message);
            }
            return result;
        }
    }
}
