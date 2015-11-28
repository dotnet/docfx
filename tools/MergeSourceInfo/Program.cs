namespace Microsoft.DocAsCode.Tools
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using System;
    using System.Collections.Generic;
    using System.IO;

    class Program
    {
        static void Merge(string source, string target)
        {
            if (!File.Exists(source))
            {
                Console.WriteLine($"{source} cannot be found, ignored.");
                return;
            }

            var srcVM = YamlUtility.Deserialize<PageViewModel>(source);
            var tgtVM = YamlUtility.Deserialize<PageViewModel>(target);
            Dictionary<string, ItemViewModel> map = new Dictionary<string, ItemViewModel>();
            foreach (var item in srcVM.Items)
            {
                map[item.Uid] = item;
            }

            foreach (var item in tgtVM.Items)
            {
                if (map.ContainsKey(item.Uid))
                {
                    item.Source = map[item.Uid].Source;
                }
                else
                {
                    Console.WriteLine($"{item.Uid} cannot be found, ignored.");
                }
            }

            Console.WriteLine($"Patching source for {target}");
            YamlUtility.Serialize(target, tgtVM);
        }

        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [source_folder] [target_folder]");
                return 1;
            }

            var srcFolder = args[0];
            var tgtFolder = args[1];
            foreach (var path in Directory.GetFiles(tgtFolder, "*.yml", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(path) == "toc.yml") continue;
                Merge(Path.Combine(srcFolder, path.Substring(tgtFolder.Length).TrimStart('\\')), path);
            }

            return 0;
        }
    }
}
