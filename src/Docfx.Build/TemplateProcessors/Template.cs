// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

using Docfx.Common;
using Newtonsoft.Json.Linq;

namespace Docfx.Build.Engine;

public class Template
{
    private const string Primary = ".primary";
    private const string Auxiliary = ".aux";

    public string Name { get; }
    public string ScriptName { get; }
    public string Extension { get; }
    public string Type { get; }
    public TemplateType TemplateType { get; }
    public IEnumerable<TemplateResourceInfo> Resources { get; }
    public bool ContainsGetOptions { get; }
    public bool ContainsModelTransformation { get; }

    public ITemplateRenderer Renderer { get; }
    public ITemplatePreprocessor Preprocessor { get; }

    public Template(ITemplateRenderer renderer, ITemplatePreprocessor preprocessor)
    {
        if (renderer == null && preprocessor == null)
        {
            throw new ArgumentNullException(nameof(preprocessor), "Both renderer and preprocessor are null");
        }

        Renderer = renderer;
        Preprocessor = preprocessor;

        Name = renderer?.Name ?? preprocessor?.Name;
        ScriptName = preprocessor?.Path;

        var templateInfo = GetTemplateInfo(Name);
        Extension = templateInfo.Extension;
        Type = templateInfo.DocumentType;
        TemplateType = templateInfo.TemplateType;

        ContainsGetOptions = Preprocessor?.ContainsGetOptions == true;
        ContainsModelTransformation = Preprocessor?.ContainsModelTransformation == true;

        Resources = ExtractDependentResources(Name);

        if (Renderer == null && !ContainsGetOptions && !ContainsModelTransformation)
        {
            Logger.LogWarning($"Template {Name} contains neither preprocessor to process model nor template to render model. Please check if the template is correctly defined. Allowed preprocessor functions are [exports.getOptions] and [exports.transform].");
        }
    }

    /// <summary>
    /// exports.getOptions = function (model) {
    ///     return {
    ///         bookmarks : {
    ///             uid1: "bookmark1"
    ///         },
    ///         isShared: true
    ///     }
    ///
    /// }
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public TransformModelOptions GetOptions(object model)
    {
        object obj = Preprocessor?.GetOptions(model) ?? null;
        if (obj == null)
        {
            return null;
        }

        return ReadOptions(obj);
    }

    /// <summary>
    /// Reads the two fields <see cref="TransformModelOptions"/> declares straight off the value the
    /// preprocessor returned. A script preprocessor hands back a property bag, so serializing it to JSON
    /// and deserializing it into the options type - once per document, per template - is pure overhead.
    /// </summary>
    private static TransformModelOptions ReadOptions(object obj)
    {
        if (obj is not IDictionary<string, object> properties)
        {
            // A preprocessor that is not script based can return anything; keep converting those.
            return JObject.FromObject(obj).ToObject<TransformModelOptions>();
        }

        var result = new TransformModelOptions();

        if (TryGetValue(properties, "isShared", out var isShared) && isShared != null)
        {
            result.IsShared = isShared as bool? ?? Convert.ToBoolean(isShared, CultureInfo.InvariantCulture);
        }

        if (TryGetValue(properties, "bookmarks", out var bookmarks) && bookmarks is IDictionary<string, object> bookmarkProperties)
        {
            var map = new Dictionary<string, string>(bookmarkProperties.Count);
            foreach (var pair in bookmarkProperties)
            {
                map[pair.Key] = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
            }

            result.Bookmarks = map;
        }

        return result;
    }

    private static bool TryGetValue(IDictionary<string, object> properties, string name, out object value)
    {
        if (properties.TryGetValue(name, out value))
        {
            return true;
        }

        // The JSON conversion this replaced matched property names case-insensitively.
        foreach (var pair in properties)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Transform from raw model to view model
    /// TODO: refactor to merge model and attrs into one input model
    /// </summary>
    /// <param name="model">The raw model</param>
    /// <param name="attrs">The system generated attributes</param>
    /// <returns>The view model</returns>
    public object TransformModel(object model)
    {
        if (Preprocessor == null)
        {
            return model;
        }

        return Preprocessor.TransformModel(model);
    }

    /// <summary>
    /// Transform from view model to the final result using template
    /// </summary>
    /// <param name="model">The input view model</param>
    /// <returns>The output after applying template</returns>
    public string Transform(object model)
    {
        if (Renderer == null || model == null)
        {
            return null;
        }

        return Renderer.Render(model);
    }

    /// <summary>
    /// Dependent files are defined in following syntax in Mustache template leveraging Mustache Comments
    /// {{! include('file') }}
    /// file path can be wrapped by quote ' or double quote " or none
    /// </summary>
    /// <param name="template"></param>
    private IEnumerable<TemplateResourceInfo> ExtractDependentResources(string templateName)
    {
        if (Renderer?.Dependencies == null)
        {
            yield break;
        }

        foreach (var dependency in Renderer.Dependencies)
        {
            yield return new TemplateResourceInfo(dependency);
        }
    }

    private static TemplateInfo GetTemplateInfo(string templateName)
    {
        // Remove folder
        templateName = Path.GetFileName(templateName);
        var splitterIndex = templateName.IndexOf('.');
        if (splitterIndex < 0)
        {
            return new TemplateInfo(templateName, string.Empty, TemplateType.Default);
        }

        var type = templateName.Substring(0, splitterIndex);
        var extension = templateName.Substring(splitterIndex);
        TemplateType templateType = TemplateType.Default;
        if (extension.EndsWith(Primary, StringComparison.Ordinal))
        {
            templateType = TemplateType.Primary;
            extension = extension.Substring(0, extension.Length - Primary.Length);
        }
        else if (extension.EndsWith(Auxiliary, StringComparison.Ordinal))
        {
            templateType = TemplateType.Auxiliary;
            extension = extension.Substring(0, extension.Length - Auxiliary.Length);
        }

        return new TemplateInfo(type, extension, templateType);
    }

    private sealed class TemplateInfo
    {
        public string DocumentType { get; }
        public string Extension { get; }
        public TemplateType TemplateType { get; }

        public TemplateInfo(string documentType, string extension, TemplateType type)
        {
            DocumentType = documentType;
            Extension = extension;
            TemplateType = type;
        }
    }
}
