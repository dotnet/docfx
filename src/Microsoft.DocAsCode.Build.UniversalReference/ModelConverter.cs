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
            Dictionary<string, ApiNames> references = null;
            if (model.References != null)
            {
                references = new Dictionary<string, ApiNames>();
                foreach (var reference in model.References
                    .Where(r => !string.IsNullOrEmpty(r.Uid))
                    .Select(ToReferenceApiNames))
                {
                    references[reference.Uid] = reference;
                }
            }

            // map references of children to ApiBuildOutput
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
                    .Select(ToReferenceApiBuildOutput))
                {
                    children[reference.Uid] = reference;
                }
            }

            // map items to ApiBuildOutput
            var items = model.Items.Select(ToItemApiBuildOutput).ToList();
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

            ApiNames ToReferenceApiNames(ReferenceViewModel src)
            {
                return new ApiNames
                {
                    Uid = src.Uid,
                    Name = ToApiListInDevLangs(src.Name, src.NameInDevLangs, supportedLanguages),
                    NameWithType = ToApiListInDevLangs(src.NameWithType, src.NameWithTypeInDevLangs, supportedLanguages),
                    FullName = ToApiListInDevLangs(src.FullName, src.FullNameInDevLangs, supportedLanguages),
                    Definition = src.Definition,
                    Spec = ToSpec(),
                    Metadata = src.Additional,
                };

                List<ApiLanguageValuePair<string>> ToSpec()
                {
                    var result = new List<ApiLanguageValuePair<string>>();
                    var specs = src.Specs;
                    foreach (var language in supportedLanguages)
                    {
                        if (specs?.ContainsKey(language) == true)
                        {
                            result.Add(new ApiLanguageValuePair<string>
                            {
                                Language = language,
                                Value = GetSpecName(specs[language])
                            });
                        }
                        else
                        {
                            result.Add(new ApiLanguageValuePair<string>
                            {
                                Language = language,
                                Value = GetXref(src.Uid, src.Name)
                            });
                        }
                    }
                    return result;
                }

                static string GetSpecName(List<SpecViewModel> spec)
                {
                    return spec == null ? null : string.Concat(spec.Select(GetCompositeName));
                }

                static string GetCompositeName(SpecViewModel svm)
                {
                    // If href does not exists, return full name
                    if (string.IsNullOrEmpty(svm.Uid)) { return HttpUtility.HtmlEncode(svm.FullName); }

                    // If href exists, return name with href
                    return GetXref(svm.Uid, svm.Name);
                }
            }

            ApiBuildOutput ToReferenceApiBuildOutput(ReferenceViewModel src)
            {
                var dest = new ApiBuildOutput
                {
                    Uid = src.Uid,
                    CommentId = src.CommentId,
                    Href = src.Href,
                    Name = ToApiListInDevLangs(src.Name, src.NameInDevLangs, supportedLanguages),
                    NameWithType = ToApiListInDevLangs(src.NameWithType, src.NameWithTypeInDevLangs, supportedLanguages),
                    FullName = ToApiListInDevLangs(src.FullName, src.FullNameInDevLangs, supportedLanguages),
                    Metadata = src.Additional,
                };

                if (dest.Metadata.TryGetValue(Constants.PropertyName.Syntax, out object syntax))
                {
                    dest.Syntax = ToApiSyntaxBuildOutput(syntax as SyntaxDetailViewModel);
                    dest.Metadata.Remove(Constants.PropertyName.Syntax);
                }
                if (dest.Metadata.TryGetValue(Constants.PropertyName.Type, out object type))
                {
                    dest.Type = type as string;
                    dest.Metadata.Remove(Constants.PropertyName.Type);
                }
                if (dest.Metadata.TryGetValue(Constants.PropertyName.Summary, out object summary))
                {
                    dest.Summary = summary as string;
                    dest.Metadata.Remove(Constants.PropertyName.Summary);
                }
                if (dest.Metadata.TryGetValue(Constants.PropertyName.Platform, out object platform))
                {
                    dest.Platform = ToApiListInDevLangs(platform as List<string>, null, supportedLanguages);
                    dest.Metadata.Remove(Constants.PropertyName.Platform);
                }

                return dest;
            }

            ApiSyntaxBuildOutput ToApiSyntaxBuildOutput(SyntaxDetailViewModel src)
            {
                if (src is null)
                    return null;

                return new ApiSyntaxBuildOutput
                {
                    Content = ToApiListInDevLangs(src.Content, src.Contents, supportedLanguages),
                    Return = ToApiListInDevLangsResolvingApiNames(src.Return, src.ReturnInDevLangs, supportedLanguages, references),
                    Parameters = ToApiParametersBuildOutput(src.Parameters),
                    TypeParameters = ToApiParametersBuildOutput(src.TypeParameters),
                };
            }

            List<ApiParameterBuildOutput> ToApiParametersBuildOutput(List<ApiParameter> src)
            {
                return src?.Select(p => new ApiParameterBuildOutput
                {
                    Name = p.Name,
                    Type = p.Type?.Select(ToApiNames).ToList(),
                    Description = p.Description,
                    Optional = p.Optional,
                    DefaultValue = p.DefaultValue,
                    Metadata = p.Metadata,
                }).ToList();
            }

            ApiNames ToApiNames(string src)
            {
                return ResolveApiNames(src, supportedLanguages, references);
            }

            ApiBuildOutput ToItemApiBuildOutput(ItemViewModel src)
            {
                return new ApiBuildOutput
                {
                    Uid = src.Uid,
                    CommentId = src.CommentId,
                    Parent = ToApiListInDevLangsResolvingApiNames(src.Parent, src.ParentInDevLangs, supportedLanguages, references),
                    Package = ToApiListInDevLangsResolvingApiNames(src.Package, src.PackageInDevLangs, supportedLanguages, references),

                    Href = src.Href,
                    SupportedLanguages = src.SupportedLanguages,

                    Name = ToApiListInDevLangs(src.Name, src.Names, supportedLanguages),
                    NameWithType = ToApiListInDevLangs(src.NameWithType, src.NamesWithType, supportedLanguages),
                    FullName = ToApiListInDevLangs(src.FullName, src.FullNames, supportedLanguages),
                    Type = src.Type,
                    Source = ToApiListInDevLangs(src.Source, src.SourceInDevLangs, supportedLanguages),
                    Documentation = src.Documentation,
                    AssemblyNameList = ToApiListInDevLangs(src.AssemblyNameList, src.AssemblyNameListInDevLangs, supportedLanguages),
                    NamespaceName = ToApiListInDevLangsResolvingApiNames(src.NamespaceName, src.NamespaceNameInDevLangs, supportedLanguages, references),
                    Summary = src.Summary,
                    Remarks = src.Remarks,
                    Examples = src.Examples,
                    Syntax = ToApiSyntaxBuildOutput(src.Syntax),

                    Overridden = ToApiListInDevLangsResolvingApiNames(src.Overridden, src.OverriddenInDevLangs, supportedLanguages, references),
                    Overload = src.Overload is null ? null : supportedLanguages.Select(l => new ApiLanguageValuePair<ApiNames>
                    {
                        Language = l,
                        Value = ToApiNames(src.Overload)
                    }).ToList(),

                    Exceptions = ToApiListInDevlangsResolvingApiNames(src.Exceptions, src.ExceptionsInDevLangs, supportedLanguages, references),

                    SeeAlsos = src.SeeAlsos?.Select(ToApiLinkInfoBuildOutput).ToList(),
                    SeeAlsoContent = src.SeeAlsoContent,
                    Sees = src.Sees?.Select(ToApiLinkInfoBuildOutput).ToList(),

                    Inheritance = ToApiListInDevLangsResolvingApiNames(src.Inheritance, src.InheritanceInDevLangs, supportedLanguages, references),
                    DerivedClasses = ToApiListInDevLangsResolvingApiNames(src.DerivedClasses, src.DerivedClassesInDevLangs, supportedLanguages, references),
                    Implements = ToApiListInDevLangsResolvingApiNames(src.Implements, src.ImplementsInDevLangs, supportedLanguages, references),
                    InheritedMembers = ToApiListInDevLangsResolvingApiNames(src.InheritedMembers, src.InheritedMembersInDevLangs, supportedLanguages, references),
                    ExtensionMethods = ToApiListInDevLangsResolvingApiNames(src.ExtensionMethods, src.ExtensionMethodsInDevLangs, supportedLanguages, references),

                    Conceptual = src.Conceptual,

                    Platform = ToApiListInDevLangs(src.Platform, src.PlatformInDevLangs, supportedLanguages),
                    Metadata = model.Metadata?.Concat(src.Metadata.Where(p => !model.Metadata.Keys.Contains(p.Key))).ToDictionary(p => p.Key, p => p.Value) ?? src.Metadata,
                };
            }

            ApiLinkInfoBuildOutput ToApiLinkInfoBuildOutput(LinkInfo src)
            {
                return new ApiLinkInfoBuildOutput
                {
                    LinkType = src.LinkType,
                    Type = src.LinkType == LinkType.CRef ? ToApiNames(src.LinkId) : null,
                    Url = src.LinkType == LinkType.HRef ? ToUrl(src) : null,
                };

                static string ToUrl(LinkInfo source)
                {
                    var href = $"<span><a href=\"{HttpUtility.HtmlEncode(source.LinkId)}\">";
                    if (!string.IsNullOrEmpty(source.AltText))
                    {
                        href += HttpUtility.HtmlEncode(source.AltText);
                    }
                    href += "</a></span>";
                    return href;
                }
            }
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

        public static List<ApiLanguageValuePairWithLevel<List<ApiInheritanceTreeBuildOutput>>> ToApiListInDevLangsResolvingApiNames(List<InheritanceTree> defaultValue, SortedList<string, List<InheritanceTree>> values, string[] supportedLanguages, IReadOnlyDictionary<string, ApiNames> references)
        {
            if (defaultValue == null || supportedLanguages == null || supportedLanguages.Length == 0)
            {
                return null;
            }
            return ToApiListInDevLangs(defaultValue, values, supportedLanguages)
                ?.Select(pair =>
                {
                    var maxDepth = CalculateInheritanceDepth(pair.Value);
                    return new ApiLanguageValuePairWithLevel<List<ApiInheritanceTreeBuildOutput>>
                    {
                        Language = pair.Language,
                        Value = pair.Value.Select(item => ResolveInheritanceTree(item, supportedLanguages, references, 0, maxDepth)).ToList(),
                        Level = maxDepth
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

        public static string GetXref(string uid, string text = null)
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
            result += "/>";
            return result;
        }

        private static int CalculateInheritanceDepth(InheritanceTree tree)
        {
            return CalculateInheritanceDepth(tree.Inheritance) + 1;
        }

        private static int CalculateInheritanceDepth(List<InheritanceTree> trees)
        {
            return trees == null ? 0 : trees.Max(CalculateInheritanceDepth);
        }

        private static ApiInheritanceTreeBuildOutput ResolveInheritanceTree(InheritanceTree tree, string[] supportedLanguages, IReadOnlyDictionary<string, ApiNames> references, int depth, int maxDepth)
        {
            return new ApiInheritanceTreeBuildOutput
            {
                Type = ResolveApiNames(tree.Type, supportedLanguages, references),
                Inheritance = tree.Inheritance?.Select(item => ResolveInheritanceTree(item, supportedLanguages, references, depth + 1, maxDepth)).ToList(),
                Level = maxDepth - depth - 1,
                Metadata = tree.Metadata
            };
        }

        private static ApiBuildOutput ResolveApiBuildOutput(string uid, IReadOnlyDictionary<string, ApiBuildOutput> children)
        {
            if (children.TryGetValue(uid, out ApiBuildOutput result))
            {
                return result;
            }

            var message = $"Can't find {uid} in items or references";
            Logger.LogError(message, code: ErrorCodes.Build.InternalUidNotFound);
            throw new DocumentException(message);
        }
    }
}
