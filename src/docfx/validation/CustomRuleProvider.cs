// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build;

internal class CustomRuleProvider
{
    private readonly Config _config;
    private readonly FileResolver _fileResolver;
    private readonly DocumentProvider _documentProvider;
    private readonly PublishUrlMap _publishUrlMap;
    private readonly MonikerProvider _monikerProvider;
    private readonly MetadataProvider _metadataProvider;
    private readonly ErrorBuilder _errors;

    private readonly Dictionary<string, List<CustomRule>> _customRules;

    public CustomRuleProvider(
        Config config,
        ErrorBuilder errors,
        FileResolver fileResolver,
        DocumentProvider documentProvider,
        PublishUrlMap publishUrlMap,
        MonikerProvider monikerProvider,
        MetadataProvider metadataProvider)
    {
        _config = config;
        _fileResolver = fileResolver;
        _documentProvider = documentProvider;
        _publishUrlMap = publishUrlMap;
        _monikerProvider = monikerProvider;
        _metadataProvider = metadataProvider;
        _errors = errors;

        _customRules = LoadCustomRules();
    }

    public bool IsEnable(FilePath filePath, CustomRule customRule, string? moniker = null)
    {
        if (customRule.ContentTypes != null && !customRule.ContentTypes.Contains(_documentProvider.GetPageType(filePath)))
        {
            return false;
        }

        if (customRule.CanonicalVersionOnly && !IsCanonicalVersion(filePath, moniker))
        {
            return false;
        }

        if (customRule.Tags != null
            && customRule.Tags.Contains("SEO")
            && _metadataProvider.GetMetadata(ErrorBuilder.Null, filePath).NoIndex())
        {
            return false;
        }

        return true;
    }

    public Error ApplyCustomRule(Error error)
    {
        return TryGetCustomRule(error, out var customRule) ? ApplyCustomRule(error, customRule) : error;
    }

    public static Error ApplyCustomRule(Error error, CustomRule customRule, bool? enabled = null)
    {
        var level = customRule.Severity ?? error.Level;

        // only filter for build rule since metadata & content rules will not generate error when disabled
        if (customRule.Disabled)
        {
            level = ErrorLevel.Off;
        }

        if (level != ErrorLevel.Off && customRule.ExcludeMatches(error.OriginalPath ?? error.Source?.File?.Path ?? ""))
        {
            level = ErrorLevel.Off;
        }

        if (enabled != null && !enabled.Value)
        {
            level = ErrorLevel.Off;
        }

        var message = error.Message;

        if (!string.IsNullOrEmpty(customRule.Message))
        {
            try
            {
                message = string.Format(customRule.Message, error.MessageArguments);
            }
            catch (FormatException)
            {
                message += "ERROR: custom message format is invalid, e.g., too many parameters {n}.";
            }
        }

        message = string.IsNullOrEmpty(customRule.AdditionalMessage) ?
            message : $"{message}{(message.EndsWith('.') ? "" : ".")} {customRule.AdditionalMessage}";

        return error with
        {
            Level = level,
            Code = string.IsNullOrEmpty(customRule.Code) ? error.Code : customRule.Code,
            Message = message,
            PullRequestOnly = customRule.PullRequestOnly,
        };
    }

    private bool TryGetCustomRule(Error error, [MaybeNullWhen(false)] out CustomRule customRule)
    {
        if (_customRules.TryGetValue(error.Code, out var customRules))
        {
            foreach (var rule in customRules)
            {
                if (rule.PropertyPath != null)
                {
                    // compare with code + propertyPath + contentType
                    var source = error.Source?.File;
                    var pageType = source != null ? _documentProvider.GetPageType(source) : null;
                    var isPageTypeInScope =
                        pageType == null ||
                        rule.ContentTypes == null ||
                        rule.ContentTypes.Contains(pageType);
                    if (rule.PropertyPath == error.PropertyPath && isPageTypeInScope)
                    {
                        customRule = rule;
                        return true;
                    }
                    continue;
                }
                else
                {
                    customRule = rule; // system error
                    return true;
                }
            }
        }
        customRule = null;
        return false;
    }

