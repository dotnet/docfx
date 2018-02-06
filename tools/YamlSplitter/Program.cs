// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tools.YamlSplitter
{
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

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<InitOptions, UpdateOptions>(args)
                .MapResult(
                (InitOptions opts) => { Console.WriteLine("init mode is not supported right now."); return 0; },
                (UpdateOptions opts) => RunUpdate(opts),
                errs => -1);
            foreach (var key in missingMergeKey)
            {
                Console.WriteLine(key + " does not have a merge key defined in schema, skipping...");
            }
        }

        private static int RunUpdate(UpdateOptions opt)
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
                ProcessFilePair(ymlFile, ymlOutputFile, mdFile, schemas);
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

        private static void ProcessFilePair(string ymlInputFile, string ymlOutputFile, string mdFile, Dictionary<string, DocumentSchema> schemas)
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
            var schemaName = mime.Substring(YamlMime.YamlMimePrefix.Length);
            if (!schemas.ContainsKey(schemaName))
            {
                Console.WriteLine("Schema {0} not found", schemaName);
                return;
            }
            var schema = schemas[schemaName];

            var mdFragments = FragmentModelHelper.LoadMarkdownFragment(mdFile);

            Traverse(yamlStream.Documents[0].RootNode, mdFragments, schema);

            var validFragments = mdFragments.Values.Where(v => v.Properties?.Count > 0).ToList();

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

        private static void Traverse(YamlNode node, Dictionary<string, MarkdownFragment> fragments, BaseSchema schema, string parentOPath = "", string uid = "")
        {
            var oPathPrefix = string.IsNullOrEmpty(parentOPath) ? "" : (parentOPath + "/");
            if (node is YamlMappingNode map)
            {
                if (string.IsNullOrEmpty(uid))
                {
                    var uidKey = schema.Properties.Keys.FirstOrDefault(k => schema.Properties[k].ContentType == ContentType.Uid);
                    if (!string.IsNullOrEmpty(uidKey))
                    {
                        uid = map.Children[uidKey].ToString();
                        fragments.AddOrUpdateFragmentEntity(uid);
                    }
                    else
                    {
                        Console.WriteLine("Cannot find Uid, aborting...");
                        return;
                    }
                }

                var keys = map.Children.Keys.Select(k => k.ToString()).ToList();
                foreach (var key in keys)
                {
                    var opath = oPathPrefix + key;
                    if (!schema.Properties.ContainsKey(key) || schema.Properties[key] == null)
                    {
                        //Console.WriteLine("Warning! Not supported property found in yaml: " + opath);
                        continue;
                    }
                    var propSchema = schema.Properties[key];

                    if (propSchema.Type == JSchemaType.String && propSchema.ContentType == ContentType.Markdown)
                    {
                        var val = map.Children[key].ToString();
                        map.Children.Remove(key);

                        fragments[uid].AddOrUpdateFragmentProperty(opath, val);
                    }
                    else if (propSchema.Type == JSchemaType.Object || propSchema.Type == JSchemaType.Array)
                    {
                        Traverse(map.Children[key], fragments, propSchema, opath, uid);
                    }
                }
            }
            else if (node is YamlSequenceNode seq)
            {
                if (string.IsNullOrEmpty(uid))
                {
                    Console.WriteLine("Cannot find Uid, aborting...");
                    return;
                }
                if (schema.Items != null && schema.Items.Properties.Any(s => s.Value.ContentType == ContentType.Markdown || s.Value.Type == JSchemaType.Object || s.Value.Type == JSchemaType.Array))
                {
                    var mergeKey = schema.Items.Properties.Keys.FirstOrDefault(k => schema.Items.Properties[k].MergeType == MergeType.Key);
                    if (mergeKey == null)
                    {
                        missingMergeKey.Add(parentOPath);
                        return;
                    }
                    foreach (var item in seq)
                    {
                        if (item is YamlMappingNode mapNode)
                        {
                            var opath = string.Format("{0}[{1}=\"{2}\"]", parentOPath, mergeKey, mapNode.Children[mergeKey].ToString());
                            Traverse(item, fragments, schema.Items, opath, uid);
                        }
                    }
                }
            }
        }
    }
}
