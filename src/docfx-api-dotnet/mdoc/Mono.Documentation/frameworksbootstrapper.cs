// Command to preserve member documentation for types that are changing in a subsequent version
// By Joel Martinez <joel.martinez@xamarin.com
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Mono.Options;

namespace Mono.Documentation
{
	public class MDocFrameworksBootstrapper : MDocCommand
	{
		public override void Run (IEnumerable<string> args)
		{
			args = args.Skip (1);
			if (args.Count () != 1)
				Error ("Need to supply a single directory, which contain folders that represent frameworks.");

			string frameworkPath = args.Single ();
			int slashOffset = frameworkPath.EndsWith (Path.DirectorySeparatorChar.ToString (), StringComparison.InvariantCultureIgnoreCase) ? 0 : 1;

			if (!Directory.Exists(frameworkPath)) 
				Error ($"Path not found: {frameworkPath}");

			var data = Directory.GetDirectories (frameworkPath)
			                    .Select (d => new {
									Path = d.Substring (frameworkPath.Length + slashOffset, d.Length - frameworkPath.Length - slashOffset),
									Name = Path.GetFileName(d)
								})
                                .Where (d => !d.Name.Equals ("dependencies", StringComparison.OrdinalIgnoreCase))
                                .OrderBy(d => d.Name)
                                .ToArray();

			var frameworks = new List<XElement>();
			var assemblyVersionMappings = new Dictionary<string, Dictionary<string, string>>();
			foreach (var d in data)
			{
				Console.WriteLine(d.Name);
				var assemblyVersionMapping = new Dictionary<string, string>();
				assemblyVersionMappings.Add(d.Name, assemblyVersionMapping);
				string sourcePath = Path.Combine(frameworkPath, d.Path);
				foreach (var xmlPath in Directory.GetFiles(sourcePath, "*.xml"))
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(xmlPath);
					string dllPath = Path.Combine(sourcePath, fileNameWithoutExtension + ".dll");
					if (File.Exists(dllPath))
					{
						var version = FileVersionInfo.GetVersionInfo(dllPath).FileVersion;
						if (!string.IsNullOrEmpty(version))
						{
							assemblyVersionMapping.Add(Path.GetFileName(xmlPath), version);
						}
					}
				}
				frameworks.Add(new XElement(
								 "Framework",
								 new XAttribute("Name", d.Name),
								 new XAttribute("Source", d.Path),
								 new XElement("assemblySearchPath", Path.Combine("dependencies", d.Name))));
			}

			var maxVersions = assemblyVersionMappings.SelectMany(f => f.Value)
						.GroupBy(m => m.Key)
						.ToDictionary(g => g.Key, g => g.Max(m => m.Value));

			foreach (var framework in frameworks)
			{
				foreach (var assembly in assemblyVersionMappings[framework.Attribute("Name").ToString().Split('"')[1]])
				{
					if (maxVersions.ContainsKey(assembly.Key) && assembly.Value == maxVersions[assembly.Key])
					{
						framework.Add(new XElement("import", string.Format("{0}\\{1}", framework.Attribute("Source").ToString().Split('"')[1], assembly.Key)));
					}
				}
			}

			var doc = new XDocument(new XElement("Frameworks", frameworks));

			var configPath = Path.Combine (frameworkPath, "frameworks.xml");
			var settings = new XmlWriterSettings { Indent = true };
			using (var writer = XmlWriter.Create (configPath, settings)) {
				doc.WriteTo (writer);
			}

			Console.WriteLine ($"Framework configuration file written to {configPath}");
		}
	}
}

