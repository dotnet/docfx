// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal static class ContinueBuild
{
    public static bool Run(ErrorBuilder errors, CommandLineOptions options)
    {
        // TODO: more thorough support of continue build, including:
        // - bookmark validation
        if (string.IsNullOrEmpty(options.Template))
        {
            throw new InvalidOperationException("Must provide the path of template repo when apply templates.");
        }

        if (options.OutputType != OutputType.PageJson)
        {
            // TODO: consider support html in multi-stage build
            throw new InvalidOperationException("Only pageJson type is supported.");
        }

        var inputDir = options.WorkingDirectory;
        var outputDir = options.Output ?? Path.Combine(AppContext.BaseDirectory, "outputs");
        var package = new LocalPackage(options.Template);
        var locale = options.Locale ?? "en-us";
        var config = new Config
        {
            DryRun = false,
            Template = new PackagePath(options.Template),
        };

        var fileResolver = new FileResolver(package);
        var jsonSchemaLoader = new JsonSchemaLoader(fileResolver);
        var jsonSchemaProvider = new JsonSchemaProvider(config, package, jsonSchemaLoader);
        var templateEngine = TemplateEngine.CreateTemplateEngine(errors, config, locale, package);

        if(!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        using var scope = Progress.Start("Apply templates...");
        ParallelUtility.ForEach(
            scope,
            errors,
            Directory.GetFiles(inputDir, "*.*", SearchOption.AllDirectories), // TODO: shouldn't glob all files as input
            filePath =>
            {
                ApplyTemplatesCore(errors, filePath, inputDir, outputDir, jsonSchemaProvider, templateEngine);
            });

        return errors.HasError;
    }

    private static void ApplyTemplatesCore(
        ErrorBuilder errors,
        string filePath,
        string inputDir,
        string outputDir,
        JsonSchemaProvider jsonSchemaProvider,
        TemplateEngine templateEngine)
    {
        var file = new FilePath(filePath);
        var pageModel = JsonUtility.Deserialize<JObject>(errors, File.ReadAllText(file.Path), file);
        var schema = pageModel.GetValue("schema")?.ToString();

        if (schema is not null && schema.Equals("toc", StringComparison.Ordinal))
        {
            var model = templateEngine.RunJavaScript("toc.json.js", JsonUtility.ToJObject(pageModel));
            CreateOutput(GetOutputPath(inputDir, filePath, outputDir, "json"), model);
        }
        else
        {
            if (jsonSchemaProvider.GetRenderType(new SourceInfo<string?>(schema)) == RenderType.Content)
            {
                var (model, metadata) = templateEngine.CreateTemplateModel(file, schema is null ? string.Empty : schema.ToString(), pageModel);
                CreateOutput(GetOutputPath(inputDir, filePath, outputDir), model);
                CreateOutput(GetOutputPath(inputDir, filePath, outputDir, "mta.json"), metadata);
            }
            else
            {
                var model = templateEngine.RunJavaScript($"{schema}.json.js", pageModel);
                CreateOutput(GetOutputPath(inputDir, filePath, outputDir, "json"), model);
            }
        }
    }

    private static string GetOutputPath(string inputDir, string filePath, string outputDir, string extension = "raw.page.json")
    {
        var relativeFilePath = Path.GetRelativePath(inputDir, filePath);
        var outputPath = Path.Combine(outputDir, relativeFilePath);
        return Path.ChangeExtension(outputPath, extension);
    }

    private static void CreateOutput(string path, object model)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, JsonUtility.Serialize(model));
    }
}