    private bool IsCanonicalVersion(FilePath filePath, string? moniker)
    {
        var canonicalVersion = _publishUrlMap.GetCanonicalVersion(filePath);

        // If content versioning not enabled for this depot, canonicalVersion will be null, content will always be the canonical version;
        // If content versioning enabled and moniker is null, we should check file-level monikers to be sure;
        // If content versioning enabled and moniker is not null, just compare canonicalVersion and moniker.
        if (string.IsNullOrEmpty(canonicalVersion))
        {
            return true;
        }

        if (string.IsNullOrEmpty(moniker))
        {
            return _monikerProvider.GetFileLevelMonikers(ErrorBuilder.Null, filePath).IsCanonicalVersion(canonicalVersion);
        }

        return canonicalVersion == moniker;
    }

    private Dictionary<string, List<CustomRule>> LoadCustomRules()
    {
        var contentValidationRules = GetValidationRules(_config.MarkdownValidationRules);
        var buildValidationRules = GetValidationRules(_config.BuildValidationRules);

        var customRules = _config.Rules.ToDictionary(item => item.Key, item => new List<CustomRule> { item.Value })
            ?? new Dictionary<string, List<CustomRule>>();

        if (contentValidationRules != null)
        {
            foreach (var validationRule in contentValidationRules.SelectMany(rules => rules.Value.Rules).Where(rule => !rule.DocfxOverride))
            {
                if (customRules.ContainsKey(validationRule.Code))
                {
                    _errors.Add(Errors.Logging.RuleOverrideInvalid(validationRule.Code));
                    customRules.Remove(validationRule.Code);
                }
            }
            foreach (var validationRule in contentValidationRules.SelectMany(rules => rules.Value.Rules).Where(rule => rule.PullRequestOnly))
            {
                if (customRules.TryGetValue(validationRule.Code, out var customRule))
                {
                    customRules[validationRule.Code] = new List<CustomRule>
                        {
                            customRule.First() with { PullRequestOnly = validationRule.PullRequestOnly },
                        };
                }
                else
                {
                    var list = new List<CustomRule>
                        {
                            new CustomRule { PullRequestOnly = validationRule.PullRequestOnly },
                        };

                    customRules.Add(validationRule.Code, list);
                }
            }
        }

        if (buildValidationRules != null)
        {
            foreach (var validationRule in buildValidationRules.SelectMany(rules => rules.Value.Rules))
            {
                var oldCode = ConvertTypeToCode(validationRule.Type);
                var newRule = new CustomRule
                {
                    Severity = ConvertSeverity(validationRule.Severity),
                    Code = validationRule.Code,
                    Message = validationRule.Message,
                    AdditionalMessage = validationRule.AdditionalErrorMessage,
                    PropertyPath = validationRule.PropertyPath,
                    CanonicalVersionOnly = validationRule.CanonicalVersionOnly,
                    PullRequestOnly = validationRule.PullRequestOnly,
                    ContentTypes = validationRule.ContentTypes,
                    Disabled = validationRule.Disabled,
                };

                // won't override docfx custom rules
                if (!customRules.ContainsKey(oldCode))
                {
                    customRules.Add(oldCode, new List<CustomRule> { newRule });
                }
                else
                {
                    customRules[oldCode].Add(newRule); // append
                }
            }
        }

        return customRules;
    }

    // MissingAttribute -> missing-attribute
    private static string ConvertTypeToCode(string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x.ToString() : x.ToString())).ToLowerInvariant();
    }

    private static ErrorLevel ConvertSeverity(ValidationSeverity severity)
    {
        return severity switch
        {
            ValidationSeverity.SUGGESTION => ErrorLevel.Suggestion,
            ValidationSeverity.WARNING => ErrorLevel.Warning,
            ValidationSeverity.ERROR => ErrorLevel.Error,
            _ => ErrorLevel.Info,
        };
    }

    private Dictionary<string, ValidationRules>? GetValidationRules(SourceInfo<string> rules)
    {
        try
        {
            return !string.IsNullOrEmpty(rules.Value)
                ? JsonUtility.DeserializeData<Dictionary<string, ValidationRules>>(_fileResolver.ReadString(rules), rules.Source?.File)
                : null;
        }
        catch (Exception ex)
        {
            Log.Write(ex);
            _errors.Add(Errors.System.ValidationIncomplete());
            return null;
        }
    }
}
