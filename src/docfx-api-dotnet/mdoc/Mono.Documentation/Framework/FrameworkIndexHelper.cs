using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Mono.Documentation.Framework
{
    public static class FrameworkIndexHelper
    {
        public static Dictionary<string, FrameworkNamespaceModel> CreateFrameworkIndex(string path, string frameworkName)
        {
            string frameworkFilePath = GetFrameworkFilePath(path, frameworkName);
            if (!File.Exists(frameworkFilePath))
            {
                throw new ArgumentException($"Can't find framework file: {frameworkFilePath}");
            }
            using (XmlReader xmlReader = XmlReader.Create(frameworkFilePath))
            {
                return ReadFrameworkIndex(xmlReader);
            }
        }

        public static Dictionary<string, FrameworkNamespaceModel> ReadFrameworkIndex(XmlReader xmlReader)
        {
            var dict = new Dictionary<string, FrameworkNamespaceModel>();

            xmlReader.ReadToFollowing("Framework");
            xmlReader.ReadToDescendant("Namespace");

            while (xmlReader.NodeType != XmlNodeType.EndElement)
            {
                XNode node = XNode.ReadFrom(xmlReader);
                XElement element = node as XElement;

                if (element == null) continue;
                var ns = new FrameworkNamespaceModel(node);

                dict.Add(ns.Name, ns);
            }

            return dict;
        }

        private static string GetFrameworkFilePath(string path, string frameworkName)
        {
            string docsRoot = Path.GetDirectoryName(path) ?? "";
            string frameworksIndexPath = Path.Combine(docsRoot, Consts.FrameworksIndexFolderName);
            foreach (string frameworkIndexFilePath in Directory.EnumerateFiles(frameworksIndexPath))
            {
                using (XmlReader xmlReader = XmlReader.Create(frameworkIndexFilePath))
                {
                    bool isFrameworkNodeFound = xmlReader.ReadToFollowing("Framework");
                    if (!isFrameworkNodeFound)
                    {
                        throw new InvalidOperationException($"Invalid framework file format in {frameworkIndexFilePath}");
                    }
                    
                    var frameworkNameInFile = xmlReader.GetAttribute("Name");
                    if (frameworkNameInFile == frameworkName)
                    {
                        return frameworkIndexFilePath;
                    }
                }
            }

            throw new InvalidEnumArgumentException($"Can't find file for frameworkName = {frameworkName}");
        }
    }
}
