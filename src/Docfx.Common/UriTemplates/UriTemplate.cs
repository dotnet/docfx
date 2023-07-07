// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

using Docfx.Plugins;

namespace Docfx.Common;

public class UriTemplate<T>
{
    private static readonly Regex _marcoRegex = new(@"{%\s*([\S]+?)\s*%}", RegexOptions.Compiled);
    private static readonly Regex _pipelineRegex = new(@"\|>\s*([^|\s]+)\s*(.*?)\s*(?:$|(?=\|>))", RegexOptions.Compiled);

    public string Template { get; }
    private readonly Func<string, T> _func;
    private readonly IUriTemplatePipeline<T>[] _pipeline;
    private readonly string[][] _parameters;

    /// <summary>
    /// step 1 (macro):
    /// * env var:      {%abc%}                         -> expand env var abc.
    /// Step 2 (pipeline):
    /// * pipeline:     |> function parameters          -> "", and append a pipeline.
    /// </summary>
    public static UriTemplate<T> Parse(string template, Func<string, T> func, Func<string, IUriTemplatePipeline<T>> pipelineProvider)
    {
        if (template == null)
        {
            throw new ArgumentNullException(nameof(template));
        }
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }
        if (pipelineProvider == null)
        {
            throw new ArgumentNullException(nameof(pipelineProvider));
        }
        var t = _marcoRegex.Replace(
            template,
            m => Environment.GetEnvironmentVariable(m.Groups[1].Value));
        List<IUriTemplatePipeline<T>> pipeline = new();
        List<string[]> parameters = new();
        t = _pipelineRegex.Replace(
            t,
            m =>
            {
                pipeline.Add(pipelineProvider(m.Groups[1].Value));
                parameters.Add(m.Groups[2].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                return string.Empty;
            });
        return new UriTemplate<T>(t, func, pipeline.ToArray(), parameters.ToArray());
    }

    private UriTemplate(string template, Func<string, T> func, IUriTemplatePipeline<T>[] pipeline, string[][] parameters)
    {
        Template = template;
        _func = func;
        _pipeline = pipeline;
        _parameters = parameters;
    }

    /// <summary>
    /// Evaluate uri template with variables
    /// </summary>
    public T Evaluate(IDictionary<string, string> variables)
    {
        if (variables == null)
        {
            throw new ArgumentNullException(nameof(variables));
        }
        var uri = EvaluateUri(variables);
        var value = _func(uri);
        return RunPostPipeline(value);
    }

    private string EvaluateUri(IDictionary<string, string> variables)
    {
        var result = Template;
        foreach (var variable in variables)
        {
            result = result.Replace("{" + variable.Key + "}", Uri.EscapeDataString(variable.Value));
        }
        return result;
    }

    private T RunPostPipeline(T value)
    {
        T current = value;
        for (int i = 0; i < _pipeline.Length; i++)
        {
            current = _pipeline[i].Handle(current, _parameters[i]);
        }
        return current;
    }
}
