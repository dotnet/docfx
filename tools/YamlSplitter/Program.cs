// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.YamlSplitter
{
    using Microsoft.DocAsCode.Build.OverwriteDocuments;
    using Microsoft.DocAsCode.Build.SchemaDriven;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Tools.YamlSplitter.Models;

    using CommandLine;
    using Newtonsoft.Json.Schema;
    using YamlDotNet.RepresentationModel;

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    class Program
    {
        static HashSet<string> missingMergeKey = new HashSet<string>();
        static readonly string[] SupportedYamlExtensions = { ".yml", ".yaml" };
        private static SchemaFragmentsIterator _iterator = new SchemaFragmentsIterator(new UpdateFragmentsHandler());

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<InitOptions, UpdateOptions>(args)
                .MapResult(
                (InitOptions opts) => RunUpdate(opts),
                (UpdateOptions opts) => RunUpdate(opts),
                errs => -1);
            foreach (var key in missingMergeKey)
            {
                Console.WriteLine(key + " does not have a merge key defined in schema, skipping...");
            }
        }

        private static int RunUpdate(CommonOptions opt)
        {
            opt.InputYamlFolder = Path.GetFullPath(opt.InputYamlFolder);
            if (!string.IsNullOrEmpty(opt.OutputYamlFolder))
            {
                opt.OutputYamlFolder = Path.GetFullPath(opt.OutputYamlFolder);
            }
            else
            {
                opt.OutputYamlFolder = opt.InputYamlFolder;
            }
            if (!string.IsNullOrEmpty(opt.MDFolder))
            {
                opt.MDFolder = Path.GetFullPath(opt.MDFolder);
            }
            else
            {
                opt.MDFolder = opt.OutputYamlFolder;
            }
            var schemas = LoadSchemas(opt.SchemaFolder);
            foreach (var ymlFile in Directory.EnumerateFiles(opt.InputYamlFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => SupportedYamlExtensions.Any(ext => string.Equals(ext, Path.GetExtension(f), StringComparison.OrdinalIgnoreCase))))
            {
                if (ymlFile.EndsWith("/toc.yml", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var relativeYmlPath = PathUtility.MakeRelativePath(opt.InputYamlFolder, ymlFile);
                var ymlOutputFile = Path.Combine(opt.OutputYamlFolder, relativeYmlPath);
                var mdFile = Path.Combine(opt.MDFolder, relativeYmlPath + ".md");
                ProcessFilePair(ymlFile, ymlOutputFile, mdFile, schemas, opt is InitOptions);
            }
            return 0;
        }

        private static Dictionary<string, DocumentSchema> LoadSchemas(string schemaFolderPath)
        {
            var schemas = new Dictionary<string, DocumentSchema>();
            foreach (var schemaFile in Directory.EnumerateFiles(schemaFolderPath, "*.schema.json"))
            {
                using (var sr = new StreamReader(schemaFile))
                {
                    var schema = DocumentSchema.Load(sr, schemaFile.Remove(schemaFile.Length - ".schema.json".Length));
                    schemas.Add(schema.Title, schema);
                }
            }

            return schemas;
        }

        private static void ProcessFilePair(
            string ymlInputFile,
            string ymlOutputFile,
            string mdFile,
            Dictionary<string, DocumentSchema> schemas,
            bool initMode = false)
        {
            var yamlStream = new YamlStream();
            using (var sr = new StreamReader(ymlInputFile))
            {
                yamlStream.Load(sr);
            }
            if (yamlStream.Documents.Count != 1)
            {
                throw new NotSupportedException("Does not support mutiple YAML documents");
            }

            var mime = YamlMime.ReadMime(ymlInputFile);
            if (string.IsNullOrEmpty(mime))
            {
                Console.WriteLine("Cannot find MIME in {0}", ymlInputFile);
                return;
            }
            var schemaName = mime.Substring(YamlMime.YamlMimePrefix.Length);
            if (!schemas.ContainsKey(schemaName))
            {
                Console.WriteLine("Schema {0} not found", schemaName);
                return;
            }
            var schema = schemas[schemaName];

            var mdFragments = initMode ? new Dictionary<string, MarkdownFragment>() : FragmentModelHelper.LoadMarkdownFragment(mdFile);

            _iterator.Traverse(yamlStream.Documents[0].RootNode, mdFragments, schema);

            foreach (var fragment in mdFragments.Values)
            {
                fragment.Properties = fragment.Properties?.Where(pair => pair.Value.Touched)?.ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            var validFragments = mdFragments.Values.Where(v => v.Properties?.Count > 0 && v.Touched).ToList();

            if (validFragments.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var fragment in validFragments)
                {
                    sb.AppendLine(fragment.ToString());
                }
                var mdFolder = Path.GetDirectoryName(mdFile);
                if (!Directory.Exists(mdFolder))
                {
                    Directory.CreateDirectory(mdFolder);
                }
                File.WriteAllText(mdFile, sb.ToString());

                var ymlFolder = Path.GetDirectoryName(ymlOutputFile);
                if (!Directory.Exists(ymlFolder))
                {
                    Directory.CreateDirectory(ymlFolder);
                }
                YamlUtility.Serialize(ymlOutputFile, yamlStream.Documents[0].RootNode, mime);
            }
        }
    }
}
