using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Mono.Cecil;
using Mono.Documentation.Updater;
using Mono.Documentation.Updater.Formatters;
using Mono.Documentation.Updater.Frameworks;
using Mono.Documentation.Updater.Statistics;
using Mono.Documentation.Util;
using Mono.Options;

using MyXmlNodeList = System.Collections.Generic.List<System.Xml.XmlNode>;
using StringList = System.Collections.Generic.List<string>;
using StringToXmlNodeMap = System.Collections.Generic.Dictionary<string, System.Xml.XmlNode>;

namespace Mono.Documentation
{
    public class MDocUpdater : MDocCommand
    {
        string srcPath;
        List<AssemblySet> assemblies = new List<AssemblySet> ();
        StringList globalSearchPaths = new StringList ();

        string apistyle = string.Empty;
        bool isClassicRun;
        bool verbose;
        bool delete;
        bool show_exceptions;
        bool no_assembly_versions, ignore_missing_types;
        ExceptionLocations? exceptions;

        internal int additions = 0, deletions = 0;

        List<DocumentationImporter> importers = new List<DocumentationImporter> ();

        DocumentationEnumerator docEnum;

        string since;

        static MemberFormatter docTypeFormatterField;
        static MemberFormatter filenameFormatterField;

        static MemberFormatter docTypeFormatter
        {
            get
            {
                if (docTypeFormatterField == null)
                    docTypeFormatterField = new DocTypeMemberFormatter(MDocUpdater.Instance.TypeMap);
                return docTypeFormatterField;
            }
        }
        static MemberFormatter filenameFormatter
        {
            get
            {
                if (filenameFormatterField == null)
                    filenameFormatterField = new FileNameMemberFormatter(MDocUpdater.Instance.TypeMap);
                return filenameFormatterField;
            }
        }

        private readonly List<string> CustomAttributeNamesToSkip = new List<string>()
        {
            Consts.CompilerGeneratedAttribute,
            "System.Runtime.InteropServices.TypeIdentifierAttribute"
        };

        internal static MemberFormatter csharpSlashdocFormatterField;
        internal static MemberFormatter csharpSlashdocFormatter
        {
            get
            {
                if (csharpSlashdocFormatterField == null)
                    csharpSlashdocFormatterField = new SlashDocCSharpMemberFormatter(MDocUpdater.Instance.TypeMap);
                return csharpSlashdocFormatterField;
            }
        }

        internal static MemberFormatter msxdocxSlashdocFormatterField;
        internal static MemberFormatter msxdocxSlashdocFormatter
        {
            get
            {
                if (msxdocxSlashdocFormatterField == null)
                    msxdocxSlashdocFormatterField = new MsxdocSlashDocMemberFormatter(MDocUpdater.Instance.TypeMap);
                return msxdocxSlashdocFormatterField;
            }
        }

        MyXmlNodeList extensionMethods = new MyXmlNodeList ();

        public static string droppedNamespace = string.Empty;

        private HashSet<string> memberSet;

        public static bool HasDroppedNamespace (TypeDefinition forType)
        {
            return HasDroppedNamespace (forType.Module);
        }

        public static bool HasDroppedNamespace (MemberReference forMember)
        {
            return HasDroppedNamespace (forMember.Module);
        }

        public static bool HasDroppedNamespace (AssemblyDefinition forAssembly)
        {
            return HasDroppedNamespace (forAssembly.MainModule);
        }

        public static bool HasDroppedNamespace (ModuleDefinition forModule)
        {
            return HasDroppedNamespace (forModule?.Assembly?.Name);
        }

        public static bool HasDroppedNamespace (AssemblyNameReference assemblyRef)
        {
            return assemblyRef != null && !string.IsNullOrWhiteSpace (droppedNamespace) && droppedAssemblies.Any (da => Path.GetFileNameWithoutExtension(da) == assemblyRef.Name);
        }

        public static bool HasDroppedAnyNamespace ()
        {
            return !string.IsNullOrWhiteSpace (droppedNamespace);
        }

        /// <summary>Logic flag to signify that we should list assemblies at the method level, since there are multiple
        /// assemblies for a given type/method.</summary>
        public bool IsMultiAssembly
        {
            get
            {
                return apistyle == "classic" || apistyle == "unified" || !string.IsNullOrWhiteSpace (FrameworksPath);
            }
        }

        bool writeIndex = true;

        /// <summary>Path which contains multiple folders with assemblies. Each folder contained will represent one framework.</summary>
        string FrameworksPath = string.Empty;
        FrameworkIndex frameworks;
        FrameworkIndex frameworksCache;
        IEnumerable<XDocument> oldFrameworkXmls;

        /// <summary>For unit tests to initialize the cache</summary>
        public void InitializeFrameworksCache (FrameworkIndex fi) => frameworksCache = fi;

        private StatisticsCollector statisticsCollector = new StatisticsCollector();

        static List<string> droppedAssemblies = new List<string> ();

        public string PreserveTag { get; set; }
        public bool DisableSearchDirectoryRecurse = false;
        private bool statisticsEnabled = false;
        private string statisticsFilePath;

        private static MDocUpdater instanceField;
        public static MDocUpdater Instance
        {
            get
            {
                if (instanceField == null)
                    instanceField = new MDocUpdater();

                return instanceField;
            }
            private set => instanceField = value;
        }
        public static bool SwitchingToMagicTypes { get; private set; }
        public TypeMap TypeMap { get; private set; }

        public override void Run (IEnumerable<string> args)
        {
            Console.WriteLine ("mdoc {0}", Consts.MonoVersion);

            Instance = this;
            show_exceptions = DebugOutput;
            var types = new List<string> ();
            var p = new OptionSet () {
            { "delete",
                "Delete removed members from the XML files.",
                v => delete = v != null },
            { "exceptions:",
              "Document potential exceptions that members can generate.  {SOURCES} " +
                "is a comma-separated list of:\n" +
                "  asm      Method calls in same assembly\n" +
                "  depasm   Method calls in dependent assemblies\n" +
                "  all      Record all possible exceptions\n" +
                "  added    Modifier; only create <exception/>s\n" +
                "             for NEW types/members\n" +
                "If nothing is specified, then only exceptions from the member will " +
                "be listed.",
                v =>
                {
                    exceptions = ParseExceptionLocations(v);
                } },
            { "f=",
                "Specify a {FLAG} to alter behavior.  See later -f* options for available flags.",
                v => {
                    switch (v) {
                        case "ignore-missing-types":
                            ignore_missing_types = true;
                            break;
                        case "no-assembly-versions":
                            no_assembly_versions = true;
                            break;
                        default:
                            throw new Exception ("Unsupported flag `" + v + "'.");
                    }
                } },
            { "fignore-missing-types",
                "Do not report an error if a --type=TYPE type\nwas not found.",
                v => ignore_missing_types = v != null },
            { "fno-assembly-versions",
                "Do not generate //AssemblyVersion elements.",
                v => no_assembly_versions = v != null },
            { "i|import=",
                "Import documentation from {FILE}.",
                v => AddImporter (v) },
            { "L|lib=",
                "Check for assembly references in {DIRECTORY}.",
                v => globalSearchPaths.Add (v) },
            { "library=",
                "Ignored for compatibility with update-ecma-xml.",
                v => {} },
            { "o|out=",
                "Root {DIRECTORY} to generate/update documentation.",
                v => srcPath = v },
            { "r=",
                "Search for dependent assemblies in the directory containing {ASSEMBLY}.\n" +
                "(Equivalent to '-L `dirname ASSEMBLY`'.)",
                v => globalSearchPaths.Add (Path.GetDirectoryName (v)) },
            { "since=",
                "Manually specify the assembly {VERSION} that new members were added in.",
                v => since = v },
            { "type=",
              "Only update documentation for {TYPE}.",
                v => types.Add (v) },
            { "dropns=",
              "When processing assembly {ASSEMBLY}, strip off leading namespace {PREFIX}:\n" +
              "  e.g. --dropns ASSEMBLY=PREFIX",
              v => {
                var parts = v.Split ('=');
                if (parts.Length != 2) { Console.Error.WriteLine ("Invalid dropns input"); return; }
                var assembly = Path.GetFileName (parts [0].Trim ());
                var prefix = parts [1].Trim();
                droppedAssemblies.Add (assembly);
                droppedNamespace = prefix;
            } },
            { "ntypes",
                "If the new assembly is switching to 'magic types', then this switch should be defined.",
                v => SwitchingToMagicTypes = true },
            { "preserve",
                "Do not delete members that don't exist in the assembly, but rather mark them as preserved.",
                v => PreserveTag = "true" },
            { "api-style=",
                "Denotes the apistyle. Currently, only `classic` and `unified` are supported. `classic` set of assemblies should be run first, immediately followed by 'unified' assemblies with the `dropns` parameter.",
                v => apistyle = v.ToLowerInvariant () },
            { "fx|frameworks=",
                "Configuration XML file, that points to directories which contain libraries that span multiple frameworks.",
                v => FrameworksPath = v },
            { "use-docid",
                "Add 'DocId' to the list of type and member signatures",
                v =>
                {
                    FormatterManager.AddFormatter(Consts.DocId);
                } },
            { "lang=",
                "Add languages to the list of type and member signatures (DocId, VB.NET). Values can be coma separated",
                v =>
                {
                    FormatterManager.AddFormatter(v);
                } },
            { "disable-searchdir-recurse",
                "Default behavior for adding search directories ('-L') is to recurse them and search in all subdirectories. This disables that",
                v => DisableSearchDirectoryRecurse = true },
            {
                "statistics=",
                "Save statistics to the specified file",
                v =>
                {
                    statisticsEnabled = true;
                    if (!string.IsNullOrEmpty(v))
                        statisticsFilePath = v;
                } },
            { "verbose",
                "Adds extra output to the log",
                v => verbose = true },
            { "index=",
                "Lets you choose to disable index.xml (true by default)",
                v => bool.TryParse(v, out writeIndex) },
            { "nocollapseinterfaces",
                "All interfaces listed in type signatures",
                v => Consts.CollapseInheritedInterfaces = false },
        };
            var assemblyPaths = Parse (p, args, "update",
                    "[OPTIONS]+ ASSEMBLIES",
                    "Create or update documentation from ASSEMBLIES.");

            int fxCount = 1;

            if (!string.IsNullOrWhiteSpace (FrameworksPath))
            {
                var configPath = FrameworksPath;
                var frameworksDir = FrameworksPath;
                if (!configPath.EndsWith ("frameworks.xml", StringComparison.InvariantCultureIgnoreCase))
                    configPath = Path.Combine (configPath, "frameworks.xml");
                else
                    frameworksDir = Path.GetDirectoryName (configPath);

                // check for typemap file
                string typeMapPath = Path.Combine(frameworksDir, "TypeMap.xml");
                if (File.Exists(typeMapPath))
                {
                    Console.WriteLine($"Loading typemap file at {typeMapPath}");
                    if (!Directory.Exists(srcPath))
                        Directory.CreateDirectory(srcPath);
                    File.Copy(typeMapPath, Path.Combine(srcPath, "TypeMap.xml"), true);
                    TypeMap map = TypeMap.FromXml(typeMapPath);
                    this.TypeMap = map;
                    FormatterManager.UpdateTypeMap(map);
                }

                Console.WriteLine($"Opening frameworks file '{configPath}'");
                var fxconfig = XDocument.Load (configPath);
                var fxd = fxconfig.Root
                                  .Elements ("Framework")
                                  .Select (f => new
                                  {
                                      Name = f.Attribute ("Name").Value,
                                      Path = Path.Combine (frameworksDir, f.Attribute ("Source").Value),
                                      SearchPaths = f.Elements ("assemblySearchPath")
                                                   .Select (a => Path.Combine (frameworksDir, a.Value))
                                                   .ToArray (),
                                      Imports = f.Elements ("import")
                                                   .Select (a => Path.Combine (frameworksDir, a.Value))
                                                   .ToArray (),
                                      Version = f.Elements("package")
                                          ?.FirstOrDefault()?.Attribute("Version")?.Value,
                                      Id = f.Elements("package")
                                       ?.FirstOrDefault()?.Attribute("Id")?.Value
                                  })
                                  .Where (f => Directory.Exists (f.Path))
                                  .ToArray();
                fxCount = fxd.Count ();
                oldFrameworkXmls = fxconfig.Root
                                               .Elements("Framework")
                                               .Select(f => new
                                               {
                                                   Name = f.Attribute("Name").Value,
                                                   Source = f.Attribute("Source").Value,
                                                   XmlPath = Path.Combine(srcPath, Consts.FrameworksIndex, f.Attribute("Source").Value + ".xml"),
                                               })
                                               .Where(f => File.Exists(f.XmlPath))
                                               .Select(f => XDocument.Load(f.XmlPath));

                Func<string, string, IEnumerable<string>> getFiles = (string path, string filters) =>
                {
                    var assemblyFiles = filters.Split('|').SelectMany(v => Directory.GetFiles(path, v));

                    // Directory.GetFiles method returned file names is not sort, 
                    // this makes the order of the assembly elements of our generated XML files is inconsistent in different environments,
                    // so we need to sort it.
                    return new SortedSet<string>(assemblyFiles);
                };

                var sets = fxd.Select (d => new AssemblySet (
                    d.Name,
                    getFiles (d.Path, "*.dll|*.exe|*.winmd"),
                    d.SearchPaths.Union(this.globalSearchPaths),
                    d.Imports,
                    d.Version,
                    d.Id
                ));
                this.assemblies.AddRange (sets);
                assemblyPaths.AddRange (sets.SelectMany (s => s.AssemblyPaths));
                Console.WriteLine($"Frameworks Configuration contains {assemblyPaths.Count} assemblies");


                if (!DisableSearchDirectoryRecurse)
                {
                    // unless it's been explicitly disabled, let's
                    // add all of the subdirectories to the resolver
                    // search paths.
                    foreach (var assemblySet in this.assemblies)
                        assemblySet.RecurseSearchDirectories();
                }

                // Create a cache of all frameworks, so we can look up 
                // members that may exist only other frameworks before deleting them
                Console.Write ("Creating frameworks cache: ");
                FrameworkIndex cacheIndex = new FrameworkIndex (FrameworksPath, fxCount, cachedfx:null);
                foreach (var assemblySet in this.assemblies)
                {
                    using (assemblySet)
                    {
                        foreach (var assembly in assemblySet.Assemblies)
                        {
                            Console.WriteLine($"Caching {assembly.MainModule.FileName}");
                            try
                            {
                                var a = cacheIndex.StartProcessingAssembly(assemblySet, assembly, assemblySet.Importers, assemblySet.Id, assemblySet.Version);

                                foreach (var type in assembly.GetTypes().Where(t => DocUtils.IsPublic(t)))
                                {
                                    var t = a.ProcessType(type, assembly);
                                    foreach (var member in type.GetMembers().Where(i => !DocUtils.IsIgnored(i) && IsMemberNotPrivateEII(i)))
                                        t.ProcessMember(member);
                                }
                            }
                            catch(Exception ex)
                            {
                                throw new MDocAssemblyException(assembly.FullName, $"Error caching {assembly.FullName} from {assembly.MainModule.FileName}", ex);
                            }
                        }
                    }
                }
                Console.WriteLine ($"{Environment.NewLine}done caching.");
                this.frameworksCache = cacheIndex;
            }
            else
            {
                this.assemblies.Add (new AssemblySet ("Default", assemblyPaths, this.globalSearchPaths));
            }

            if (assemblyPaths == null)
                return;
            if (assemblyPaths.Count == 0)
                Error ("No assemblies specified.");


            // validation for the api-style parameter
            if (apistyle == "classic")
                isClassicRun = true;
            else if (apistyle == "unified")
            {
                if (!droppedAssemblies.Any ())
                    Error ("api-style 'unified' must also supply the 'dropns' parameter with at least one assembly and dropped namespace.");
            }
            else if (!string.IsNullOrWhiteSpace (apistyle))
                Error ("api-style '{0}' is not currently supported", apistyle);

            // PARSE BASIC OPTIONS AND LOAD THE ASSEMBLY TO DOCUMENT

            if (srcPath == null)
                throw new InvalidOperationException ("The --out option is required.");

            docEnum = docEnum ?? new DocumentationEnumerator ();

            // PERFORM THE UPDATES
            frameworks = new FrameworkIndex (FrameworksPath, fxCount, cachedfx: this.frameworksCache?.Frameworks);

            if (types.Count > 0)
            {
                types.Sort ();
                DoUpdateTypes (srcPath, types, srcPath);
            }
            else
                DoUpdateAssemblies (srcPath, srcPath);

            if (!string.IsNullOrWhiteSpace (FrameworksPath))
                frameworks.WriteToDisk (srcPath);

            if (statisticsEnabled)
            {
                try
                {
                    StatisticsSaver.Save(statisticsCollector, statisticsFilePath);
                }
                catch (Exception exception)
                {
                    Warning($"Unable to save statistics file: {exception.Message}");
                }
            }

            Console.WriteLine ("Members Added: {0}, Members Deleted: {1}", additions, deletions);
        }

        public static bool IsInAssemblies (string name)
        {
            return Instance?.assemblies != null ? Instance.assemblies.Any (a => a.Contains (name)) : true;
        }

        void AddImporter (string path)
        {
            var importer = GetImporter (path, supportsEcmaDoc: true);
            if (importer != null)
                importers.Add (importer);
        }

        internal DocumentationImporter GetImporter (string path, bool supportsEcmaDoc)
        {
            try
            {
                XmlReader r = new XmlTextReader (path);
                if (r.Read ())
                {
                    while (r.NodeType != XmlNodeType.Element)
                    {
                        if (!r.Read ())
                            Error ("Unable to read XML file: {0}.", path);
                    }
                    if (r.LocalName == "doc")
                    {
                        return new MsxdocDocumentationImporter (path);
                    }
                    else if (r.LocalName == "Libraries")
                    {
                        if (!supportsEcmaDoc)
                            throw new NotSupportedException ($"Ecma documentation not supported in this mode: {path}");

                        var ecmadocs = new XmlTextReader (path);
                        docEnum = new EcmaDocumentationEnumerator (this, ecmadocs);
                        return new EcmaDocumentationImporter (ecmadocs);
                    }
                    else
                        Error ("Unsupported XML format within {0}.", path);
                }
                r.Close ();
            }
            catch (Exception e)
            {
                Environment.ExitCode = 1;
                Error ("Could not load XML file: {0}.", e.Message);
            }
            return null;
        }

        static ExceptionLocations ParseExceptionLocations (string s)
        {
            ExceptionLocations loc = ExceptionLocations.Member;
            if (s == null)
                return loc;
            foreach (var type in s.Split (','))
            {
                switch (type)
                {
                    case "added": loc |= ExceptionLocations.AddedMembers; break;
                    case "all": loc |= ExceptionLocations.Assembly | ExceptionLocations.DependentAssemblies; break;
                    case "asm": loc |= ExceptionLocations.Assembly; break;
                    case "depasm": loc |= ExceptionLocations.DependentAssemblies; break;
                    default: throw new NotSupportedException ("Unsupported --exceptions value: " + type);
                }
            }
            return loc;
        }

        internal void Warning (string format, params object[] args)
        {
            Message (TraceLevel.Warning, "mdoc: " + format, args);
        }

        internal AssemblyDefinition LoadAssembly (string name, IMetadataResolver resolver,  IAssemblyResolver assemblyResolver)
        {
            AssemblyDefinition assembly = null;
            try
            {
                assembly = AssemblyDefinition.ReadAssembly (name, new ReaderParameters { AssemblyResolver = assemblyResolver, MetadataResolver = resolver });
            }
            catch (Exception ex)
            {
                Warning ($"Unable to load assembly '{name}': {ex.Message}");
            }

            return assembly;
        }

        private static void WriteXml (XmlElement element, System.IO.TextWriter output)
        {
            OrderTypeAttributes (element);
            XmlTextWriter writer = new XmlTextWriter (output);
            writer.Formatting = Formatting.Indented;
            writer.Indentation = 2;
            writer.IndentChar = ' ';
            element.WriteTo (writer);
            output.WriteLine ();
        }

        private static void WriteFile (string filename, FileMode mode, Action<TextWriter> action)
        {
            Action<string> creator = file =>
            {
                using (var writer = OpenWrite (file, mode))
                    action (writer);
            };

            MdocFile.UpdateFile (filename, creator);
        }

        private static void OrderTypeAttributes (XmlElement e)
        {
            foreach (XmlElement type in e.SelectNodes ("//Type"))
            {
                OrderTypeAttributes (type.Attributes);
            }
        }

        static readonly string[] TypeAttributeOrder = {
        "Name", "FullName", "FullNameSP", "Maintainer"
    };

        private static void OrderTypeAttributes (XmlAttributeCollection c)
        {
            XmlAttribute[] attrs = new XmlAttribute[TypeAttributeOrder.Length];
            for (int i = 0; i < c.Count; ++i)
            {
                XmlAttribute a = c[i];
                for (int j = 0; j < TypeAttributeOrder.Length; ++j)
                {
                    if (a.Name == TypeAttributeOrder[j])
                    {
                        attrs[j] = a;
                        break;
                    }
                }
            }
            for (int i = attrs.Length - 1; i >= 0; --i)
            {
                XmlAttribute n = attrs[i];
                if (n == null)
                    continue;
                XmlAttribute r = null;
                for (int j = i + 1; j < attrs.Length; ++j)
                {
                    if (attrs[j] != null)
                    {
                        r = attrs[j];
                        break;
                    }
                }
                if (r == null)
                    continue;
                if (c[n.Name] != null)
                {
                    c.RemoveNamedItem (n.Name);
                    c.InsertBefore (n, r);
                }
            }
        }

        private XmlDocument CreateIndexStub ()
        {
            XmlDocument index = new XmlDocument ();

            XmlElement index_root = index.CreateElement ("Overview");
            index.AppendChild (index_root);

            if (assemblies.Count == 0)
                throw new Exception ("No assembly");

            XmlElement index_assemblies = index.CreateElement ("Assemblies");
            index_root.AppendChild (index_assemblies);

            XmlElement index_remarks = index.CreateElement ("Remarks");
            index_remarks.InnerText = "To be added.";
            index_root.AppendChild (index_remarks);

            XmlElement index_copyright = index.CreateElement ("Copyright");
            index_copyright.InnerText = "To be added.";
            index_root.AppendChild (index_copyright);

            XmlElement index_types = index.CreateElement ("Types");
            index_root.AppendChild (index_types);

            return index;
        }

        private static void WriteNamespaceStub (string ns, string outdir)
        {
            XmlDocument index = new XmlDocument ();

            XmlElement index_root = index.CreateElement ("Namespace");
            index.AppendChild (index_root);

            index_root.SetAttribute ("Name", ns);

            XmlElement index_docs = index.CreateElement ("Docs");
            index_root.AppendChild (index_docs);

            XmlElement index_summary = index.CreateElement ("summary");
            index_summary.InnerText = "To be added.";
            index_docs.AppendChild (index_summary);

            XmlElement index_remarks = index.CreateElement ("remarks");
            index_remarks.InnerText = "To be added.";
            index_docs.AppendChild (index_remarks);

            WriteFile (outdir + "/ns-" + ns + ".xml", FileMode.CreateNew,
                    writer => WriteXml (index.DocumentElement, writer));
        }

        public void DoUpdateTypes (string basepath, List<string> typenames, string dest)
        {
            var index = CreateIndexForTypes (dest);

            var found = new HashSet<string> ();
            foreach (var assemblySet in this.assemblies)
            {
                using (assemblySet)
                {
                    foreach (AssemblyDefinition assembly in assemblySet.Assemblies)
                    {
                        using (assembly)
                        {
                            try
                            {
                                var typeSet = new HashSet<string>();
                                var namespacesSet = new HashSet<string>();
                                memberSet = new HashSet<string>();

                                var frameworkEntry = frameworks.StartProcessingAssembly(assemblySet, assembly, assemblySet.Importers, assemblySet.Id, assemblySet.Version);
                                assemblySet.Framework = frameworkEntry;

                                foreach (TypeDefinition type in docEnum.GetDocumentationTypes(assembly, typenames))
                                {
                                    var typeEntry = frameworkEntry.ProcessType(type, assembly);

                                    string relpath = DoUpdateType(assemblySet, assembly, type, typeEntry, basepath, dest);
                                    if (relpath == null)
                                        continue;

                                    found.Add(type.FullName);

                                    if (index == null)
                                        continue;

                                    index.Add(assemblySet, assembly);
                                    index.Add(type);

                                    namespacesSet.Add(type.Namespace);
                                    typeSet.Add(type.FullName);
                                }

                                statisticsCollector.AddMetric(frameworkEntry.Name, StatisticsItem.Types, StatisticsMetrics.Total, typeSet.Count);
                                statisticsCollector.AddMetric(frameworkEntry.Name, StatisticsItem.Namespaces, StatisticsMetrics.Total, namespacesSet.Count);
                                statisticsCollector.AddMetric(frameworkEntry.Name, StatisticsItem.Members, StatisticsMetrics.Total, memberSet.Count);
                            }
                            catch (Exception ex)
                            {
                                throw new MDocAssemblyException(assembly.FullName, $"Error processing {assembly.FullName} from {assembly.MainModule.FileName}", ex);
                            }
                        }
                    }
                }
            }

            if (index != null)
                index.Write ();


            if (ignore_missing_types)
                return;

            var notFound = from n in typenames where !found.Contains (n) select n;
            if (notFound.Any ())
                throw new InvalidOperationException ("Type(s) not found: " + string.Join (", ", notFound.ToArray ()));
        }

        class IndexForTypes
        {

            MDocUpdater app;
            string indexFile;

            XmlDocument index;
            XmlElement index_types;
            XmlElement index_assemblies;

            public IndexForTypes (MDocUpdater app, string indexFile, XmlDocument index)
            {
                this.app = app;
                this.indexFile = indexFile;
                this.index = index;

                index_types = WriteElement (index.DocumentElement, "Types");
                index_assemblies = WriteElement (index.DocumentElement, "Assemblies");
            }

            public void Add (AssemblySet set, AssemblyDefinition assembly)
            {
                if (index_assemblies.SelectSingleNode ("Assembly[@Name='" + assembly.Name.Name + "']") != null)
                    return;

                app.AddIndexAssembly (assembly, index_assemblies, set.Framework);
            }

            public void Add (TypeDefinition type)
            {
                app.AddIndexType (type, index_types);
            }

            public void Write ()
            {
                SortIndexEntries (index_types);
                WriteFile (indexFile, FileMode.Create,
                        writer => WriteXml (index.DocumentElement, writer));
            }
        }

        IndexForTypes CreateIndexForTypes (string dest)
        {
            string indexFile = Path.Combine (dest, "index.xml");
            if (File.Exists (indexFile))
                return null;
            return new IndexForTypes (this, indexFile, CreateIndexStub ());
        }

        /// <summary>Constructs the presumed path to the type's documentation file</summary>
        /// <returns><c>true</c>, if the type file was found, <c>false</c> otherwise.</returns>
        /// <param name="result">A typle that contains 1) the 'reltypefile', 2) the 'typefile', and 3) the file info</param>
        bool TryFindTypeFile (string nsname, string typename, string basepath, out Tuple<string, string, FileInfo> result)
        {
            string reltypefile = DocUtils.PathCombine (nsname, typename + ".xml");
            string typefile = Path.Combine (basepath, reltypefile);
            System.IO.FileInfo file = new System.IO.FileInfo (typefile);

            result = new Tuple<string, string, FileInfo> (reltypefile, typefile, file);

            return file.Exists;
        }

        public string DoUpdateType (AssemblySet set, AssemblyDefinition assembly, TypeDefinition type, FrameworkTypeEntry typeEntry, string basepath, string dest)
        {
            if (type.Namespace == null)
                Warning ("warning: The type `{0}' is in the root namespace.  This may cause problems with display within monodoc.",
                        type.FullName);
            if (!DocUtils.IsPublic (type))
                return null;

            if (type.HasCustomAttributes && CustomAttributeNamesToSkip.All(x => type.CustomAttributes.Any(y => y.AttributeType.FullName == x)))
            {
                Console.WriteLine(string.Format("Embedded Type: {0}. Skip it.", type.FullName));
                return null;
            }

            // Must get the A+B form of the type name.
            string typename = GetTypeFileName (type);
            string nsname = DocUtils.GetNamespace (type);

            // Find the file, if it exists
            string[] searchLocations = new string[] {
                nsname
            };

            if (MDocUpdater.HasDroppedNamespace (type))
            {
                // If dropping namespace, types may have moved into a couple of different places.
                var newSearchLocations = searchLocations.Union (new string[] {
                string.Format ("{0}.{1}", droppedNamespace, nsname),
                nsname.Replace (droppedNamespace + ".", string.Empty),
                MDocUpdater.droppedNamespace
            });

                searchLocations = newSearchLocations.ToArray ();
            }

            string reltypefile = "", typefile = "";
            System.IO.FileInfo file = null;

            foreach (var f in searchLocations)
            {
                Tuple<string, string, FileInfo> result;
                bool fileExists = TryFindTypeFile (f, typename, basepath, out result);

                if (fileExists)
                {
                    reltypefile = result.Item1;
                    typefile = result.Item2;
                    file = result.Item3;

                    break;
                }
            }

            if (file == null || !file.Exists)
            {
                // we were not able to find a file, let's use the original type informatio.
                // so that we create the stub in the right place.
                Tuple<string, string, FileInfo> result;
                TryFindTypeFile (nsname, typename, basepath, out result);

                reltypefile = result.Item1;
                typefile = result.Item2;
                file = result.Item3;
            }

            string output = null;
            if (dest == null)
            {
                output = typefile;
            }
            else if (dest == "-")
            {
                output = null;
            }
            else
            {
                output = Path.Combine (dest, reltypefile);
            }

            if (file != null && file.Exists)
            {
                // Update
                XmlDocument basefile = new XmlDocument ();
                try
                {
                    basefile.Load (typefile);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException ("Error loading " + typefile + ": " + e.Message, e);
                }

                DoUpdateType2 ("Updating", basefile, type, typeEntry, output, false);
            }
            else
            {
                // Stub
                XmlElement td = StubType (set, assembly, type, typeEntry, output, typeEntry.Framework.Importers, typeEntry.Framework.Id, typeEntry.Framework.Version);
                if (td == null)
                    return null;
            }
            return reltypefile;
        }

        private static string GetTypeFileName (TypeReference type)
        {
            return filenameFormatter.GetName (type, useTypeProjection: false);
        }

        public static string GetTypeFileName (string typename)
        {
            StringBuilder filename = new StringBuilder (typename.Length);
            int numArgs = 0;
            int numLt = 0;
            bool copy = true;
            for (int i = 0; i < typename.Length; ++i)
            {
                char c = typename[i];
                switch (c)
                {
                    case '<':
                        copy = false;
                        ++numLt;
                        break;
                    case '>':
                        --numLt;
                        if (numLt == 0)
                        {
                            filename.Append ('`').Append ((numArgs + 1).ToString ());
                            numArgs = 0;
                            copy = true;
                        }
                        break;
                    case ',':
                        if (numLt == 1)
                            ++numArgs;
                        break;
                    default:
                        if (copy)
                            filename.Append (c);
                        break;
                }
            }
            return filename.ToString ();
        }

        private void AddIndexAssembly (AssemblyDefinition assembly, XmlElement parent, FrameworkEntry fx)
        {
            XmlElement index_assembly = null;
            if (IsMultiAssembly)
                index_assembly = (XmlElement)parent.SelectSingleNode ("Assembly[@Name='" + assembly.Name.Name + "']");

            if (index_assembly == null)
                index_assembly = parent.OwnerDocument.CreateElement ("Assembly");

            index_assembly.SetAttribute ("Name", assembly.Name.Name);
            index_assembly.SetAttribute ("Version", assembly.Name.Version.ToString ());

            AssemblyNameDefinition name = assembly.Name;
            if (name.HasPublicKey)
            {
                XmlElement pubkey = parent.OwnerDocument.CreateElement ("AssemblyPublicKey");
                var key = new StringBuilder (name.PublicKey.Length * 3 + 2);
                key.Append ("[");
                foreach (byte b in name.PublicKey)
                    key.AppendFormat ("{0,2:x2} ", b);
                key.Append ("]");
                pubkey.InnerText = key.ToString ();
                index_assembly.AppendChild (pubkey);
            }

            if (!string.IsNullOrEmpty (name.Culture))
            {
                XmlElement culture = parent.OwnerDocument.CreateElement ("AssemblyCulture");
                culture.InnerText = name.Culture;
                index_assembly.AppendChild (culture);
            }

            MakeAssemblyAttributes(index_assembly, fx, assembly);
            parent.AppendChild (index_assembly);
        }

        private void AddIndexType (TypeDefinition type, XmlElement index_types)
        {
            string typename = GetTypeFileName (type);

            // Add namespace and type nodes into the index file as needed
            string ns = DocUtils.GetNamespace (type);
            XmlElement nsnode = (XmlElement)index_types.SelectSingleNode ("Namespace[@Name=" + DocUtils.GetStringForXPath(ns) + "]");
            if (nsnode == null)
            {
                nsnode = index_types.OwnerDocument.CreateElement ("Namespace");
                nsnode.SetAttribute ("Name", ns);
                index_types.AppendChild (nsnode);
            }
            string doc_typename = GetDocTypeName (type);
            XmlElement typenode = (XmlElement)nsnode.SelectSingleNode ("Type[@Name=" + DocUtils.GetStringForXPath(typename) + "]");
            if (typenode == null)
            {
                typenode = index_types.OwnerDocument.CreateElement ("Type");
                typenode.SetAttribute ("Name", typename);
                nsnode.AppendChild (typenode);
            }
            if (typename != doc_typename)
                typenode.SetAttribute ("DisplayName", doc_typename);
            else
                typenode.RemoveAttribute ("DisplayName");

            typenode.SetAttribute ("Kind", GetTypeKind (type));
        }

        private void DoUpdateAssemblies (string source, string dest)
        {
            string indexfile = dest + "/index.xml";
            XmlDocument index;
            if (System.IO.File.Exists (indexfile))
            {
                index = new XmlDocument ();
                index.Load (indexfile);

                // Format change
                ClearElement (index.DocumentElement, "Assembly");
                ClearElement (index.DocumentElement, "Attributes");
            }
            else
            {
                index = CreateIndexStub ();
            }

            XmlElement index_types = WriteElement (index.DocumentElement, "Types");
            XmlElement index_assemblies = WriteElement (index.DocumentElement, "Assemblies");
            if (!IsMultiAssembly)
                index_assemblies.RemoveAll ();


            HashSet<string> goodfiles = new HashSet<string> (StringComparer.OrdinalIgnoreCase);

            int processedAssemblyCount = 0;
            foreach (var assemblySet in assemblies)
            {
                using (assemblySet)
                {
                    foreach (AssemblyDefinition assm in assemblySet.Assemblies)
                    {
                        using (assm)
                        {
                            try
                            {
                                DoUpdateAssembly(assemblySet, assm, index_types, source, dest, goodfiles);
                                AddIndexAssembly(assm, index_assemblies, assemblySet.Framework);

                                processedAssemblyCount++;
                            }
                            catch (Exception ex)
                            {
                                throw new MDocAssemblyException(assm.FullName, $"Error processing {assm.FullName} from {assm.MainModule.FileName}", ex);
                            }
                        }
                    }
                }
            }

            string defaultTitle = "Untitled";
            if (processedAssemblyCount == 1 && assemblies[0] != null)
            {
                var assembly = assemblies[0].Assemblies.FirstOrDefault();
                if (assembly != null) 
                    defaultTitle = assembly.Name.Name;
                else
                    Warning($"Seems to be an issue with assembly group '{assemblies[0].Name}'. There are no assemblies loaded.");
            }
            WriteElementInitialText (index.DocumentElement, "Title", defaultTitle);

            SortIndexEntries (index_types);

            CleanupFiles (dest, goodfiles);
            CleanupIndexTypes (index_types, goodfiles);
            CleanupExtensions (index_types);

            if (writeIndex)
            {
                WriteFile(indexfile, FileMode.Create,
                        writer => WriteXml(index.DocumentElement, writer));
            }
        }

        private static char[] InvalidFilenameChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        private void DoUpdateAssembly (AssemblySet assemblySet, AssemblyDefinition assembly, XmlElement index_types, string source, string dest, HashSet<string> goodfiles)
        {
            Console.WriteLine($"Updating {assembly.FullName} from {assembly.MainModule.FileName}");

            var namespacesSet = new HashSet<string> ();
            var typeSet = new HashSet<string> ();
            memberSet = new HashSet<string> ();

            var frameworkEntry = frameworks.StartProcessingAssembly (assemblySet, assembly, assemblySet.Importers, assemblySet.Id, assemblySet.Version);
            foreach (TypeDefinition type in docEnum.GetDocumentationTypes (assembly, null))
            {
                string typename = GetTypeFileName (type);
                if (!DocUtils.IsPublic (type) || typename.IndexOfAny (InvalidFilenameChars) >= 0)
                    continue;

                var typeEntry = frameworkEntry.ProcessType (type, assembly);

                string reltypepath = DoUpdateType (assemblySet, assembly, type, typeEntry, source, dest);
                if (reltypepath == null)
                    continue;

                // Add namespace and type nodes into the index file as needed
                AddIndexType (type, index_types);

                // Ensure the namespace index file exists
                string namespaceToUse = type.Namespace;
                if (HasDroppedNamespace (assembly))
                {
                    namespaceToUse = string.Format ("{0}.{1}", droppedNamespace, namespaceToUse);
                }
                string onsdoc = DocUtils.PathCombine (dest, namespaceToUse + ".xml");
                string nsdoc = DocUtils.PathCombine (dest, "ns-" + namespaceToUse + ".xml");
                namespacesSet.Add (namespaceToUse);
                if (File.Exists (onsdoc))
                {
                    File.Move (onsdoc, nsdoc);
                }

                if (!File.Exists (nsdoc))
                {
                    statisticsCollector.AddMetric (frameworkEntry.Name, StatisticsItem.Namespaces, StatisticsMetrics.Added);
                    Console.WriteLine ("New Namespace File: " + type.Namespace);
                    WriteNamespaceStub (namespaceToUse, dest);
                }

                goodfiles.Add (reltypepath);
                typeSet.Add (type.FullName);
            }
            statisticsCollector.AddMetric (frameworkEntry.Name, StatisticsItem.Types, StatisticsMetrics.Total, typeSet.Count);
            statisticsCollector.AddMetric (frameworkEntry.Name, StatisticsItem.Namespaces, StatisticsMetrics.Total, namespacesSet.Count);
            statisticsCollector.AddMetric (frameworkEntry.Name, StatisticsItem.Members, StatisticsMetrics.Total, memberSet.Count);
        }

        private static void SortIndexEntries (XmlElement indexTypes)
        {
            XmlNodeList namespaces = indexTypes.SelectNodes ("Namespace");
            XmlNodeComparer c = new AttributeNameComparer ();
            SortXmlNodes (indexTypes, namespaces, c);

            for (int i = 0; i < namespaces.Count; ++i)
                SortXmlNodes (namespaces[i], namespaces[i].SelectNodes ("Type"), c);
        }

        private static void SortXmlNodes (XmlNode parent, XmlNodeList children, XmlNodeComparer comparer)
        {
            MyXmlNodeList l = new MyXmlNodeList (children.Count);
            for (int i = 0; i < children.Count; ++i)
                l.Add (children[i]);
            l.Sort (comparer);
            for (int i = l.Count - 1; i > 0; --i)
            {
                parent.InsertBefore (parent.RemoveChild ((XmlNode)l[i - 1]), (XmlNode)l[i]);
            }
        }

        public abstract class XmlNodeComparer : IComparer, IComparer<XmlNode>
        {
            public abstract int Compare (XmlNode x, XmlNode y);

            public int Compare (object x, object y)
            {
                return Compare ((XmlNode)x, (XmlNode)y);
            }
        }

        public class MemberParameterNameComparer : XmlNodeComparer 
        {
            Dictionary<string, int> order = new Dictionary<string, int> ();

            public MemberParameterNameComparer(XmlElement parent, string tagName = "Parameter") 
            {
                var s = parent.GetElementsByTagName (tagName).Cast<XmlElement> ();

                int currentIndex = 0;
                foreach(var param in s.Select (p => new {Element=p, Name = p.SelectSingleNode ("@Name"), Index=p.SelectSingleNode ("@Index")})) {
                    if (param.Name == null)
                        throw new Exception (param.Element.OuterXml);
                    order[param.Name.Value] = currentIndex;

                    currentIndex++;
                }
            }
            public override int Compare (XmlNode x, XmlNode y)
            {
                return Compare (x as XmlElement, y as XmlElement);    
            }

            int Compare(XmlElement x, XmlElement y) 
            {
                string xname = x.GetAttribute ("name");
                string yname = y.GetAttribute ("name");

                int xindex, yindex;
                order.TryGetValue (xname, out xindex);
                order.TryGetValue (yname, out yindex);

                return xindex.CompareTo (yindex);
            }
        }

        class AttributeNameComparer : XmlNodeComparer
        {
            string attribute;

            public AttributeNameComparer ()
                : this ("Name")
            {
            }

            public AttributeNameComparer (string attribute)
            {
                this.attribute = attribute;
            }

            public override int Compare (XmlNode x, XmlNode y)
            {
                return x.Attributes[attribute].Value.CompareTo (y.Attributes[attribute].Value);
            }
        }

        class VersionComparer : XmlNodeComparer
        {
            public override int Compare (XmlNode x, XmlNode y)
            {
                // Some of the existing docs use e.g. 1.0.x.x, which Version doesn't like.
                string a = GetVersion (x.InnerText);
                string b = GetVersion (y.InnerText);
                return new Version (a).CompareTo (new Version (b));
            }

            static string GetVersion (string v)
            {
                int n = v.IndexOf ("x");
                if (n < 0)
                    return v;
                return v.Substring (0, n - 1);
            }
        }

        private static string GetTypeKind (TypeDefinition type)
        {
            if (type.IsEnum)
                return "Enumeration";
            if (type.IsValueType)
                return "Structure";
            if (type.IsInterface)
                return "Interface";
            if (DocUtils.IsDelegate (type))
                return "Delegate";
            if (type.IsClass || type.FullName == "System.Enum") // FIXME
                return "Class";
            throw new ArgumentException ("Unknown kind for type: " + type.FullName);
        }

        private void CleanupFiles (string dest, HashSet<string> goodfiles)
        {
            // Look for files that no longer correspond to types
            foreach (System.IO.DirectoryInfo nsdir in new System.IO.DirectoryInfo (dest).GetDirectories ("*").Where (d => Path.GetFileName (d.FullName) != Consts.FrameworksIndex))
            {
                foreach (System.IO.FileInfo typefile in nsdir.GetFiles ("*.xml"))
                {
                    string relTypeFile = Path.Combine (nsdir.Name, typefile.Name);
                    if (!goodfiles.Contains (relTypeFile))
                    {
                        XmlDocument doc = new XmlDocument ();
                        doc.Load (typefile.FullName);
                        XmlElement e = doc.SelectSingleNode ("/Type") as XmlElement;
                        if (e == null) {
                            Warning ($"{typefile.FullName} is not an EcmaXML type file.");
                            continue;
                        }
                        var typeFullName = e.GetAttribute("FullName");
                        var assemblyNameNode = doc.SelectSingleNode ("/Type/AssemblyInfo/AssemblyName");
                        if (assemblyNameNode == null)
                        {
                            Warning ("Did not find /Type/AssemblyInfo/AssemblyName on {0}", typefile.FullName);
                            continue;
                        }
                        string assemblyName = assemblyNameNode.InnerText;


                        Action saveDoc = () =>
                        {
                            using (TextWriter writer = OpenWrite (typefile.FullName, FileMode.Truncate))
                                WriteXml (doc.DocumentElement, writer);
                        };

                        if (!IsMultiAssembly)
                        { // only do this for "regular" runs
                            AssemblyDefinition assembly = assemblies
                                .SelectMany (aset => aset.Assemblies)
                                .FirstOrDefault (a => a.Name.Name == assemblyName);
                            if (e != null && !no_assembly_versions && assembly != null && assemblyName != null && UpdateAssemblyVersions (e, assembly, GetAssemblyVersions (assemblyName), false))
                            {
                                saveDoc ();
                                goodfiles.Add (relTypeFile);
                                continue;
                            }
                        }

                        Action actuallyDelete = () =>
                        {
                            string filename = typefile.FullName; 
                            try { MdocFile.DeleteFile (filename); } catch (Exception fex) { Warning ("Unable to delete existing file: {0} - {1}", filename, fex.Message); }
                            Console.WriteLine ("Class no longer present; file deleted: " + Path.Combine (nsdir.Name, typefile.Name));

                            // Here we don't know the framwork which contained the removed type. So, we determine it by the old frameworks XML-file
                            // If there is only one framework, use it as a default value
                            var defaultFramework = frameworks.Frameworks.FirstOrDefault();
                            // If there is no frameworks (no frameworks mode) or there is more than one framework
                            if (defaultFramework == null)
                                // Use FrameworkEntry.Empty as the default value (as well as in FrameworkIndex/StartProcessingAssembly)
                                defaultFramework = FrameworkEntry.Empty;
                            var frameworkName = defaultFramework.Name;
                            // Try to find the removed type in the old frameworks XML-file
                            var frameworkXml = oldFrameworkXmls?.FirstOrDefault
                                (i => i.XPathSelectElements($"Framework/Namespace/Type[@Name='{typeFullName}']").Any());
                            var frameworkNameAttribute = frameworkXml?.Root?.Attribute ("Name");
                            // If the removed type is found in the old frameworks XML-file, use this framework name
                            if (frameworkNameAttribute != null)
                                frameworkName = frameworkNameAttribute.Value;
                            statisticsCollector.AddMetric (frameworkName, StatisticsItem.Types, StatisticsMetrics.Removed);
                        };

                        if (string.IsNullOrWhiteSpace (PreserveTag))
                        { // only do this if there was not a -preserve
                            saveDoc ();

                            var unifiedAssemblyNode = doc.SelectSingleNode ("/Type/AssemblyInfo[@apistyle='unified']");
                            var classicAssemblyNode = doc.SelectSingleNode ("/Type/AssemblyInfo[not(@apistyle) or @apistyle='classic']");
                            var unifiedMembers = doc.SelectNodes ("//Member[@apistyle='unified']|//Member/AssemblyInfo[@apistyle='unified']");
                            var classicMembers = doc.SelectNodes ("//Member[@apistyle='classic']|//Member/AssemblyInfo[@apistyle='classic']");
                            bool isUnifiedRun = HasDroppedAnyNamespace ();
                            bool isClassicOrNormalRun = !isUnifiedRun;

                            Action<XmlNode, ApiStyle> removeStyles = (x, style) =>
                            {
                                var styledNodes = doc.SelectNodes ("//*[@apistyle='" + style.ToString ().ToLowerInvariant () + "']");
                                if (styledNodes != null && styledNodes.Count > 0)
                                {
                                    foreach (var node in styledNodes.SafeCast<XmlNode> ())
                                    {
                                        node.ParentNode.RemoveChild (node);
                                    }
                                }
                                saveDoc ();
                            };
                            if (isClassicOrNormalRun)
                            {
                                if (unifiedAssemblyNode != null || unifiedMembers.Count > 0)
                                {
                                    Warning ("*** this type is marked as unified, not deleting during this run: {0}", typefile.FullName);
                                    // if truly removed from both assemblies, it will be removed fully during the unified run
                                    removeStyles (doc, ApiStyle.Classic);
                                    continue;
                                }
                                else
                                {
                                    // we should be safe to delete here because it was not marked as a unified assembly
                                    actuallyDelete ();
                                }
                            }
                            if (isUnifiedRun)
                            {
                                if (classicAssemblyNode != null || classicMembers.Count > 0)
                                {
                                    Warning ("*** this type is marked as classic, not deleting {0}", typefile.FullName);
                                    continue;
                                }
                                else
                                {
                                    // safe to delete because it wasn't marked as a classic assembly, so the type is gone in both.
                                    actuallyDelete ();
                                }
                            }
                        }
                    }
                }
            }
        }

        private static TextWriter OpenWrite (string path, FileMode mode)
        {
            var w = new StreamWriter (
                new FileStream (path, mode),
                new UTF8Encoding (false)
            );
            w.NewLine = "\n";
            return w;
        }

        private string[] GetAssemblyVersions (string assemblyName)
        {
            return (from a in assemblies.SelectMany (aset => aset.Assemblies)
                    where a.Name.Name == assemblyName
                    select GetAssemblyVersion (a)).ToArray ();
        }

        private static void CleanupIndexTypes (XmlElement index_types, HashSet<string> goodfiles)
        {
            // Look for type nodes that no longer correspond to types
            MyXmlNodeList remove = new MyXmlNodeList ();
            foreach (XmlElement typenode in index_types.SelectNodes ("Namespace/Type"))
            {
                string fulltypename = Path.Combine (((XmlElement)typenode.ParentNode).GetAttribute ("Name"), typenode.GetAttribute ("Name") + ".xml");
                if (!goodfiles.Contains (fulltypename))
                {
                    remove.Add (typenode);
                }
            }
            foreach (XmlNode n in remove)
                n.ParentNode.RemoveChild (n);
        }

        private void CleanupExtensions (XmlElement index_types)
        {
            XmlNode e = index_types.SelectSingleNode ("/Overview/ExtensionMethods");
            if (extensionMethods.Count == 0)
            {
                if (e == null)
                    return;
                index_types.SelectSingleNode ("/Overview").RemoveChild (e);
                return;
            }
            if (e == null)
            {
                e = index_types.OwnerDocument.CreateElement ("ExtensionMethods");
                index_types.SelectSingleNode ("/Overview").AppendChild (e);
            }
            else
                e.RemoveAll ();
            extensionMethods.Sort (DefaultExtensionMethodComparer);
            foreach (XmlNode m in extensionMethods)
            {
                e.AppendChild (index_types.OwnerDocument.ImportNode (m, true));
            }
        }

        class ExtensionMethodComparer : XmlNodeComparer
        {
            public override int Compare (XmlNode x, XmlNode y)
            {
                XmlNode xLink = x.SelectSingleNode ("Member/Link");
                XmlNode yLink = y.SelectSingleNode ("Member/Link");

                int n = xLink.Attributes["Type"].Value.CompareTo (
                        yLink.Attributes["Type"].Value);
                if (n != 0)
                    return n;
                n = xLink.Attributes["Member"].Value.CompareTo (
                        yLink.Attributes["Member"].Value);
                if (n == 0 && !object.ReferenceEquals (x, y))
                    throw new InvalidOperationException ("Duplicate extension method found!");
                return n;
            }
        }

        static readonly XmlNodeComparer DefaultExtensionMethodComparer = new ExtensionMethodComparer ();
        
        public void DoUpdateType2 (string message, XmlDocument basefile, TypeDefinition type, FrameworkTypeEntry typeEntry, string output, bool insertSince)
        {
            Console.WriteLine (message + ": " + type.FullName);
            StringToXmlNodeMap seenmembers = new StringToXmlNodeMap ();
            StringToXmlNodeMap seenmembersdocid = new StringToXmlNodeMap();
            var allTypeEiimembers = GetTypeEiiMembers(type);
            // Update type metadata
            UpdateType (basefile.DocumentElement, type, typeEntry);

            // Update existing members.  Delete member nodes that no longer should be there,
            // and remember what members are already documented so we don't add them again.

            MyXmlNodeList todelete = new MyXmlNodeList ();

            Dictionary<string, List<MemberReference>> implementedMembers = DocUtils.GetImplementedMembersFingerprintLookup(type);

            foreach (DocsNodeInfo info in docEnum.GetDocumentationMembers(basefile, type, typeEntry))
            {
                if (info.Node.ParentNode == null)
                    continue;
                
                XmlElement oldmember = info.Node;
                MemberReference oldmember2 = info.Member;

                if (info.Member != null && info.Node != null)
                {
                    // Check for an error condition where the xml MemberName doesn't match the matched member
                    var memberName = GetMemberName (info.Member);
                    var memberAttribute = info.Node.Attributes["MemberName"];
                    if (NeedToSetMemberName(memberAttribute, memberName, info))
                    {
                        oldmember.SetAttribute ("MemberName", memberName);
                    }

                    AddEiiNameAsAttribute(info.Member, oldmember, memberName);

                    // Check valid Member AssemblyInfo and delete invalid
                    if (typeEntry.Framework.IsFirstFrameworkForType(typeEntry))
                    {
                        var delList = DocUtils.RemoveInvalidAssemblyInfo(info.Node, no_assembly_versions, "Member");
                        foreach (var delitem in delList)
                            info.Node.RemoveChild(delitem);
                    }
                }

                string sig = oldmember2 != null ? FormatterManager.MemberFormatters[1].GetDeclaration (oldmember2) : null;

                // Interface implementations and overrides are deleted from the docs
                // unless the overrides option is given.
                if (oldmember2 != null && sig == null)
                    oldmember2 = null;

                // Deleted (or signature changed, or existing member is EII to a private interface)
                // note: apologies for the double negatives in names here ... but the intent is ultimately 
                // to remove private EII members that were there prior to this rule being instated 
                bool isprivateeii = !IsMemberNotPrivateEII(oldmember2);
                if (oldmember2 == null || isprivateeii)
                {
                    if (!string.IsNullOrWhiteSpace (FrameworksPath) && !isprivateeii) // only do this check if fx mode AND it's not a private EII. If it's a private EII, we just want to delete it
                    {
                        // verify that this member wasn't seen in another framework and is indeed valid
                        var sigFromXml = oldmember
                            .GetElementsByTagName ("MemberSignature")
                            .Cast<XmlElement> ()
                            .FirstOrDefault (x => x.GetAttribute ("Language").Equals ("DocId"));

                        Func<FrameworkTypeEntry, string, bool> checksig = (t, s) => t.ContainsDocId (s);

                        if (sigFromXml == null)
                        {
                            sigFromXml = oldmember
                                .GetElementsByTagName ("MemberSignature")
                                .Cast<XmlElement> ()
                                .FirstOrDefault (x => x.GetAttribute ("Language").Equals ("ILAsm"));
                            checksig = (t, s) => t.ContainsCSharpSig (s);
                        }

                        if (sigFromXml != null)
                        {
                            var sigvalue = sigFromXml.GetAttribute ("Value");
                            Func<FrameworkEntry, bool> hasMember = fx =>
                            {
                                var tInstance = fx.FindTypeEntry (typeEntry.Name);
                                if (tInstance != null)
                                {
                                    bool hassig = checksig (tInstance, sigvalue);
                                    return hassig;
                                }
                                return false;
                            };
                            if (frameworksCache.Frameworks.Any(hasMember)) // does this function exist in *any* frameworks
                            {
                                continue; // we won't be deleting it
                            }
                        }
                    }

                    if (!no_assembly_versions && UpdateAssemblyVersions (oldmember, type.Module.Assembly, new string[] { GetAssemblyVersion (type.Module.Assembly) }, false))
                        continue;

                    DeleteMember ("Member Removed", output, oldmember, todelete, type);
                    statisticsCollector.AddMetric(typeEntry.Framework.Name, StatisticsItem.Members, StatisticsMetrics.Removed);
                    continue;
                }


                // Duplicated
                if (seenmembers.ContainsKey (sig))
                {
                    if (object.ReferenceEquals (oldmember, seenmembers[sig]))
                    {
                        // ignore, already seen
                    }
                    else
                    {
                        bool hasContent = MDocUpdater.MemberDocsHaveUserContent (oldmember);
                        if (hasContent)
                        {
                            // let's see if we can find a dupe with fewer docs ... just in case
                            var matchingMembers = oldmember.ParentNode.SelectNodes ("Member[MemberSignature[@Language='ILAsm' and @Value='" + sig + "']]");
                            if (matchingMembers.Count > 1)
                            {
                                // ok, there's more than one, let's make sure it's a better candidate for removal
                                var membersWithNoDocs = matchingMembers
                                    .Cast<XmlElement> ()
                                    .TakeWhile (memberElement => !object.ReferenceEquals (memberElement, oldmember))
                                    .Select (memberElement => new
                                    {
                                        Element = memberElement,
                                        hasContent = MDocUpdater.MemberDocsHaveUserContent (memberElement),
                                        alreadyDeleted = memberElement.GetAttribute ("ToDelete") == "true"
                                    })
                                    .Where (mem => !mem.hasContent && !mem.alreadyDeleted)
                                    .Select (mem => mem.Element)
                                    .ToArray();

                                if (membersWithNoDocs.Any())
                                {
                                    foreach (var memberToDelete in membersWithNoDocs)
                                    {
                                        memberToDelete.SetAttribute ("ToDelete", "true");
                                        DeleteMember ("Duplicate Member (empty) Found", output, memberToDelete, todelete, type);
                                        statisticsCollector.AddMetric (typeEntry.Framework.Name, StatisticsItem.Members, StatisticsMetrics.Removed);
                                    }
                                    continue;
                                }
                            }
                        }

                        if (DocUtils.DocIdCheck(seenmembers[sig], oldmember))
                        { continue; }

                        oldmember.SetAttribute ("ToDelete", "true");
                        DeleteMember ("Duplicate Member Found", output, oldmember, todelete, type);
                        statisticsCollector.AddMetric(typeEntry.Framework.Name, StatisticsItem.Members, StatisticsMetrics.Removed);
                    }
                    continue;
                }

                // Update signature information
                UpdateMember (info, typeEntry, implementedMembers, allTypeEiimembers);
                memberSet.Add (info.Member.FullName);

                // get all apistyles of sig from info.Node
                var sigs = oldmember.GetElementsByTagName("MemberSignature").Cast<XmlElement>().ToArray();
                var styles = sigs
                    .Where (x => x.GetAttribute ("Language") == "ILAsm" && !seenmembers.ContainsKey (x.GetAttribute ("Value")))
                    .Select (x => x.GetAttribute ("Value"));
                var docidstyles = sigs
                    .Where(x => x.GetAttribute("Language") == "DocId" && !seenmembersdocid.ContainsKey(x.GetAttribute("Value")))
                    .Select(x => x.GetAttribute("Value"));


                typeEntry.ProcessMember (info.Member);

                foreach (var stylesig in styles)
                    seenmembers.Add(stylesig, oldmember);
                foreach (var stylesig in docidstyles)
                    seenmembersdocid.Add(stylesig, oldmember);

                if (oldmember.HasAttribute("ToDelete"))
                {
                    // this is the result of an error state, let's remove this
                    oldmember.RemoveAttribute ("ToDelete");
                }
            }
            foreach (XmlElement oldmember in todelete.Where(mem => mem.ParentNode != null))
                oldmember.ParentNode.RemoveChild (oldmember);


            if (!DocUtils.IsDelegate (type))
            {
                XmlNode members = WriteElement (basefile.DocumentElement, "Members");
                var typemembers = type.GetMembers ()
                        .Where(m =>
                        {
                            if (m is TypeDefinition) return false;
                            string cssig = FormatterManager.MemberFormatters[0].GetDeclaration(m);
                            if (cssig == null) return false;

                            string sig = FormatterManager.MemberFormatters[1].GetDeclaration(m);
                            if (sig == null || seenmembers.ContainsKey(sig)) return false;

                            var docidsig = FormatterManager.DocIdFormatter.GetDeclaration(m);
                            if (seenmembersdocid.ContainsKey(docidsig ?? "")) return false;

                            // Verify that the member isn't an explicitly implemented 
                            // member of an internal interface, in which case we shouldn't return true.
                            

                            if (!IsMemberNotPrivateEII(m))
                                return false;

                            return true;
                        })
                        .ToArray();
                foreach (MemberReference m in typemembers)
                {
                    XmlElement mm = MakeMember (basefile, new DocsNodeInfo (null, m), members, typeEntry, implementedMembers, allTypeEiimembers);
                    if (mm == null) continue;

                    if (MDocUpdater.SwitchingToMagicTypes || MDocUpdater.HasDroppedNamespace (m))
                    {
                        // this is a unified style API that obviously doesn't exist in the classic API. Let's mark
                        // it with apistyle="unified", so that it's not displayed for classic style APIs
                        mm.AddApiStyle (ApiStyle.Unified);
                    }

                    statisticsCollector.AddMetric (typeEntry.Framework.Name, StatisticsItem.Members, StatisticsMetrics.Added);
                    memberSet.Add (m.FullName);
                    var node = mm.SelectSingleNode("MemberSignature/@Value") ??
                               mm.SelectSingleNode("MemberSignature/@Usage");
                    Console.WriteLine ("Member Added: " + (node?.InnerText ?? m.FullName));
                    additions++;
                }

                if (this.TypeMap != null)
                {
                    foreach (var iface in type.Interfaces)
                    {
                        // check typemap to see if there's an ifacereplace for this
                        var facename = iface.InterfaceType.FullName;
                        TypeMapInterfaceItem ifaceItem = this.TypeMap.HasInterfaceReplace("C#", facename);

                        if (ifaceItem == null || ifaceItem?.Members == null)
                            continue;

                        // if so, foreach Member in ifacereplace
                        foreach (var ifaceMember in ifaceItem.Members.Elements())
                        {
                            // check member/seenmember

                            string ifacedocid = null;

                            var sigs = ifaceMember.Elements("MemberSignature").Where(s => s.Attribute("Language")?.Value == "ILAsm" && !seenmembers.ContainsKey(s.Attribute("Language")?.Value));

                            if (sigs.Any())
                            {

                                // insert entry into `members`
                                var xElement = ifaceItem.ToXmlElement(ifaceMember);
                                var imported = members.OwnerDocument.ImportNode(xElement, true);

                                // replace the docid type
                                var docIdelement = imported.SelectSingleNode("MemberSignature[@Language='DocId']");
                                if (docIdelement != null)
                                {
                                    var valueAttribute = docIdelement.Attributes["Value"];
                                    var docidvalue = valueAttribute?.Value;
                                    if (valueAttribute != null && docidvalue != null)
                                    {
                                        ifacedocid = docidvalue.Replace(ifaceItem.To, type.FullName);
                                        valueAttribute.Value = ifacedocid;
                                    }
                                }


                                members.AppendChild(imported);

                                // add to statisticscollector
                                typeEntry.ProcessMember(ifacedocid);
                                statisticsCollector.AddMetric(typeEntry.Framework.Name, StatisticsItem.Members, StatisticsMetrics.Added);
                                additions++;
                            }
                        }
                    }
                }
            }

            // Import code snippets from files
            foreach (XmlNode code in basefile.GetElementsByTagName ("code"))
            {
                if (!(code is XmlElement)) continue;
                string file = ((XmlElement)code).GetAttribute ("src");
                string lang = ((XmlElement)code).GetAttribute ("lang");
                if (file != "")
                {
                    string src = GetCodeSource (lang, Path.Combine (srcPath, file));
                    if (src != null)
                        code.InnerText = src;
                }
            }

            if (insertSince && since != null)
            {
                XmlNode docs = basefile.DocumentElement.SelectSingleNode ("Docs");
                docs.AppendChild (CreateSinceNode (basefile));
            }

            do
            {
                XmlElement d = basefile.DocumentElement["Docs"];
                XmlElement m = basefile.DocumentElement["Members"];
                if (d != null && m != null)
                    basefile.DocumentElement.InsertBefore (
                            basefile.DocumentElement.RemoveChild (d), m);
                SortTypeMembers (m);
            } while (false);

            if (output == null)
                WriteXml (basefile.DocumentElement, Console.Out);
            else
            {
                FileInfo file = new FileInfo (output);
                if (!file.Directory.Exists)
                {
                    Console.WriteLine ("Namespace Directory Created: " + type.Namespace);
                    file.Directory.Create ();
                }
                WriteFile (output, FileMode.Create,
                        writer => WriteXml (basefile.DocumentElement, writer));
            }
        }

        public static bool IsMemberNotPrivateEII(MemberReference m)
        {
            MethodDefinition methdef = null;
            if (m is MethodDefinition)
                methdef = m as MethodDefinition;
            else if (m is PropertyDefinition)
            {
                var prop = m as PropertyDefinition;
                methdef = prop.GetMethod ?? prop.SetMethod;
            }
            else if (m is EventDefinition)
            {
                var ev = m as EventDefinition;
                methdef = ev.AddMethod ?? ev.RemoveMethod;
            }
            if (methdef != null)
            {
                TypeReference iface;
                MethodReference imethod;

                // private interface check only if the method isn't public and isn't protected virtual in a non-sealed class (because both are accessible to client code)
                if (methdef.Overrides.Count == 1 && !methdef.IsPublic && !(methdef.IsFamily && methdef.IsVirtual && !methdef.DeclaringType.IsSealed))
                {
                    DocUtils.GetInfoForExplicitlyImplementedMethod(methdef, out iface, out imethod);
                    if (!DocUtils.IsPublic(iface.Resolve())) 
                        return false;

                    if (DocUtils.IsEiiIgnoredMethod(methdef, imethod))
                        return false;
                }
            }

            return true;
        }

        private bool NeedToSetMemberName(XmlAttribute memberAttribute, string memberName, DocsNodeInfo info)
        {
            return memberAttribute == null
                   || memberAttribute.Value != memberName && memberAttribute.Value.Split(',').Length != memberName.Split(',').Length
                   //needs to fix the issue with Eii names for VB https://github.com/mono/api-doc-tools/issues/92
                   || memberAttribute.Value != memberName && DocUtils.IsExplicitlyImplemented(info.Member);
        }

        private string GetCodeSource (string lang, string file)
        {
            int anchorStart;
            if (lang == "C#" && (anchorStart = file.IndexOf (".cs#")) >= 0)
            {
                // Grab the specified region
                string region = "#region " + file.Substring (anchorStart + 4);
                file = file.Substring (0, anchorStart + 3);
                try
                {
                    using (StreamReader reader = new StreamReader (file))
                    {
                        string line;
                        StringBuilder src = new StringBuilder ();
                        int indent = -1;
                        while ((line = reader.ReadLine ()) != null)
                        {
                            if (line.Trim () == region)
                            {
                                indent = line.IndexOf (region);
                                continue;
                            }
                            if (indent >= 0 && line.Trim ().StartsWith ("#endregion"))
                            {
                                break;
                            }
                            if (indent >= 0)
                                src.Append (
                                        (line.Length > 0 ? line.Substring (indent) : string.Empty) +
                                        "\n");
                        }
                        return src.ToString ();
                    }
                }
                catch (Exception e)
                {
                    Warning ("Could not load <code/> file '{0}' region '{1}': {2}",
                            file, region, show_exceptions ? e.ToString () : e.Message);
                    return null;
                }
            }
            try
            {
                using (StreamReader reader = new StreamReader (file))
                    return reader.ReadToEnd ();
            }
            catch (Exception e)
            {
                Warning ("Could not load <code/> file '" + file + "': " + e.Message);
            }
            return null;
        }

        void DeleteMember (string reason, string output, XmlNode member, MyXmlNodeList todelete, TypeDefinition type)
        {
            string format = output != null
                ? "{0}: File='{1}'; Signature='{4}'"
                : "{0}: XPath='/Type[@FullName=\"{2}\"]/Members/Member[@MemberName=\"{3}\"]'; Signature='{4}'";
            string signature = member.SelectSingleNode ("MemberSignature[@Language='C#']/@Value")?.Value 
                ?? member.Attributes["MemberName"]?.Value
                ?? member.OuterXml;

            Warning (format,
                    reason,
                    output,
                    member.OwnerDocument.DocumentElement.GetAttribute ("FullName"),
                    member.Attributes["MemberName"].Value,
                    signature);

            // Identify all of the different states that could affect our decision to delete the member
            bool shouldPreserve = !string.IsNullOrWhiteSpace (PreserveTag);
            bool hasContent = MemberDocsHaveUserContent (member);
            bool shouldDelete = !shouldPreserve && (delete || !hasContent);

            bool unifiedRun = HasDroppedNamespace (type);

            var classicAssemblyInfo = member.SelectSingleNode ("AssemblyInfo[not(@apistyle) or @apistyle='classic']");
            bool nodeIsClassic = classicAssemblyInfo != null || member.HasApiStyle (ApiStyle.Classic);
            var unifiedAssemblyInfo = member.SelectSingleNode ("AssemblyInfo[@apistyle='unified']");
            bool nodeIsUnified = unifiedAssemblyInfo != null || member.HasApiStyle (ApiStyle.Unified);

            Action actuallyDelete = () =>
            {
                todelete.Add (member);
                deletions++;
            };

            if (!shouldDelete)
            {
                // explicitly not deleting
                string message = shouldPreserve ?
                        "Not deleting '{0}' due to --preserve." :
                        "Not deleting '{0}'; must be enabled with the --delete option";
                Warning (message, signature);
            }
            else if (unifiedRun && nodeIsClassic)
            {
                // this is a unified run, and the member doesn't exist, but is marked as being in the classic assembly.
                member.RemoveApiStyle (ApiStyle.Unified);
                member.AddApiStyle (ApiStyle.Classic);
                Warning ("Not removing '{0}' since it's still in the classic assembly.", signature);
            }
            else if (unifiedRun && !nodeIsClassic)
            {
                // unified run, and the node is not classic, which means it doesn't exist anywhere.
                actuallyDelete ();
            }
            else
            {
                if (!isClassicRun || (isClassicRun && !nodeIsClassic && !nodeIsUnified))
                { // regular codepath (ie. not classic/unified)
                    actuallyDelete ();
                }
                else
                { // this is a classic run
                    Warning ("Removing classic from '{0}' ... will be removed in the unified run if not present there.", signature);
                    member.RemoveApiStyle (ApiStyle.Classic);
                    if (classicAssemblyInfo != null)
                    {
                        member.RemoveChild (classicAssemblyInfo);
                    }
                }
            }
        }

        class MemberComparer : XmlNodeComparer
        {
            public override int Compare (XmlNode x, XmlNode y)
            {
                int r;
                string xMemberName = x.Attributes["MemberName"].Value;
                string yMemberName = y.Attributes["MemberName"].Value;

                // generic methods *end* with '>'
                // it's possible for explicitly implemented generic interfaces to
                // contain <...> without being a generic method
                if ((!xMemberName.EndsWith (">") || !yMemberName.EndsWith (">")) &&
                        (r = xMemberName.CompareTo (yMemberName)) != 0)
                    return r;

                int lt;
                if ((lt = xMemberName.IndexOf ("<")) >= 0)
                    xMemberName = xMemberName.Substring (0, lt);
                if ((lt = yMemberName.IndexOf ("<")) >= 0)
                    yMemberName = yMemberName.Substring (0, lt);
                if ((r = xMemberName.CompareTo (yMemberName)) != 0)
                    return r;

                // Handle MemberGroup sorting
                var sc = StringComparison.InvariantCultureIgnoreCase;
                if (x.Name.Equals ("MemberGroup", sc) || y.Name.Equals ("MemberGroup", sc))
                {
                    if (x.Name.Equals ("MemberGroup", sc) && y.Name.Equals ("Member", sc))
                        return -1;
                    else if (x.Name.Equals ("Member", sc) && y.Name.Equals ("MemberGroup", sc))
                        return 1;
                    else
                        return xMemberName.CompareTo (yMemberName);
                }

                // if @MemberName matches, then it's either two different types of
                // members sharing the same name, e.g. field & property, or it's an
                // overloaded method.
                // for different type, sort based on MemberType value.
                r = x.SelectSingleNode ("MemberType").InnerText.CompareTo (
                        y.SelectSingleNode ("MemberType").InnerText);
                if (r != 0)
                    return r;

                // same type -- must be an overloaded method.  Sort based on type 
                // parameter count, then parameter count, then by the parameter 
                // type names.
                XmlNodeList xTypeParams = x.SelectNodes ("TypeParameters/TypeParameter");
                XmlNodeList yTypeParams = y.SelectNodes ("TypeParameters/TypeParameter");
                if (xTypeParams.Count != yTypeParams.Count)
                    return xTypeParams.Count <= yTypeParams.Count ? -1 : 1;
                for (int i = 0; i < xTypeParams.Count; ++i)
                {
                    r = xTypeParams[i].Attributes["Name"].Value.CompareTo (
                            yTypeParams[i].Attributes["Name"].Value);
                    if (r != 0)
                        return r;
                }

                XmlNodeList xParams = x.SelectNodes ("Parameters/Parameter");
                XmlNodeList yParams = y.SelectNodes ("Parameters/Parameter");
                if (xParams.Count != yParams.Count)
                    return xParams.Count <= yParams.Count ? -1 : 1;
                for (int i = 0; i < xParams.Count; ++i)
                {
                    r = xParams[i].Attributes["Type"].Value.CompareTo (
                            yParams[i].Attributes["Type"].Value);
                    if (r != 0)
                        return r;
                }
                // all parameters match, but return value might not match if it was
                // changed between one version and another.
                XmlNode xReturn = x.SelectSingleNode ("ReturnValue/ReturnType");
                XmlNode yReturn = y.SelectSingleNode ("ReturnValue/ReturnType");
                if (xReturn != null && yReturn != null)
                {
                    r = xReturn.InnerText.CompareTo (yReturn.InnerText);
                    if (r != 0)
                        return r;
                }

                return 0;
            }
        }

        static readonly MemberComparer DefaultMemberComparer = new MemberComparer ();

        private static void SortTypeMembers (XmlNode members)
        {
            if (members == null)
                return;
            SortXmlNodes (members, members.SelectNodes ("Member|MemberGroup"), DefaultMemberComparer);
        }

        private static bool MemberDocsHaveUserContent (XmlNode e)
        {
            e = (XmlElement)e.SelectSingleNode ("Docs");
            if (e == null) return false;
            foreach (XmlElement d in e.SelectNodes ("*"))
                if (d.InnerText != "" && !d.InnerText.StartsWith ("To be added"))
                    return true;
            return false;
        }

        // UPDATE HELPER FUNCTIONS

        // CREATE A STUB DOCUMENTATION FILE	

        public XmlElement StubType (AssemblySet set, AssemblyDefinition assembly, TypeDefinition type, FrameworkTypeEntry typeEntry, string output, IEnumerable<DocumentationImporter> importers, string Id, string Version)
        {
            string typesig = FormatterManager.TypeFormatters[0].GetDeclaration (type);
            if (typesig == null) return null; // not publicly visible

            XmlDocument doc = new XmlDocument ();
            XmlElement root = doc.CreateElement ("Type");
            doc.AppendChild (root);

            DoUpdateType2 ("New Type", doc, type, typeEntry, output, true);
            statisticsCollector.AddMetric (typeEntry.Framework.Name, StatisticsItem.Types, StatisticsMetrics.Added);

            return root;
        }

        private XmlElement CreateSinceNode (XmlDocument doc)
        {
            XmlElement s = doc.CreateElement ("since");
            s.SetAttribute ("version", since);
            return s;
        }

        // STUBBING/UPDATING FUNCTIONS

        public void UpdateType (XmlElement root, TypeDefinition type, FrameworkTypeEntry typeEntry)
        {
            root.SetAttribute ("Name", GetDocTypeName (type, useTypeProjection: false));
            root.SetAttribute ("FullName", GetDocTypeFullName (type, useTypeProjection: false));

            foreach (MemberFormatter f in FormatterManager.TypeFormatters)
            {
                UpdateSignature(f, type, root, typeEntry);
            }

            // Check valid Type AssemblyInfo and delete invalid
            if (typeEntry.Framework.IsFirstFrameworkForType(typeEntry))
            {
                var delList = DocUtils.RemoveInvalidAssemblyInfo(root, no_assembly_versions, "Type");
                foreach (var delitem in delList)
                    delitem.ParentNode.RemoveChild(delitem);
            }

            AddAssemblyNameToNode (root, type);
            UpdateTypeForwardingChain(root, typeEntry, type);

            string assemblyInfoNodeFilter = MDocUpdater.HasDroppedNamespace (type) ? "[@apistyle='unified']" : "[not(@apistyle) or @apistyle='classic']";
            Func<XmlElement, bool> assemblyFilter = x => x.SelectSingleNode ("AssemblyName").InnerText == type.Module.Assembly.Name.Name;
            foreach (var ass in root.SelectNodes ("AssemblyInfo" + assemblyInfoNodeFilter).Cast<XmlElement> ().Where (assemblyFilter))
            {
                WriteElementText (ass, "AssemblyName", type.Module.Assembly.Name.Name);
                if (!no_assembly_versions)
                {
                    UpdateAssemblyVersions (ass, type, true);
                }
                else
                {
                    var versions = ass.SelectNodes ("AssemblyVersion").Cast<XmlNode> ().ToList ();
                    foreach (var version in versions)
                        ass.RemoveChild (version);
                }
                if (!string.IsNullOrEmpty (type.Module.Assembly.Name.Culture))
                    WriteElementText (ass, "AssemblyCulture", type.Module.Assembly.Name.Culture);
                else
                    ClearElement (ass, "AssemblyCulture");


                // Why-oh-why do we put assembly attributes in each type file?
                // Neither monodoc nor monodocs2html use them, so I'm deleting them
                // since they're outdated in current docs, and a waste of space.
                //MakeAttributes(ass, type.Assembly, true);
                XmlNode assattrs = ass.SelectSingleNode ("Attributes");
                if (assattrs != null)
                    ass.RemoveChild (assattrs);

                NormalizeWhitespace (ass);
            }

            if (type.IsGenericType ())
            {
                MakeTypeParameters (typeEntry, root, type.GenericParameters, type, MDocUpdater.HasDroppedNamespace (type));
            }
            else
            {
                ClearElement (root, "TypeParameters");
            }

            UpdateBaseType(root, type, typeEntry);
            
            if (!DocUtils.IsDelegate (type) && !type.IsEnum)
            {
                IEnumerable<TypeReference> userInterfaces = DocUtils.GetAllPublicInterfaces (type);
                List<string> interface_names = userInterfaces
                        .Select (iface => 
                            GetDocTypeFullName (iface))
                        .OrderBy (s => s)
                        .Distinct()
                        .ToList ();

                XmlElement interfaces = WriteElement (root, "Interfaces");
                if (typeEntry.IsOnFirstFramework)
                {
                    interfaces.RemoveAll();
                }

                foreach (string iname in interface_names)
                {
                    XmlElement iface = WriteElementWithSubElementText(interfaces, "Interface", "InterfaceName", iname);
                    iface.AddFrameworkToElement(typeEntry.Framework);
                    if (typeEntry.IsOnLastFramework)
                    {
                        iface.ClearFrameworkIfAll(typeEntry.Framework.AllFrameworksWithType(typeEntry));
                    }
                }
            }
            else
            {
                ClearElement (root, "Interfaces");
            }

			MakeAttributes (root, AttributeFormatter.GetCustomAttributes(type), typeEntry);

            if (DocUtils.IsDelegate (type))
            {
                // Checked with dotnet team, we should be align with MSDN
                // If a generic parameter is from delaring type rather than delegate itself, we should not add "TypeParameter" node in ecmaxml

                MakeTypeParameters(typeEntry, root, DocUtils.GetGenericParameters(type), type, MDocUpdater.HasDroppedNamespace (type));
                var member = type.GetMethod ("Invoke");

                bool fxAlternateTriggered = false;
                MakeParameters (root, member, member.Parameters, typeEntry, ref fxAlternateTriggered);
                MakeReturnValue (typeEntry, root, member);
            }

            DocsNodeInfo typeInfo = new DocsNodeInfo (WriteElement (root, "Docs"), type);
            MakeDocNode (typeInfo, typeEntry.Framework.Importers, typeEntry);

            if (!DocUtils.IsDelegate (type))
                WriteElement (root, "Members");

            OrderTypeNodes (root, root.ChildNodes);
            NormalizeWhitespace (root);
        }

        private void UpdateBaseType(XmlElement root, TypeDefinition type, FrameworkTypeEntry typeEntry)
        {
            if (typeEntry.TimesProcessed > 1)
                return;

            if (typeEntry.Framework.IsFirstFrameworkForType(typeEntry))
                ClearElement(root, "Base");

            if (type.BaseType != null)
            {
                XmlElement basenode = WriteElement(root, "Base");

                string basetypename = GetDocTypeFullName(type.BaseType);

                // Initially CLR used to support both singlecast and multicast for delegates. But then distinction was removed later.
                // The line could be added to reduces clutter in the docs by not showing the redundant MulticastDelegate.
                // Although all delegates are multicast now. We will keep this line to avoid huge changes in ecmaxml files.
                // https://ceapex.visualstudio.com/Engineering/_workitems/129591
                if (basetypename == "System.MulticastDelegate") basetypename = "System.Delegate";

                if (string.IsNullOrWhiteSpace(FrameworksPath))
                    WriteElementText(root, "Base/BaseTypeName", basetypename);
                else
                {
                    // Check for the possibility of an alternate inheritance chain in different frameworks
                    var typeElements = basenode.GetElementsByTagName("BaseTypeName");

                    if (typeElements.Count == 0) // no existing elements, just add
                        WriteElementText(root, "Base/BaseTypeName", basetypename);
                    else
                    {
                        // There's already a BaseTypeName, see if it matches
                        if (typeElements[0].InnerText != basetypename)
                        {
                            // Add a framework alternate if one doesn't already exist
                            var existing = typeElements.Cast<XmlNode>().Where(n => n.InnerText == basetypename);
                            if (!existing.Any())
                            {
                                var newNode = WriteElementText(basenode, "BaseTypeName", basetypename, forceNewElement: true);
                                WriteElementAttribute(newNode, Consts.FrameworkAlternate, typeEntry.Framework.Name);
                            }
                            else
                            {
                                // Append framework alternate if one already exist
                                var existingNode = existing.Cast<XmlElement>().FirstOrDefault();
                                WriteElementAttribute(existingNode, Consts.FrameworkAlternate, FXUtils.AddFXToList(existingNode.GetAttribute(Consts.FrameworkAlternate), typeEntry.Framework.Name));
                            }
                        }
                    }
                }

                // Document how this type instantiates the generic parameters of its base type
                TypeReference origBase = type.BaseType.GetElementType();
                if (origBase.IsGenericType())
                {
                    ClearElement(basenode, "BaseTypeArguments");
                    GenericInstanceType baseInst = type.BaseType as GenericInstanceType;
                    IList<TypeReference> baseGenArgs = baseInst == null ? null : baseInst.GenericArguments;
                    IList<GenericParameter> baseGenParams = origBase.GenericParameters;
                    if (baseGenArgs.Count != baseGenParams.Count)
                        throw new InvalidOperationException("internal error: number of generic arguments doesn't match number of generic parameters.");
                    for (int i = 0; baseGenArgs != null && i < baseGenArgs.Count; i++)
                    {
                        GenericParameter param = baseGenParams[i];
                        TypeReference value = baseGenArgs[i];

                        XmlElement bta = WriteElement(basenode, "BaseTypeArguments");
                        XmlElement arg = bta.OwnerDocument.CreateElement("BaseTypeArgument");
                        bta.AppendChild(arg);
                        arg.SetAttribute("TypeParamName", param.Name);
                        arg.InnerText = GetDocTypeFullName(value);
                    }
                }
            }
        }

        /// <summary>Adds an AssemblyInfo with AssemblyName node to an XmlElement.</summary>
        /// <returns>The assembly that was either added, or was already present</returns>
        XmlElement AddAssemblyNameToNode (XmlElement root, TypeDefinition forType)
        {
            return AddAssemblyNameToNode (root, forType.Module, forType);
        }

        XmlElement AddAssemblyNameToNode (XmlElement root, ModuleDefinition module, TypeDefinition forType)
        {
            var list = this.assemblies.SelectMany (a => a.FullAssemblyChain (forType));
            
            var names = list.Select(a => AddAssemblyNameToNodeCore (root, a, forType));
            return names.ToArray().First ();
        }

        XmlElement AddAssemblyNameToNodeCore (XmlElement root, ModuleDefinition module, TypeDefinition forType)
        {
            return AddAssemblyNameToNodeCore (root, module.Assembly.Name, forType);
        }

        /// <summary>Adds an AssemblyInfo with AssemblyName node to an XmlElement.</summary>
        /// <returns>The assembly that was either added, or was already present</returns>
        XmlElement AddAssemblyNameToNodeCore (XmlElement root, AssemblyNameReference assembly, TypeDefinition forType)
        {
            Func<XmlElement, bool> assemblyFilter = x =>
            {
                var existingName = x.SelectSingleNode ("AssemblyName");

                bool apiStyleMatches = true;
                string currentApiStyle = x.GetAttribute ("apistyle");
                if ((HasDroppedNamespace (assembly) && !string.IsNullOrWhiteSpace (currentApiStyle) && currentApiStyle != "unified") ||
                        (isClassicRun && (string.IsNullOrWhiteSpace (currentApiStyle) || currentApiStyle != "classic")))
                {
                    apiStyleMatches = false;
                }
                return apiStyleMatches && (existingName == null || (existingName != null && existingName.InnerText == assembly.Name));
            };

            return AddAssemblyXmlNode (
                root.SelectNodes ("AssemblyInfo").Cast<XmlElement> ().ToArray (),
                assemblyFilter, x => WriteElementText (x, "AssemblyName", assembly.Name),
                () =>
                {
                    XmlElement ass = WriteElement (root, "AssemblyInfo", forceNewElement: true);

                    if (MDocUpdater.HasDroppedNamespace (assembly))
                        ass.AddApiStyle (ApiStyle.Unified);
                    if (isClassicRun)
                        ass.AddApiStyle (ApiStyle.Classic);
                    return ass;
                }, assembly);
        }

        static readonly string[] TypeNodeOrder = {
        "Metadata",
        "TypeSignature",
        "MemberOfLibrary",
        "AssemblyInfo",
        "TypeForwardingChain",
        "ThreadingSafetyStatement",
        "ThreadSafetyStatement",
        "TypeParameters",
        "Base",
        "Interfaces",
        "Attributes",
        "Parameters",
        "ReturnValue",
        "Docs",
        "Members",
        "TypeExcluded",
    };

        static void OrderTypeNodes (XmlNode member, XmlNodeList children)
        {
            ReorderNodes (member, children, TypeNodeOrder);
        }

        internal static IEnumerable<T> Sort<T> (IEnumerable<T> list)
        {
            List<T> l = new List<T> (list);
            l.Sort ();
            return l;
        }

        private void UpdateMember (DocsNodeInfo info, FrameworkTypeEntry typeEntry, Dictionary<string, List<MemberReference>> implementedMembers, IEnumerable<Eiimembers> eiiMenbers)
        {
            XmlElement me = (XmlElement)info.Node;
            MemberReference mi = info.Member;
            typeEntry.ProcessMember (mi);


            var memberName = GetMemberName (mi);
            me.SetAttribute ("MemberName", memberName);

            WriteElementText (me, "MemberType", GetMemberType (mi));
            AddImplementedMembers(typeEntry, mi, implementedMembers, me, eiiMenbers);

            if (!no_assembly_versions)
            {
                if (!IsMultiAssembly)
                    UpdateAssemblyVersions (me, mi, true);
                else
                {
                    var node = AddAssemblyNameToNode (me, mi.Module, mi.DeclaringType.Resolve());

                    UpdateAssemblyVersionForAssemblyInfo (node, me, new[] { GetAssemblyVersion (mi.Module.Assembly) }, add: true);
                }
            }
            else
            {
                ClearElement (me, "AssemblyInfo");
            }

			MakeMemberAttributes (me, mi, typeEntry);

            MakeReturnValue (typeEntry, me, mi, MDocUpdater.HasDroppedNamespace (mi));
            if (mi is MethodReference)
            {
                MethodReference mb = (MethodReference)mi;
                if (mb.IsGenericMethod ())
                    MakeTypeParameters (typeEntry, me, mb.GenericParameters, mi, MDocUpdater.HasDroppedNamespace (mi));
            }
            bool fxAlternateTriggered = false;
            MakeParameters (me, mi, typeEntry, ref fxAlternateTriggered, MDocUpdater.HasDroppedNamespace (mi));

            string fieldValue;
            if (mi is FieldDefinition && GetFieldConstValue ((FieldDefinition)mi, out fieldValue))
                WriteElementText (me, "MemberValue", fieldValue);

            info.Node = WriteElement (me, "Docs");
            MakeDocNode (info, typeEntry.Framework.Importers, typeEntry);
            
            foreach (MemberFormatter f in FormatterManager.MemberFormatters)
            {
                UpdateSignature (f, mi, me, typeEntry, implementedMembers);
            }

            OrderMemberNodes (me, me.ChildNodes);
            UpdateExtensionMethods (me, info);
        }

        private static void UpdateSignature(MemberFormatter formatter, TypeDefinition type, XmlElement xmlElement, FrameworkTypeEntry typeEntry)
        {
            string elementName = "TypeSignature";
            string elementXPath = $"{elementName}[@Language='" + formatter.Language + "']";
            var existingElements = QueryXmlElementsByXpath(xmlElement, elementXPath).ToList();

            if (typeEntry.TimesProcessed > 1 && existingElements.Any())
                return;

            // if first framework of type, clear signatures and generate from scratch
            if (typeEntry.Framework.IsFirstFrameworkForType(typeEntry))
            {
                existingElements.ForEach(e => e.ParentNode.RemoveChild(e));
                existingElements = QueryXmlElementsByXpath(xmlElement, elementXPath).ToList();
            }

            // start to process 
            var usageSample = formatter.UsageFormatter?.GetDeclaration(type);
            var valueToUse = formatter.GetDeclaration(type);
            if (valueToUse == null && usageSample == null)
                return;

            bool elementFound = false;

            // if there is already signature found under same formatter, update fxa
            if (existingElements.Any())
            {
                foreach (var element in existingElements)
                {
                    var val = element.GetAttribute("Value");
                    var usage = element.GetAttribute("Usage");
                    if (val == (valueToUse ?? "") && usage == (usageSample ?? ""))
                    {
                        // add fxa
                        string newfxa = FXUtils.AddFXToList(element.GetAttribute(Consts.FrameworkAlternate), typeEntry.Framework.Name);
                        element.SetAttribute(Consts.FrameworkAlternate, newfxa);
                        elementFound = true;
                    }
                }
            }

            if (!elementFound) //not exists, add signature with fxa
            {
                var newElement = xmlElement.OwnerDocument.CreateElement(elementName);
                xmlElement.AppendChild(newElement);
                newElement.SetAttribute("Language", formatter.Language);

                if (!string.IsNullOrWhiteSpace(valueToUse))
                    newElement.SetAttribute("Value", valueToUse);

                if (!string.IsNullOrWhiteSpace(usageSample))
                    newElement.SetAttribute("Usage", usageSample);

                newElement.SetAttribute(Consts.FrameworkAlternate, typeEntry.Framework.Name);
            }

            // if last framework for type, check if need to purge FrameworkAlternate attribute
            if (typeEntry.Framework.IsLastFrameworkForType(typeEntry))
            {
                foreach (var element in QueryXmlElementsByXpath(xmlElement, elementXPath))
                {
                    var fxa = element.GetAttribute(Consts.FrameworkAlternate);
                    if (string.IsNullOrWhiteSpace(fxa))
                    {
                        element.ParentNode.RemoveChild(element);
                    }

                    var allFrameworks = typeEntry.Framework.AllFrameworksWithType(typeEntry);
                    if (element.HasAttribute(Consts.FrameworkAlternate) && element.GetAttribute(Consts.FrameworkAlternate) == allFrameworks)
                    {
                        element.RemoveAttribute(Consts.FrameworkAlternate);
                    }
                }
            }
        }

        public static void UpdateSignature(MemberFormatter formatter, MemberReference member, XmlElement xmlElement, FrameworkTypeEntry typeEntry, Dictionary<string, List<MemberReference>> implementedMembers)
        {
            var valueToUse = formatter.GetDeclaration(member);
            var usageSample = formatter.UsageFormatter?.GetDeclaration(member);

            List<MemberReference> implementedRefs;
            if (formatter.TypeMap != null && valueToUse != null && implementedMembers.TryGetValue(DocUtils.GetFingerprint(member), out implementedRefs))
            {
                foreach(var iref in implementedRefs)
                {
                    var nsformatter = formatter as IFormatterNamespaceControl;
                    if (nsformatter != null) nsformatter.ShouldAppendNamespace = true;

                    var irefsig = formatter.GetName(iref.DeclaringType, useTypeProjection: false);

                    if (nsformatter != null) nsformatter.ShouldAppendNamespace = false;

                    var typeReplaceItem = formatter?.TypeMap?.HasInterfaceReplace(formatter.Language, irefsig);
                    if (typeReplaceItem != null)
                    {
                        valueToUse = $"{formatter.SingleLineComment} This member is not implemented in {formatter.Language}";
                        usageSample = null;
                    }
                }
            }

            string elementName = "MemberSignature";
            string elementXPath = $"{elementName}[@Language='" + formatter.Language + "']";
            Func<IEnumerable<XmlElement>> elementsQuery = () => xmlElement.SelectNodes(elementXPath).SafeCast<XmlElement>().ToArray();

            var existingElements = elementsQuery();

            if (typeEntry.TimesProcessed > 1 && existingElements.Any())
                return;

            // pre: clear all the signatures
            if (typeEntry.IsMemberOnFirstFramework(member))
            {
                foreach (var element in existingElements)// xmlElement.SelectNodes(elementName).SafeCast<XmlElement>())
                {
                    // remove element
                    element.ParentNode.RemoveChild(element);
                }

                existingElements = elementsQuery();
            }

            if (valueToUse == null && usageSample == null)
                return;

            bool elementFound = false;

            // if exists, add fxa to it
            if (existingElements.Any())
            {
                //var matchingElement = elementsQuery.Where(e => e.GetAttribute("Value") == valueToUse);
                foreach(var element in existingElements)
                {
                    var val = element.GetAttribute("Value");
                    var usage = element.GetAttribute("Usage");
                    if (val == (valueToUse??"") && usage == (usageSample??""))
                    {
                        // add FXA
                        string newfxa = FXUtils.AddFXToList(element.GetAttribute(Consts.FrameworkAlternate), typeEntry.Framework.Name);
                        element.SetAttribute(Consts.FrameworkAlternate, newfxa);
                        elementFound = true;
                    }
                    else
                    {
                        string newfxa = FXUtils.RemoveFXFromList(element.GetAttribute(Consts.FrameworkAlternate), typeEntry.Framework.Name);
                        element.SetAttribute(Consts.FrameworkAlternate, newfxa);
                    }
                }
            }

            if (!elementFound) //not exists, just add it with fxa
            {
                var newElement = xmlElement.OwnerDocument.CreateElement(elementName);
                xmlElement.AppendChild(newElement);
                newElement.SetAttribute("Language", formatter.Language);

                if (!string.IsNullOrWhiteSpace(valueToUse))
                    newElement.SetAttribute("Value", valueToUse);

                if (!string.IsNullOrWhiteSpace(usageSample))
                    newElement.SetAttribute("Usage", usageSample);

                newElement.SetAttribute(Consts.FrameworkAlternate, typeEntry.Framework.Name);
            }


            // if last framework, check to see if fxa list is the entire, and remove
            if (typeEntry.IsMemberOnLastFramework(member))
            {
                foreach(var element in elementsQuery())
                {
                    var fxa = element.GetAttribute(Consts.FrameworkAlternate);
                    if (string.IsNullOrWhiteSpace(fxa))
                    {
                        element.ParentNode.RemoveChild(element);
                    }

                    var allfx = typeEntry.AllFrameworkStringForMember (member);
                    if (allfx == fxa)
                    {
                        element.RemoveAttribute(Consts.FrameworkAlternate);
                    }
                }
            }
        }

        private static void GetUpdateXmlMethods(MemberFormatter formatter, XmlElement xmlElement, string elementXPath, string valueToUse,
            string usageSample, out Func<XmlElement, bool> valueMatches, out Action<XmlElement> setValue, out Func<bool, XmlElement> makeNewNode)
        {
            valueMatches = x =>
                x.GetAttribute("Value") == valueToUse 
                && (string.IsNullOrEmpty(x.GetAttribute("Usage")) || x.GetAttribute("Usage") == usageSample);

            setValue = x =>
            {
                if (valueToUse != null)
                {
                    x.SetAttribute("Value", valueToUse);
                }
                if (usageSample != null)
                {
                    x.SetAttribute("Usage", usageSample);
                }
            };

            makeNewNode = (forceIt) =>
            {
                XmlElement node = null;

                if(!forceIt) // let's reuse based on lang, value, and usage if this is false
                {
                    var existingLangs = xmlElement.SelectNodes (elementXPath);
                    if (existingLangs != null && existingLangs.Count > 0)
                    {
                        var withValue = existingLangs.Cast<XmlElement> ().Where (e => e.GetAttribute ("Value") == (valueToUse??"") && e.GetAttribute ("Usage") == (usageSample??""));
                        if (withValue.Any ())
                        {
                            node = withValue.First ();
                        }
                        else
                            forceIt = true;
                    }
                    else // oh well, there are none to reuse anyways
                    {
                        forceIt = true;
                    }
                }
                if (forceIt)
                {
                    node = WriteElement(xmlElement, elementXPath, forceNewElement: true);
                    WriteElementAttribute(node, "Language", formatter.Language);
                }
                if (valueToUse != null)
                {
                    node = WriteElementAttribute(node, "Value", valueToUse);
                }
                if (usageSample != null)
                {
                    node = WriteElementAttribute(node, "Usage", usageSample);
                }
                return node;
            };
        }


        public static void AddImplementedMembers(FrameworkTypeEntry typeEntry, MemberReference mi, Dictionary<string, List<MemberReference>> allImplementedMembers, XmlElement root, IEnumerable<Eiimembers> eiiMembers)
        {
            if (typeEntry.TimesProcessed > 1)
                return;

            bool isExplicitlyImplemented = DocUtils.IsExplicitlyImplemented(mi);

            var fingerprint = DocUtils.GetFingerprint(mi);
            if (typeEntry.IsMemberOnFirstFramework(mi))
            {
                ClearElement(root, "Implements");
            }

            if (!allImplementedMembers.ContainsKey(fingerprint))
                return;

            List<MemberReference> implementedMembers = allImplementedMembers[fingerprint]
                 .Select(x => new { Ref=x, Resolved=x.Resolve(), DeclaringType=x.DeclaringType.Resolve() })
                 .Where(x => x.DeclaringType.IsInterface && DocUtils.IsPublic(x.DeclaringType)  ) // Display only public interface members
                 .Select(x => x.Ref)
                 .ToList();

            if (isExplicitlyImplemented)
            {
                // leave only one explicitly implemented member
                var explicitTypeName = DocUtils.GetExplicitTypeName(mi);

                // find one member wich is pocessed by the explicitly mentioned type
                var explicitlyImplemented = implementedMembers.FirstOrDefault(i => i.DeclaringType.GetElementType().FullName == explicitTypeName);

                if (explicitlyImplemented != null)
                {
                    implementedMembers = new List<MemberReference>
                    {
                        explicitlyImplemented
                    };
                }
            }

            if (!isExplicitlyImplemented)
            {
                eiiMembers.Where(x => x.Fingerprint == fingerprint).ToList().ForEach(t =>
                {
                    t.Interfaces.ForEach(u =>
                    {
                        var delitem = implementedMembers.FirstOrDefault(i =>
                        {
                            if (mi is MethodDefinition)
                            {
                                MethodDefinition methodDefinition = (MethodDefinition)i;
                                if (methodDefinition.FullName == u.FullName)
                                    return true;
                            }

                            if (mi is PropertyDefinition)
                            {
                                PropertyDefinition propertyDefinition = (PropertyDefinition)i;
                                if (propertyDefinition.GetMethod?.FullName == u.FullName
                                    || propertyDefinition.SetMethod?.FullName == u.FullName)
                                    return true;
                            }

                            if (mi is EventDefinition)
                            {
                                EventDefinition evendDefinition = (EventDefinition)i;
                                if (evendDefinition.AddMethod?.FullName == u.FullName
                                    || evendDefinition.RemoveMethod?.FullName == u.FullName)
                                    return true;
                            }
                            return false;
                        });

                        if (delitem != null)
                            implementedMembers.Remove(delitem);
                    });
                });
            }

            if (!implementedMembers.Any())
                return;

            XmlElement e = (XmlElement)root.SelectSingleNode("Implements");
            if (e == null)
                e = root.OwnerDocument.CreateElement("Implements");

            foreach (var implementedMember in implementedMembers)
            {
                var value = msxdocxSlashdocFormatter.GetDeclaration(implementedMember);
                var interfaceMemberElement = WriteElementWithText(e, "InterfaceMember", value);

                interfaceMemberElement.AddFrameworkToElement(typeEntry.Framework);
                if (typeEntry.IsMemberOnLastFramework(mi))
                {
                    interfaceMemberElement.ClearFrameworkIfAll(typeEntry.AllFrameworkStringForMember(mi));
                }
            }

            if (e.ParentNode == null)
                root.AppendChild(e);
        }

        static void AddXmlNode (XmlElement[] relevant, Func<XmlElement, bool> valueMatches, Action<XmlElement> setValue, Func<XmlElement> makeNewNode, MemberReference member)
        {
            AddXmlNode (relevant, valueMatches, setValue, makeNewNode, member.Module);
        }

        static void AddXmlNode (XmlElement[] relevant, Func<XmlElement, bool> valueMatches, Action<XmlElement> setValue, Func<XmlElement> makeNewNode, TypeDefinition type)
        {
            AddXmlNode (relevant, valueMatches, setValue, makeNewNode, type.Module);
        }

        static XmlElement AddAssemblyXmlNode (XmlElement[] relevant, Func<XmlElement, bool> valueMatches, Action<XmlElement> setValue, Func<XmlElement> makeNewNode, ModuleDefinition module)
        {
            return AddAssemblyXmlNode (relevant, valueMatches, setValue, makeNewNode, module.Assembly.Name);
        }

        static XmlElement AddAssemblyXmlNode (XmlElement[] relevant, Func<XmlElement, bool> valueMatches, Action<XmlElement> setValue, Func<XmlElement> makeNewNode, AssemblyNameReference assembly)
        {
            bool isUnified = MDocUpdater.HasDroppedNamespace (assembly);
            XmlElement thisAssemblyNode = relevant.FirstOrDefault (valueMatches);
            if (thisAssemblyNode == null)
            {
                thisAssemblyNode = makeNewNode ();
            }
            setValue (thisAssemblyNode);

            if (isUnified)
            {
                thisAssemblyNode.AddApiStyle (ApiStyle.Unified);

                foreach (var otherNodes in relevant.Where (n => n != thisAssemblyNode && n.DoesNotHaveApiStyle (ApiStyle.Unified)))
                {
                    otherNodes.AddApiStyle (ApiStyle.Classic);
                }
            }
            return thisAssemblyNode;
        }

        /// <summary>Adds an xml node, reusing the node if it's available</summary>
        /// <param name="relevant">The existing set of nodes</param>
        /// <param name="valueMatches">Checks to see if the node's value matches what you're trying to write.</param>
        /// <param name="setValue">Sets the node's value</param>
        /// <param name="makeNewNode">Creates a new node, if valueMatches returns false.</param>
        static void AddXmlNode (XmlElement[] relevant, Func<XmlElement, bool> valueMatches, Action<XmlElement> setValue, Func<XmlElement> makeNewNode, ModuleDefinition module)
        {
            bool shouldDuplicate = MDocUpdater.HasDroppedNamespace (module);
            var styleToUse = shouldDuplicate ? ApiStyle.Unified : ApiStyle.Classic;
            var existing = relevant;
            bool done = false;
            bool addedOldApiStyle = false;

            if (shouldDuplicate)
            {
                existing = existing.Where (n => n.HasApiStyle (styleToUse)).ToArray ();
                foreach (var n in relevant.Where (n => n.DoesNotHaveApiStyle (styleToUse)))
                {
                    if (valueMatches (n))
                    {
                        done = true;
                    }
                    else
                    {
                        n.AddApiStyle (ApiStyle.Classic);
                        addedOldApiStyle = true;
                    }
                }
            }
            if (!done)
            {
                if (!existing.Any ())
                {
                    var newNode = makeNewNode ();
                    if (shouldDuplicate && addedOldApiStyle)
                    {
                        newNode.AddApiStyle (ApiStyle.Unified);
                    }
                }
                else
                {
                    var itemToReuse = existing.First ();
                    setValue (itemToReuse);

                    if (shouldDuplicate && addedOldApiStyle)
                    {
                        itemToReuse.AddApiStyle (styleToUse);
                    }
                }
            }
        }

        static readonly string[] MemberNodeOrder = {
        "Metadata",
        "MemberSignature",
        "MemberType",
        "Implements",
        "AssemblyInfo",
        "Attributes",
        "ReturnValue",
        "TypeParameters",
        "Parameters",
        "MemberValue",
        "Docs",
        "Excluded",
        "ExcludedLibrary",
        "Link",
    };

        static void OrderMemberNodes (XmlNode member, XmlNodeList children)
        {
            ReorderNodes (member, children, MemberNodeOrder);
        }

        static void ReorderNodes (XmlNode node, XmlNodeList children, string[] ordering)
        {
            MyXmlNodeList newChildren = new MyXmlNodeList (children.Count);
            for (int i = 0; i < ordering.Length; ++i)
            {
                for (int j = 0; j < children.Count; ++j)
                {
                    XmlNode c = children[j];
                    if (c.Name == ordering[i])
                    {
                        newChildren.Add (c);
                    }
                }
            }
            if (newChildren.Count >= 0)
                node.PrependChild ((XmlNode)newChildren[0]);
            for (int i = 1; i < newChildren.Count; ++i)
            {
                XmlNode prev = (XmlNode)newChildren[i - 1];
                XmlNode cur = (XmlNode)newChildren[i];
                node.RemoveChild (cur);
                node.InsertAfter (cur, prev);
            }
        }

        static readonly string[] ValidExtensionMembers = {
        "Docs",
        "MemberSignature",
        "MemberType",
        "Parameters",
        "ReturnValue",
        "TypeParameters",
    };

        static readonly string[] ValidExtensionDocMembers = {
        "param",
        "summary",
        "typeparam",
    };

        private void UpdateExtensionMethods (XmlElement e, DocsNodeInfo info)
        {
            if (!writeIndex)
                return;

            MethodDefinition me = info.Member as MethodDefinition;
            if (me == null)
                return;
            if (info.Parameters.Count < 1)
                return;
            if (!DocUtils.IsExtensionMethod (me))
                return;

            XmlNode em = e.OwnerDocument.CreateElement ("ExtensionMethod");
            XmlNode member = e.CloneNode (true);
            em.AppendChild (member);
            RemoveExcept (member, ValidExtensionMembers);
            RemoveExcept (member.SelectSingleNode ("Docs"), ValidExtensionDocMembers);
            WriteElementText (member, "MemberType", "ExtensionMethod");
            XmlElement link = member.OwnerDocument.CreateElement ("Link");
            var linktype = FormatterManager.SlashdocFormatter.GetName (me.DeclaringType);
            var linkmember = FormatterManager.SlashdocFormatter.GetDeclaration (me);
            link.SetAttribute ("Type", linktype);
            link.SetAttribute ("Member", linkmember);
            member.AppendChild (link);
            AddTargets (em, info);

            if (!IsMultiAssembly || (IsMultiAssembly && !extensionMethods.Any (ex => ex.SelectSingleNode ("Member/Link/@Type").Value == linktype && ex.SelectSingleNode ("Member/Link/@Member").Value == linkmember)))
            {
                extensionMethods.Add (em);
            }
        }

        private static void RemoveExcept (XmlNode node, string[] except)
        {
            if (node == null)
                return;
            MyXmlNodeList remove = null;
            foreach (XmlNode n in node.ChildNodes)
            {
                if (Array.BinarySearch (except, n.Name) < 0)
                {
                    if (remove == null)
                        remove = new MyXmlNodeList ();
                    remove.Add (n);
                }
            }
            if (remove != null)
                foreach (XmlNode n in remove)
                    node.RemoveChild (n);
        }

        private static void AddTargets (XmlNode member, DocsNodeInfo info)
        {
            XmlElement targets = member.OwnerDocument.CreateElement ("Targets");
            member.PrependChild (targets);
            if (!(info.Parameters[0].ParameterType is GenericParameter))
            {
                var reference = info.Parameters[0].ParameterType;
                TypeReference typeReference = reference as TypeReference;
                var declaration = reference != null ?
                    FormatterManager.SlashdocFormatter.GetDeclaration (typeReference) :
                    FormatterManager.SlashdocFormatter.GetDeclaration (reference);

                AppendElementAttributeText (targets, "Target", "Type", declaration);
            }
            else
            {
                GenericParameter gp = (GenericParameter)info.Parameters[0].ParameterType;


#if NEW_CECIL
                Mono.Collections.Generic.Collection<GenericParameterConstraint> constraints = gp.Constraints;
#else
                IList<TypeReference> constraints = gp.Constraints;
#endif
                if (constraints.Count == 0)
                    AppendElementAttributeText (targets, "Target", "Type", "System.Object");
                else
#if NEW_CECIL
               foreach (GenericParameterConstraint c in constraints)
                   AppendElementAttributeText(targets, "Target", "Type",
                       slashdocFormatter.GetDeclaration (c.ConstraintType));
#else
                    foreach (TypeReference c in constraints)
                        AppendElementAttributeText (targets, "Target", "Type",
                            FormatterManager.SlashdocFormatter.GetDeclaration (c));
#endif
            }
        }

        private static bool GetFieldConstValue (FieldDefinition field, out string value)
        {
            value = null;
            TypeDefinition type = field.DeclaringType.Resolve ();

            if (!field.HasConstant)
                return false;
            if (field.IsLiteral)
            {
                object val = field.Constant;
                if (val == null) value = "null";
                else if (val is Enum) value = val.ToString ();
                else if (val is IFormattable)
                {
                    switch (field.FieldType.FullName)
                    {
                        case "System.Double":
                        case "System.Single":
                            value = ((IFormattable)val).ToString("R", CultureInfo.InvariantCulture);
                            break;
                        default:
                            value = ((IFormattable)val).ToString(null, CultureInfo.InvariantCulture);
                            break;
                    }
                    if (val is string)
                        value = "\"" + value + "\"";
                }
                if (value != null && value != "")
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Update forwarding inforamtion of a given type under specified Framework to XmlElement
        /// </summary>
        private void UpdateTypeForwardingChain(XmlElement root, FrameworkTypeEntry typeEntry, TypeDefinition type)
        {
            if (typeEntry.TimesProcessed > 1)
                return;

            string elementName = "TypeForwardingChain";

            var forwardings = new List<MDocResolver.TypeForwardEventArgs>();

            // Get type forwardings of current type and framkework 
            Func<TypeDefinition, List<MDocResolver.TypeForwardEventArgs>> getForwardings = (TypeDefinition typeToQuery) => this.assemblies
                .Where(a => a.Name == typeEntry.Framework.Name)
                .SelectMany(a => a.ForwardingChains(typeToQuery))
                .ToList();

            forwardings.AddRange(getForwardings(type));

            // If current type is nested inner class, add forwardings from outer classes as well
            // Using AddRange here becuase IsForwarder=true is used as filter when import exportedType from metadata
            // And for inner class IsForwarder will be false
            if (type.IsNested)
            {
                var outerType = type.DeclaringType;

                // Handle multiple layers nested
                while (null != outerType)
                {
                    forwardings.AddRange(getForwardings(outerType.Resolve()));
                    outerType = outerType.DeclaringType;
                }
            }

            XmlElement exsitingChain = (XmlElement)root.SelectSingleNode(elementName);

            //  Clean up and gernate from scratch
            if (typeEntry.Framework.IsFirstFrameworkForType(typeEntry))
            {
                ClearElement(root, elementName);
                exsitingChain = null;
            }

            // Update
            if (forwardings.Any())
            {
                exsitingChain = exsitingChain ?? root.OwnerDocument.CreateElement(elementName);
                root.AppendChild(UpdateTypeForwarding(forwardings, typeEntry.Framework.Name, exsitingChain));
            }

            // Purge attribute if needed
            if (typeEntry.Framework.IsLastFrameworkForType(typeEntry) && exsitingChain != null)
                PurgeFrameworkAlternateAttribute(typeEntry, exsitingChain.SelectNodes("TypeForwarding").SafeCast<XmlElement>().ToList());
        }

        private XmlElement UpdateTypeForwarding(IEnumerable<MDocResolver.TypeForwardEventArgs> forwardings, string framework, XmlElement exsitingChain)
        {
            Func<IEnumerable<XmlElement>> elementsQuery = () => exsitingChain.SelectNodes("TypeForwarding").SafeCast<XmlElement>();

            foreach (var forwarding in forwardings)
            {
                var existingElements = elementsQuery()
                    .Where(e => IsTypeForwardingFound(e, forwarding))
                    .FirstOrDefault();

                // Add type forwarding with fxa
                if (existingElements == null)
                {
                    var newElement = exsitingChain.OwnerDocument.CreateElement("TypeForwarding");
                    exsitingChain.AppendChild(newElement);
                    newElement.SetAttribute("From", forwarding.From.Name);
                    newElement.SetAttribute("FromVersion", forwarding.From.Version.ToString());
                    newElement.SetAttribute("To", forwarding.To.Name);
                    newElement.SetAttribute("ToVersion", forwarding.To.Version.ToString());
                    newElement.SetAttribute(Consts.FrameworkAlternate, framework);
                }

                // Update and append fxa
                else
                {
                    string newfxa = FXUtils.AddFXToList(existingElements.GetAttribute(Consts.FrameworkAlternate), framework);
                    existingElements.SetAttribute(Consts.FrameworkAlternate, newfxa);
                }
            }

            return exsitingChain;
        }

        /// <summary>
        /// Purge FrameworkAlternate attribute if it includes all frameworks 
        /// </summary>
        private void PurgeFrameworkAlternateAttribute(FrameworkTypeEntry typeEntry, IEnumerable<XmlElement> nodes)
        {
            var allFrameworks = typeEntry.Framework.AllFrameworksWithType(typeEntry);

            foreach (var node in nodes)
            {
                // if FXAlternate is entire list, just remove it
                if (node.HasAttribute(Consts.FrameworkAlternate) && node.GetAttribute(Consts.FrameworkAlternate) == allFrameworks)
                {
                    node.RemoveAttribute(Consts.FrameworkAlternate);
                }
            }
        }

        /// <summary>
        /// Return true if same type forward found in Xml
        /// </summary>
        private static bool IsTypeForwardingFound(XmlElement element, MDocResolver.TypeForwardEventArgs typeForward)
        {
            return (element.GetAttribute("From") == typeForward.From.Name)
                 && (element.GetAttribute("FromVersion") == typeForward.From.Version.ToString())
                 && (element.GetAttribute("To") == typeForward.To.Name)
                 && (element.GetAttribute("ToVersion") == typeForward.To.Version.ToString());
        }

    // XML HELPER FUNCTIONS

    internal static XmlElement WriteElement (XmlNode parent, string element, bool forceNewElement = false)
        {
            XmlElement ret = parent.ChildNodes.SafeCast<XmlElement>().FirstOrDefault(e => e.LocalName == element);
            if (ret == null || forceNewElement)
            {
                string[] path = element.Split ('/');
                foreach (string p in path)
                {
                    ret = (XmlElement)parent.SelectSingleNode (p);
                    if (ret == null || forceNewElement)
                    {
                        string ename = p;
                        if (ename.IndexOf ('[') >= 0) // strip off XPath predicate
                            ename = ename.Substring (0, ename.IndexOf ('['));
                        ret = parent.OwnerDocument.CreateElement (ename);
                        parent.AppendChild (ret);
                        parent = ret;
                    }
                    else
                    {
                        parent = ret;
                    }
                }
            }
            return ret;
        }

        private static XmlElement WriteElementText (XmlNode parent, string element, string value, bool forceNewElement = false)
        {
            XmlElement node = WriteElement (parent, element, forceNewElement: forceNewElement);
            node.InnerText = value;
            return node;
        }

        internal static XmlElement WriteElementWithText(XmlNode parent, string elementName, string value)
        {
            XmlElement element = parent.ChildNodes.SafeCast<XmlElement>().FirstOrDefault(e => e.Name == elementName && e.InnerText == value);

            // Create element if not exsits
            if (element == null)
            {
                element = parent.OwnerDocument.CreateElement(elementName);
                element.InnerText = value;
                parent.AppendChild(element);
            }
            return element;
        }

        internal static XmlElement WriteElementWithSubElementText(XmlNode parent, string elementName, string subElementName, string value)
        {
            XmlElement element = null;
            XmlElement subElement = null;
            foreach (var elementCandidate in parent.ChildNodes.SafeCast<XmlElement>().Where(e => e.Name == elementName))
            {
                var subElementCandidate = elementCandidate.ChildNodes.SafeCast<XmlElement>().FirstOrDefault(e => e.Name == subElementName && e.InnerText == value);
                if (subElementCandidate != null)
                {
                    element = elementCandidate;
                    subElement = subElementCandidate;
                    break;
                }
            }

            // Create element if not exsits
            if (subElement == null)
            {
                element = parent.OwnerDocument.CreateElement(elementName);
                subElement = parent.OwnerDocument.CreateElement(subElementName);
                subElement.InnerText = value;
                element.AppendChild(subElement);
                parent.AppendChild(element);
            }
            return element;
        }

        static XmlElement AppendElementText (XmlNode parent, string element, string value)
        {
            XmlElement n = parent.OwnerDocument.CreateElement (element);
            parent.AppendChild (n);
            n.InnerText = value;
            return n;
        }

        static XmlElement AppendElementAttributeText (XmlNode parent, string element, string attribute, string value)
        {
            XmlElement n = parent.OwnerDocument.CreateElement (element);
            parent.AppendChild (n);
            n.SetAttribute (attribute, value);
            return n;
        }

        internal static XmlNode CopyNode (XmlNode source, XmlNode dest)
        {
            XmlNode copy = dest.OwnerDocument.ImportNode (source, true);
            dest.AppendChild (copy);
            return copy;
        }

        private static void WriteElementInitialText (XmlElement parent, string element, string value)
        {
            XmlElement node = (XmlElement)parent.SelectSingleNode (element);
            if (node != null)
                return;
            node = WriteElement (parent, element);
            node.InnerText = value;
        }

        private static XmlElement WriteElementAttribute (XmlElement node, string attribute, string value)
        {
            if (node.GetAttribute (attribute) != value)
            {
                node.SetAttribute (attribute, value);
            }
            return node;
        }

        internal static void ClearElement (XmlElement parent, string name)
        {
            XmlElement node = (XmlElement)parent.SelectSingleNode (name);
            if (node != null)
                parent.RemoveChild (node);
        }

        // DOCUMENTATION HELPER FUNCTIONS

        private void MakeDocNode (DocsNodeInfo info, IEnumerable<DocumentationImporter> setimporters, FrameworkTypeEntry typeEntry)
        {
            List<GenericParameter> genericParams = info.GenericParameters;
            IList<ParameterDefinition> parameters = info.Parameters;
            TypeReference returntype = info.ReturnType;
            bool returnisreturn = info.ReturnIsReturn;
            XmlElement e = info.Node;
            bool addremarks = info.AddRemarks;

            WriteElementInitialText (e, "summary", "To be added.");

            if (parameters != null)
            {
                string[] values = new string[parameters.Count];
                for (int i = 0; i < values.Length; ++i)
                    values[i] = parameters[i].Name;
                UpdateParameters (e, "param", values, typeEntry);
            }

            if (genericParams != null)
            {
                string[] values = new string[genericParams.Count];
                for (int i = 0; i < values.Length; ++i)
                    values[i] = genericParams[i].Name;
                UpdateParameters (e, "typeparam", values, typeEntry);
            }

            Predicate<string> CheckRemoveByImporter =
               (string filter) =>
               {
                   foreach (DocumentationImporter i in importers)
                   {
                       if (i.CheckRemoveByMapping(info, filter))
                           return true;
                   }

                   if (setimporters != null)
                   {
                       foreach (var i in setimporters)
                       {
                           if (i.CheckRemoveByMapping(info, filter))
                               return true;
                       }
                   }

                   return false;
               };

            string retnodename = null;
            if (returntype != null && returntype.FullName != "System.Void")
            { // FIXME
                retnodename = returnisreturn ? "returns" : "value";
                string retnodename_other = !returnisreturn ? "returns" : "value";

                // If it has a returns node instead of a value node, change its name.
                XmlElement retother = (XmlElement)e.SelectSingleNode (retnodename_other);
                if (retother != null)
                {
                    XmlElement retnode = e.OwnerDocument.CreateElement (retnodename);
                    foreach (XmlNode node in retother)
                        retnode.AppendChild (node.CloneNode (true));
                    e.ReplaceChild (retnode, retother);
                }
                else
                {
                    WriteElementInitialText (e, retnodename, "To be added.");
                }
            }
            else
            {
                var commMemberKeys = new string[] { "returns", "value" };
                for (int i = 0; i < commMemberKeys.Length; i++)
                {
                    if (DocUtils.NeedsOverwrite(e[commMemberKeys[i]]))
                        if (DocUtils.CheckRemoveByImporter(info, commMemberKeys[i], importers, setimporters))
                            ClearElement(e, commMemberKeys[i]);
                }              
            }

            if (DocUtils.NeedsOverwrite(e["related"]))
                if (DocUtils.CheckRemoveByImporter(info, "related", importers, setimporters))
                    ClearElement(e, "related");

            var altMemberKeys = new string[] { "altmember", "seealso" };
            for (int i = 0; i < altMemberKeys.Length; i++)
            {
                if (DocUtils.NeedsOverwrite(e["altmember"]))
                    if (DocUtils.CheckRemoveByImporter(info, altMemberKeys[i], importers, setimporters))
                        ClearElement(e, "altmember");
            }

            if (addremarks)
                WriteElementInitialText (e, "remarks", "To be added.");

            if (exceptions.HasValue && info.Member != null &&
                    (exceptions.Value & ExceptionLocations.AddedMembers) == 0)
            {
                UpdateExceptions (e, info.Member);
            }

            foreach (DocumentationImporter importer in importers)
            {
                importer.ImportDocumentation (info);
            }
            if (setimporters != null)
            {
                foreach (var i in setimporters)
                    i.ImportDocumentation (info);
            }

            OrderDocsNodes (e, e.ChildNodes);
            NormalizeWhitespace (e);
        }

        static readonly string[] DocsNodeOrder = {
        "typeparam", "param", "summary", "returns", "value", "remarks",
    };

        private static void OrderDocsNodes (XmlNode docs, XmlNodeList children)
        {
            ReorderNodes (docs, children, DocsNodeOrder);
        }

        public void UpdateParameters (XmlElement e, string element, string[] values, FrameworkTypeEntry typeEntry)
        {
            if (e.Name != "Docs") // make sure we're working with the Docs node
                e = (e.SelectSingleNode("Docs") as XmlElement) ?? e;

            string parentElement = element == "typeparam" ? "TypeParameter" : "Parameter";
            string rootParentElement = element == "typeparam" ? "TypeParameters" : "Parameters";

            if (values != null)
            {
                // Add whichever `param` values aren't present from `values`
                Dictionary<string, StringList> seen = new Dictionary<string, StringList>(values.Length);
                for (int i = 0; i < values.Length; i++)
                {
                    var value = values[i];

                    if (!seen.ContainsKey(value))
                        seen.Add(value, new StringList(2));

                    var seenlist = seen[value];
                    seenlist.Add(value);

                    // query for `param` of `value`
                    var existingParameters = e.SelectNodes(element + "[@name='" + value + "']");
                    
                    // if not exists, add
                    if (existingParameters.Count < seenlist.Count)
                    {
                        XmlElement pe = e.OwnerDocument.CreateElement(element);
                        pe.SetAttribute("name", value);
                        // TODO: set index attribute with `i`
                        pe.InnerText = "To be added.";
                        e.AppendChild(pe);
                    }
                }
                seen = null;

                // on last, get a list of all parent elements, and then nuke `param` if name doesn't match one on the list
                if (typeEntry.IsOnLastFramework)
                {
                    var mainRoots = e.ParentNode.SelectNodes($"{rootParentElement}/{parentElement}")
                                 .Cast<XmlElement>()
                                 .GroupBy(pe => pe.GetAttribute("Name"))
                                 .ToDictionary(k => k.Key, k => k);

                    // query all `param`
                    var paramsToDelete = new List<XmlElement>(1);
                    foreach (var paramnodes in e.SelectNodes(element).Cast<XmlElement>().GroupBy(el => el.GetAttribute("name") ).ToArray())
                    {
                        // if doesn't exist in `mainRoots` ... delete
                        var pname = paramnodes.Key;
                        bool containsRoot = mainRoots.ContainsKey(pname);
                        if (!containsRoot || mainRoots[pname].Count() < paramnodes.Count())
                        {
                            int rootCount = containsRoot ? mainRoots[pname].Count() : 0;
                            int currentParamCount = 0;
                            foreach (var paramnode in paramnodes)
                            {
                                currentParamCount++;

                                if (currentParamCount <= rootCount) continue;

                                bool istba = paramnode.InnerText.StartsWith("To be added", StringComparison.Ordinal);
                                if (delete || istba)
                                    paramsToDelete.Add(paramnode);
                                else
                                    Warning($"Would have deleted param '{pname}', but -delete is {delete}{(istba ? "" : " it's not empty ('" + paramnode.InnerText + "')") }");

                            }
                        }
                    }

                    for (int i = 0; i < paramsToDelete.Count; i++)
                    {
                        var item = paramsToDelete[i];
                        item.ParentNode.RemoveChild(item);
                    }

                    // sort
                    var paramParent = e.ParentNode.SelectSingleNode(rootParentElement);
                    if (paramParent != null)
                        SortXmlNodes(
                            e,
                            e.SelectNodes(element),
                            new MemberParameterNameComparer(paramParent as XmlElement, parentElement));
                }
            }
        }

        private static void UpdateParameterName (XmlElement docs, XmlElement pe, string newName)
        {
            string existingName = pe.GetAttribute ("name");
            pe.SetAttribute ("name", newName);
            if (existingName == newName)
                return;
            foreach (XmlElement paramref in docs.SelectNodes (".//paramref"))
                if (paramref.GetAttribute ("name").Trim () == existingName)
                    paramref.SetAttribute ("name", newName);
        }

        class CrefComparer : XmlNodeComparer
        {

            public override int Compare (XmlNode x, XmlNode y)
            {
                string xType = x.Attributes["cref"].Value;
                string yType = y.Attributes["cref"].Value;
                string xNamespace = GetNamespace (xType);
                string yNamespace = GetNamespace (yType);

                int c = xNamespace.CompareTo (yNamespace);
                if (c != 0)
                    return c;
                return xType.CompareTo (yType);
            }

            static string GetNamespace (string type)
            {
                int n = type.LastIndexOf ('.');
                if (n >= 0)
                    return type.Substring (0, n);
                return string.Empty;
            }
        }

        private void UpdateExceptions (XmlNode docs, MemberReference member)
        {
            string indent = new string (' ', 10);
            foreach (var source in new ExceptionLookup (exceptions.Value)[member])
            {
                string cref = FormatterManager.SlashdocFormatter.GetDeclaration (source.Exception);
                var node = docs.SelectSingleNode ("exception[@cref='" + cref + "']");
                if (node != null)
                    continue;
                XmlElement e = docs.OwnerDocument.CreateElement ("exception");
                e.SetAttribute ("cref", cref);
                e.InnerXml = "To be added; from:\n" + indent + "<see cref=\"" +
                    string.Join ("\" />,\n" + indent + "<see cref=\"",
                            source.Sources.Select (m => msxdocxSlashdocFormatter.GetDeclaration (m))
                            .OrderBy (s => s)) +
                    "\" />";
                docs.AppendChild (e);
            }
            SortXmlNodes (docs, docs.SelectNodes ("exception"),
                    new CrefComparer ());
        }

        private static void NormalizeWhitespace (XmlElement e)
        {
            if (e == null)
                return;
            
            // Remove all text and whitespace nodes from the element so it
            // is outputted with nice indentation and no blank lines.
            ArrayList deleteNodes = new ArrayList ();
            foreach (XmlNode n in e)
                if (n is XmlText || n is XmlWhitespace || n is XmlSignificantWhitespace)
                    deleteNodes.Add (n);
            foreach (XmlNode n in deleteNodes)
                n.ParentNode.RemoveChild (n);
        }

        private bool UpdateAssemblyVersions (XmlElement root, MemberReference member, bool add)
        {
            TypeDefinition type = member as TypeDefinition;
            if (type == null)
                type = member.DeclaringType as TypeDefinition;

            var versions = new string[] { GetAssemblyVersion (type.Module.Assembly) };

            if (root.LocalName == "AssemblyInfo")
                return UpdateAssemblyVersionForAssemblyInfo (root, root.ParentNode as XmlElement, versions, add: true);
            else
                return UpdateAssemblyVersions (root, type.Module.Assembly, versions, add);
        }

        private static string GetAssemblyVersion (AssemblyDefinition assembly)
        {
            return assembly.Name.Version.ToString ();
        }

        private bool UpdateAssemblyVersions (XmlElement root, AssemblyDefinition assembly, string[] assemblyVersions, bool add)
        {
            if (IsMultiAssembly)
                return false;

            XmlElement av = (XmlElement)root.SelectSingleNode ("AssemblyVersions");
            if (av != null)
            {
                // AssemblyVersions is not part of the spec
                root.RemoveChild (av);
            }

            string oldNodeFilter = "AssemblyInfo[not(@apistyle) or @apistyle='classic']";
            string newNodeFilter = "AssemblyInfo[@apistyle='unified']";
            string thisNodeFilter = MDocUpdater.HasDroppedNamespace (assembly) ? newNodeFilter : oldNodeFilter;
            string thatNodeFilter = MDocUpdater.HasDroppedNamespace (assembly) ? oldNodeFilter : newNodeFilter;

            XmlElement e = (XmlElement)root.SelectSingleNode (thisNodeFilter);
            if (e == null)
            {
                e = root.OwnerDocument.CreateElement ("AssemblyInfo");

                if (MDocUpdater.HasDroppedNamespace (assembly))
                {
                    e.AddApiStyle (ApiStyle.Unified);
                }

                root.AppendChild (e);
            }

            var thatNode = (XmlElement)root.SelectSingleNode (thatNodeFilter);
            if (MDocUpdater.HasDroppedNamespace (assembly) && thatNode != null)
            {
                // there's a classic node, we should add apistyles
                e.AddApiStyle (ApiStyle.Unified);
                thatNode.AddApiStyle (ApiStyle.Classic);
            }

            return UpdateAssemblyVersionForAssemblyInfo (e, root, assemblyVersions, add);
        }

        static bool UpdateAssemblyVersionForAssemblyInfo (XmlElement e, XmlElement root, string[] assemblyVersions, bool add)
        {
            List<XmlNode> matches = e.SelectNodes ("AssemblyVersion").Cast<XmlNode> ().Where (v => Array.IndexOf (assemblyVersions, v.InnerText) >= 0).ToList ();
            // matches.Count > 0 && add: ignore -- already present
            if (matches.Count > 0 && !add)
            {
                foreach (XmlNode c in matches)
                    e.RemoveChild (c);
            }
            else if (matches.Count == 0 && add)
            {
                foreach (string sv in assemblyVersions)
                {
                    XmlElement c = root.OwnerDocument.CreateElement ("AssemblyVersion");
                    c.InnerText = sv;
                    e.AppendChild (c);
                }
            }

            // matches.Count == 0 && !add: ignore -- already not present
            XmlNodeList avs = e.SelectNodes ("AssemblyVersion");
            SortXmlNodes (e, avs, new VersionComparer ());

            bool anyNodesLeft = avs.Count != 0;
            if (!anyNodesLeft)
            {
                e.ParentNode.RemoveChild (e);
            }
            return anyNodesLeft;
        }

        public static void MakeAssemblyAttributes(
            XmlElement root,
            FrameworkEntry fx,
            AssemblyDefinition assembly)
        {
            if (assembly == null || !assembly.HasCustomAttributes)
            {
                return;
            }
            var assemblyName = assembly.Name.Name;
            MakeAttributes(
                root,
                AttributeFormatter.PreProcessCustomAttributes(assembly.CustomAttributes),
                fx.IsFirstFrameworkForAssembly(assemblyName),
                fx.IsLastFrameworkForAssembly(assemblyName),
                () => fx.AllFrameworksStringWithAssembly(assemblyName),
                null,
                perLanguage: false);
        }

        public static void MakeAttributes(
            XmlElement root,
            IEnumerable<(CustomAttribute, string)> customAttributesWithPrefix,
            FrameworkTypeEntry typeEntry)
        {
            MakeAttributes(
                root,
                customAttributesWithPrefix,
                typeEntry.Framework.IsFirstFrameworkForType(typeEntry),
                typeEntry.Framework.IsLastFrameworkForType(typeEntry),
                () => typeEntry.Framework.AllFrameworksWithType(typeEntry),
                typeEntry,
                perLanguage: true);
        }

        public static void MakeMemberAttributes(
            XmlElement root,
            MemberReference member,
            FrameworkTypeEntry typeEntry)
        {
            MakeAttributes(
                root,
                AttributeFormatter.GetCustomAttributes(member),
                typeEntry.IsMemberOnFirstFramework(member),
                typeEntry.IsMemberOnLastFramework(member),
                () => typeEntry.Framework.AllFrameworksWithType(typeEntry),
                typeEntry,
                perLanguage: true);
        }

        public static void MakeParamsAttributes(
            XmlElement root,
            IEnumerable<(CustomAttribute, string)> customAttributesWithPrefix,
            FrameworkTypeEntry typeEntry,
            MemberReference member)
        {
            if (member is TypeDefinition t)
            {
                MakeAttributes(root, customAttributesWithPrefix, typeEntry);
            }
            else
            {
                MakeAttributes(
                    root,
                    customAttributesWithPrefix,
                    typeEntry.IsMemberOnFirstFramework(member),
                    typeEntry.IsMemberOnLastFramework(member),
                    () => typeEntry.AllFrameworkStringForMember(member),
                    typeEntry,
                    perLanguage: true);
            }
        }

        /// <summary>
        /// Update attributes in parent xml node
        /// </summary>
        /// <param name="root">parent xml node</param>
        /// <param name="customAttributesWithPrefix">attributes and prefix</param>
        /// <param name="isFirstFramework"></param>
        /// <param name="isLastFramework"></param>
        /// <param name="getAllFxString"></param>
        /// <param name="typeEntry"></param>
        /// <param name="perLanguage"></param>
        public static void MakeAttributes(
            XmlElement root,
            IEnumerable<(CustomAttribute, string)> customAttributesWithPrefix,
            bool isFirstFramework,
            bool isLastFramework,
            Func<string> getAllFxString,
            FrameworkTypeEntry typeEntry,
            bool perLanguage = true
            )
        {
            const string Language = "Language";
            XmlElement e = (XmlElement)root.SelectSingleNode("Attributes");
            if (e == null)
                e = root.OwnerDocument.CreateElement("Attributes");
            if (isFirstFramework && typeEntry?.TimesProcessed == 1)
            {
                e.RemoveAll();
            }
            if (customAttributesWithPrefix != null)
            {
                foreach (var customAttrWithPrefix in customAttributesWithPrefix)
                {
                    var customAttr = customAttrWithPrefix.Item1;
                    var prefix = customAttrWithPrefix.Item2;
                    if (FormatterManager.CSharpAttributeFormatter.TryGetAttributeString(customAttr, out string csharpAttrStr, prefix, perLanguage))
                    {
                        DocUtils.AddElementWithFx(
                            typeEntry,
                            parent: e,
                            isFirst: isFirstFramework,
                            isLast: isLastFramework,
                            allfxstring: new Lazy<string>(() => getAllFxString()),
                            clear: parent => { },
                            findExisting: parent =>
                            {
                                return parent.ChildNodes.Cast<XmlElement>().FirstOrDefault(
                                    rt =>
                                    {
                                        var text = rt.SelectSingleNode("/AttributeName[@" + Language + "='" + Consts.CSharp + "']")?.InnerText
                                          ?? rt.FirstChild.InnerText;
                                        return text == csharpAttrStr;
                                    });
                            },
                            addItem: parent =>
                            {
                                XmlElement ae = root.OwnerDocument.CreateElement("Attribute");
                                e.AppendChild(ae);
                                var node = WriteElementText(ae, "AttributeName", csharpAttrStr, forceNewElement: true);
                                if (perLanguage)
                                {
                                    WriteElementAttribute(node, Language, Consts.CSharp);

                                    foreach (var formatter in FormatterManager.AdditionalAttributeFormatters)
                                    {
                                        if (formatter.TryGetAttributeString(customAttr, out string attrStr, prefix))
                                        {
                                            node = WriteElementText(ae, "AttributeName", attrStr, forceNewElement: true);
                                            WriteElementAttribute(node, Language, formatter.Language);
                                        }
                                    }
                                }

                                return ae;
                            });
                    }
                }
            }

            if (e != null && e.ParentNode == null)
                root.AppendChild(e);

            if (e.ChildNodes.Count == 0 && e.ParentNode != null)
            {
                var parent = e.ParentNode as XmlElement;
                parent.RemoveChild(e);
                if (parent.ChildNodes.Count == 0)
                    parent.IsEmpty = true;
                return;
            }

            NormalizeWhitespace(e);
        }

        private bool ProcessedMoreThanOnce(FrameworkTypeEntry typeEntry)
        {
            if (typeEntry.TimesProcessed <= 1)
            {
                return false;
            }
            else
            {
                var assemblies = this.assemblies.Where(a => a.Name == typeEntry.Framework.Name).ToList();
                return assemblies.Any(a => a.IsTypeForwardingTo(typeEntry));
            }
        }

        public void MakeParameters (XmlElement root, MemberReference member, IList<ParameterDefinition> parameters, FrameworkTypeEntry typeEntry, ref bool fxAlternateTriggered, bool shouldDuplicateWithNew = false)
        {
            if (ProcessedMoreThanOnce(typeEntry))
                return;

            XmlElement e = WriteElement (root, "Parameters");

            if (typeEntry.Framework.IsFirstFrameworkForType(typeEntry))
            {
                e.RemoveAll();
            }

            #region helper functions
            /// addParameter does the work of adding the actual parameter to the XML
            Action<ParameterDefinition, XmlElement, string, int, bool, string, bool> addParameter = (ParameterDefinition param, XmlElement nextTo, string paramType, int index, bool addIndex, string fx, bool addfx) =>
            {
                var pe = root.OwnerDocument.CreateElement ("Parameter");

                if (nextTo == null)
                    e.AppendChild (pe);
                else
                    e.InsertAfter (pe, nextTo);

                pe.SetAttribute ("Name", param.Name);
                pe.SetAttribute ("Type", paramType);
                if (param.ParameterType is ByReferenceType)
                {
                    if (param.IsOut)
                        pe.SetAttribute ("RefType", "out");
                    else
                        pe.SetAttribute ("RefType", "ref");
                }
                //if (addIndex)
                    pe.SetAttribute (Consts.Index, index.ToString ());
                //if (addfx)
                    pe.SetAttribute (Consts.FrameworkAlternate, fx);

				MakeParamsAttributes (pe, AttributeFormatter.PreProcessCustomAttributes(param.CustomAttributes), typeEntry, member);
            };
            /// addFXAttributes, adds the index attribute to all existing elements.
            /// Used when we first detect the scenario which requires this.
            Action<IEnumerable<XmlElement>> addFXAttributes = nodes =>
            {
                var i = 0;
                foreach (var node in nodes)
                {
                    if (!node.HasAttribute(Consts.Index))
                    {
                        node.SetAttribute(Consts.Index, i.ToString());
                    }
                    i++;
                }
            };
            #endregion

            // Gather information about this method's parameters
            var pdata = parameters.Select ((p, i) =>
            {
                return new
                {
                    Name = p.Name,
                    Type = GetDocParameterType(p.ParameterType),
                    Index = i,
                    IsOut = p.IsOut,
                    IsIn = p.IsIn,
                    Definition = p
                };
            }).ToArray ();

            // Gather information about current XMl state
            var xdata = e.GetElementsByTagName ("Parameter")
                         .Cast<XmlElement> ()
                         .Select ((n, i) =>
                         {
                             int actualIndex = i;
                             if (n.HasAttribute (Consts.Index))
                                 int.TryParse (n.GetAttribute (Consts.Index), out actualIndex);


                             return new
                             {
                                 Element = n,
                                 Name = n.GetAttribute ("Name"),
                                 Type = n.GetAttribute ("Type"),
                                 ChildIndex = i,
                                 ActualIndex = actualIndex,
                                 FrameworkAlternates = n.GetAttribute (Consts.FrameworkAlternate)
                             };
                         })
                         .ToArray ();

            // Now sync up the state
            for (int i=0; i < pdata.Length; i++) {
                var p = pdata[i];
                // check for name
                
                var xitems = xdata.Where (x => x.Name == p.Name);
                var xitem = xitems.SingleOrDefault(x => x.ActualIndex == i);

                if (xitem != null)
                {
                    var xelement = xitem.Element;

                    // update the type name. This supports the migration to a
                    // formal `RefType` attribute, rather than appending `&` to the Type attribute.
                    xelement.SetAttribute("Type", p.Type);

                    // set FXA Values (they'll be filtered out on the last run if necessary)
                    var fxaValue = FXUtils.AddFXToList(xelement.GetAttribute(Consts.FrameworkAlternate), typeEntry.Framework.Name);
                    xelement.RemoveAttribute(Consts.FrameworkAlternate);
                    xelement.RemoveAttribute(Consts.Index);
                    xelement.SetAttribute(Consts.Index, i.ToString());
                    xelement.SetAttribute (Consts.FrameworkAlternate, fxaValue);
                    
                    continue;
                }
                else {
                    // if no check actualIndex and type
                    if (xdata.Any (x => x.ActualIndex == i && x.Type == p.Type))
                    {
                        // TODO: this probably needs to be a bit smarter about "" params
                        addFXAttributes (xdata.Select (x => x.Element));
                        //-find type in previous frameworks

                        //-find < parameter where index = currentIndex >
                        var existingElements = xdata.Where(x => x.ActualIndex == i && x.Type == p.Type).ToArray();
                        foreach (var currentNode in existingElements.Select(el => el.Element))
                        {

                            if (!currentNode.HasAttribute(Consts.FrameworkAlternate))
                            {
                                string fxList = FXUtils.PreviouslyProcessedFXString(typeEntry);
                                currentNode.SetAttribute(Consts.FrameworkAlternate, fxList);
                            }
                        }

                        addParameter (p.Definition, existingElements.Last().Element, p.Type, i, true, typeEntry.Framework.Name, true);

                        fxAlternateTriggered = true;
                    }
                    else
                    {
                        // if no, make it
                        int lastIndex = i - 1;
                        XmlElement lastElement = lastIndex > -1 && lastIndex < xdata.Length ? xdata[lastIndex].Element : null;
                        addParameter (p.Definition, lastElement, p.Type, i, false, typeEntry.Framework.Name, false);
                    }
                }
            }

            //-purge `typeEntry.Framework` from any<parameter> that 
            // has FrameworkAlternate, and “name” doesn’t match any 
            // `parameters`
            var paramNodes = e.GetElementsByTagName ("Parameter");
            var alternates = paramNodes
                .Cast<XmlElement> ()
                .Select (p => new
                {
                    Element = p,
                    Name = p.GetAttribute ("Name"),
                    HasFrameworkAlternate = p.HasAttribute (Consts.FrameworkAlternate),
                    FrameworkAlternate = p.GetAttribute (Consts.FrameworkAlternate)
                })
                .Where (p =>
                        p.HasFrameworkAlternate && 
                        ((!string.IsNullOrWhiteSpace (p.FrameworkAlternate) &&
                          p.FrameworkAlternate.Contains (typeEntry.Framework.Name)) ||
                         (string.IsNullOrWhiteSpace (p.FrameworkAlternate))) &&
                        !parameters.Any (param => param.Name == p.Name))
                .ToArray ();
            if (alternates.Any ())
            {
                foreach (var a in alternates)
                {
                    string newValue = FXUtils.RemoveFXFromList (a.FrameworkAlternate, typeEntry.Framework.Name);
                    if (string.IsNullOrWhiteSpace (newValue))
                    {
                        a.Element.ParentNode.RemoveChild (a.Element);
                    }
                    else
                    {
                        a.Element.SetAttribute (Consts.FrameworkAlternate, newValue);
                    }
                }
            }

            if (typeEntry.Framework.IsLastFrameworkForType(typeEntry))
            {
                // Now clean up
                var allFrameworks = typeEntry.Framework.AllFrameworksWithType(typeEntry);
                var finalNodes = paramNodes
                    .Cast<XmlElement> ().ToArray ();
                foreach (var parameter in finalNodes)
                {
                    // if FXAlternate is entire list, just remove it
                    if (parameter.HasAttribute (Consts.FrameworkAlternate) && parameter.GetAttribute (Consts.FrameworkAlternate) == allFrameworks)
                    {
                        parameter.RemoveAttribute (Consts.FrameworkAlternate);
                    }
                }

                // if there are no fx attributes left, just remove the indices entirely
                if (!finalNodes.Any (n => n.HasAttribute (Consts.FrameworkAlternate)))
                {
                    foreach (var parameter in finalNodes)
                        parameter.RemoveAttribute (Consts.Index);
                }
            }
        }

        private void MakeTypeParameters (FrameworkTypeEntry entry, XmlElement root, IList<GenericParameter> typeParams, MemberReference member, bool shouldDuplicateWithNew)
        {
            if (typeParams == null || typeParams.Count == 0)
            {
                XmlElement f = (XmlElement)root.SelectSingleNode ("TypeParameters");
                if (f != null)
                    root.RemoveChild (f);
                return;
            }

            XmlElement e = WriteElement (root, "TypeParameters");

            var nodes = e.SelectNodes ("TypeParameter").Cast<XmlElement> ().ToArray ();

            foreach (GenericParameter t in typeParams)
            {

#if NEW_CECIL
                Mono.Collections.Generic.Collection<GenericParameterConstraint> constraints = t.Constraints;
#else
                IList<TypeReference> constraints = t.Constraints;
#endif
                GenericParameterAttributes attrs = t.Attributes;

                var existing = nodes.FirstOrDefault(x => x.GetAttribute("Name") == t.Name);
                if (existing != null)
                {
                    MakeParamsAttributes(existing, AttributeFormatter.PreProcessCustomAttributes(t.CustomAttributes), entry, member);
                }
                else
                {
                    XmlElement pe = root.OwnerDocument.CreateElement("TypeParameter");
                    e.AppendChild(pe);
                    pe.SetAttribute("Name", t.Name);
                    MakeParamsAttributes(pe, AttributeFormatter.PreProcessCustomAttributes(t.CustomAttributes), entry, member);
                    XmlElement ce = (XmlElement)e.SelectSingleNode("Constraints");
                    if (attrs == GenericParameterAttributes.NonVariant && constraints.Count == 0)
                    {
                        if (ce != null)
                            e.RemoveChild(ce);
                    }
                    if (ce != null)
                        ce.RemoveAll();
                    else
                    {
                        ce = root.OwnerDocument.CreateElement("Constraints");
                    }
                    if ((attrs & GenericParameterAttributes.Contravariant) != 0)
                        AppendElementText(ce, "ParameterAttribute", "Contravariant");
                    if ((attrs & GenericParameterAttributes.Covariant) != 0)
                        AppendElementText(ce, "ParameterAttribute", "Covariant");
                    if ((attrs & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
                        AppendElementText(ce, "ParameterAttribute", "DefaultConstructorConstraint");
                    if ((attrs & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                        AppendElementText(ce, "ParameterAttribute", "NotNullableValueTypeConstraint");
                    if ((attrs & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                        AppendElementText(ce, "ParameterAttribute", "ReferenceTypeConstraint");

#if NEW_CECIL
                       foreach (GenericParameterConstraint c in constraints)
                       {
                           TypeDefinition cd = c.ConstraintType.Resolve ();
                            AppendElementText (ce,
                                    (cd != null && cd.IsInterface) ? "InterfaceName" : "BaseTypeName",
                                    GetDocTypeFullName (c.ConstraintType));
                        }
#else
                    foreach (TypeReference c in constraints)
                    {
                        TypeDefinition cd = c.Resolve();
                        AppendElementText(ce,
                                (cd != null && cd.IsInterface) ? "InterfaceName" : "BaseTypeName",
                                GetDocTypeFullName(c));
                    }
#endif
                    if (ce.HasChildNodes)
                    {
                        pe.AppendChild(ce);
                    }
                }
            }
        }

        private void MakeParameters (XmlElement root, MemberReference mi, FrameworkTypeEntry typeEntry, ref bool fxAlternateTriggered, bool shouldDuplicateWithNew)
        {
            if (mi is MethodDefinition && ((MethodDefinition)mi).IsConstructor)
                MakeParameters (root, mi, ((MethodDefinition)mi).Parameters, typeEntry, ref fxAlternateTriggered, shouldDuplicateWithNew);
            else if (mi is MethodDefinition)
            {
                MethodDefinition mb = (MethodDefinition)mi;
                IList<ParameterDefinition> parameters = mb.Parameters;
                MakeParameters (root, mi, parameters, typeEntry, ref fxAlternateTriggered, shouldDuplicateWithNew);
                if (parameters.Count > 0 && DocUtils.IsExtensionMethod (mb))
                {
                    XmlElement p = (XmlElement)root.SelectSingleNode ("Parameters/Parameter[position()=1]");
                    p?.SetAttribute ("RefType", "this");
                }
            }
            else if (mi is PropertyDefinition)
            {
                IList<ParameterDefinition> parameters = ((PropertyDefinition)mi).Parameters;
                if (parameters.Count > 0)
                    MakeParameters (root, mi, parameters, typeEntry, ref fxAlternateTriggered, shouldDuplicateWithNew);
                else
                    return;
            }
            else if (mi is FieldDefinition) return;
            else if (mi is EventDefinition) return;
            else if (mi is AttachedEventReference) return;
            else if (mi is AttachedPropertyReference) return;
            else throw new ArgumentException ();
        }

        public static string GetDocParameterType (TypeReference type, bool useTypeProjection = false)
        {
            var typename = GetDocTypeFullName (type, useTypeProjection).Replace ("@", "&");

            if (useTypeProjection || string.IsNullOrEmpty(typename))
            {
                typename = MDocUpdater.Instance.TypeMap?.GetTypeName("C#", typename) ?? typename;
            }
            
            return typename;
        }

        private void MakeReturnValue (FrameworkTypeEntry typeEntry, XmlElement root, TypeReference type, MemberReference member, IList<CustomAttribute> attributes, bool shouldDuplicateWithNew = false)
        {
            XmlElement e = WriteElement (root, "ReturnValue");
            var valueToUse = GetDocTypeFullName (type, false);
            if ((type.IsRequiredModifier && ((RequiredModifierType)type).ElementType.IsByReference)
                    || type.IsByReference)
                e.SetAttribute("RefType", "Ref");

            if (type.IsRequiredModifier && IsReadonlyAttribute(attributes))
            {
                e.SetAttribute("RefType", "Readonly");
                if (valueToUse[valueToUse.Length - 1] == '&')
                    valueToUse = valueToUse.Remove(valueToUse.Length - 1);
            }


            DocUtils.AddElementWithFx(
                    typeEntry,
                    parent: e,
                    isFirst: typeEntry.IsMemberOnFirstFramework(member),// typeEntry.Framework.IsFirstFrameworkForType(typeEntry),
                    isLast: typeEntry.IsMemberOnLastFramework(member),// typeEntry.Framework.IsLastFrameworkForType(typeEntry),
                    allfxstring: new Lazy<string>(() => typeEntry.AllFrameworkStringForMember(member)),
                    clear: parent =>
                    {
                        parent.RemoveAll();
                    },
                    findExisting: parent =>
                    {
                        return parent.ChildNodes.Cast<XmlElement>().SingleOrDefault(rt => rt.Name == "ReturnType" && rt.InnerText == valueToUse);
                    },
                    addItem: parent =>
                    {
                        var newNode = WriteElementText(e, "ReturnType", valueToUse, forceNewElement: true);
                        if (attributes != null)
                            MakeParamsAttributes(e, AttributeFormatter.PreProcessCustomAttributes(attributes), typeEntry, member);

                        return newNode;
                    });
        }

        private bool IsReadonlyAttribute(IList<CustomAttribute> attributes)
        {
            if (attributes == null) return false;

            foreach (var attribute in attributes)
            {
                if (attribute?.AttributeType?.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute")
                {
                    return true;
                }
            }
            return false;
        }

        private void MakeReturnValue (FrameworkTypeEntry typeEntry, XmlElement root, MemberReference mi, bool shouldDuplicateWithNew = false)
        {
            if (mi is MethodDefinition && ((MethodDefinition)mi).IsConstructor)
                return;
            else if (mi is MethodDefinition)
                MakeReturnValue (typeEntry, root, ((MethodDefinition)mi).ReturnType,mi, ((MethodDefinition)mi).MethodReturnType.CustomAttributes, shouldDuplicateWithNew);
            else if (mi is PropertyDefinition)
                MakeReturnValue (typeEntry, root, ((PropertyDefinition)mi).PropertyType, mi, null, shouldDuplicateWithNew);
            else if (mi is FieldDefinition)
                MakeReturnValue (typeEntry, root, ((FieldDefinition)mi).FieldType, mi, null, shouldDuplicateWithNew);
            else if (mi is EventDefinition)
                MakeReturnValue (typeEntry, root, ((EventDefinition)mi).EventType, mi, null, shouldDuplicateWithNew);
            else if (mi is AttachedEventReference)
                return;
            else if (mi is AttachedPropertyReference)
                return;
            else
                throw new ArgumentException (mi + " is a " + mi.GetType ().FullName);
        }

        private XmlElement MakeMember (XmlDocument doc, DocsNodeInfo info, XmlNode members, FrameworkTypeEntry typeEntry, Dictionary<string, List<MemberReference>> iplementedMembers, IEnumerable<Eiimembers> eiiMenbers)
        {
            MemberReference mi = info.Member;
            if (mi is TypeDefinition) return null;

            string sigs = FormatterManager.MemberFormatters[0].GetDeclaration (mi);
            if (sigs == null) return null; // not publicly visible

            if (DocUtils.IsIgnored(mi))
                return null;

            XmlElement me = doc.CreateElement ("Member");
            members.AppendChild (me);
            var memberName = GetMemberName(mi);
            me.SetAttribute ("MemberName", memberName);

            AddEiiNameAsAttribute(mi, me, memberName);

            info.Node = me;
            UpdateMember (info, typeEntry, iplementedMembers, eiiMenbers);
            if (exceptions.HasValue &&
                    (exceptions.Value & ExceptionLocations.AddedMembers) != 0)
                UpdateExceptions (info.Node, info.Member);

            if (since != null)
            {
                XmlNode docs = me.SelectSingleNode ("Docs");
                docs.AppendChild (CreateSinceNode (doc));
            }

            return me;
        }

        internal static string GetMemberName (MemberReference mi)
        {
            MethodDefinition mb = mi as MethodDefinition;
            if (mb == null)
            {
                PropertyDefinition pi = mi as PropertyDefinition;
                if (pi == null)
                    return mi.Name;
                return DocUtils.GetPropertyName (pi);
            }
            StringBuilder sb = new StringBuilder (mi.Name.Length);
            if (!DocUtils.IsExplicitlyImplemented (mb))
                sb.Append (mi.Name);
            else
            {
                TypeReference iface;
                MethodReference ifaceMethod;
                DocUtils.GetInfoForExplicitlyImplementedMethod (mb, out iface, out ifaceMethod);
                sb.Append (GetDocTypeFullName (iface));
                sb.Append ('.');
                sb.Append (ifaceMethod.Name);
            }

            AddGenericParameter(sb, mb);
            return sb.ToString ();
        }

        private static void AddGenericParameter(StringBuilder sb, MethodDefinition mb)
        {
            if (mb.IsGenericMethod())
            {
                IList<GenericParameter> typeParams = mb.GenericParameters;
                if (typeParams.Count > 0)
                {
                    sb.Append("<");
                    sb.Append(typeParams[0].Name);
                    for (int i = 1; i < typeParams.Count; ++i)
                        sb.Append(",").Append(typeParams[i].Name);
                    sb.Append(">");
                }
            }
        }

        private void AddEiiNameAsAttribute(MemberReference memberReference, XmlElement memberElement, string memberName)
        {
            if (DocUtils.IsExplicitlyImplemented(memberReference))
            {
                var eiiName = GetExplicitInterfaceMemberName(memberReference);
                if (!string.Equals(memberName, eiiName, StringComparison.InvariantCulture))
                {
                    memberElement.SetAttribute("ExplicitInterfaceMemberName", eiiName);
                }
            }
        }

        private static string GetExplicitInterfaceMemberName(MemberReference mi)
        {
            if (mi is MethodDefinition methodDefinition)
            {
                var stringBuilder = new StringBuilder(methodDefinition.Name ?? mi.Name);
                AddGenericParameter(stringBuilder, methodDefinition);
                return stringBuilder.ToString();                
            }

            var propertyDefinition = mi as PropertyDefinition;
            return propertyDefinition?.Name ?? mi.Name;
        }

        /// SIGNATURE GENERATION FUNCTIONS
        internal static bool IsPrivate (MemberReference mi)
        {
            return FormatterManager.MemberFormatters[0].GetDeclaration (mi) == null;
        }

        internal static string GetMemberType (MemberReference mi)
        {
            if (mi is MethodDefinition && ((MethodDefinition)mi).IsConstructor)
                return "Constructor";
            if (mi is MethodDefinition)
                return "Method";
            if (mi is PropertyDefinition)
                return "Property";
            if (mi is FieldDefinition)
                return "Field";
            if (mi is EventDefinition)
                return "Event";
            if (mi is AttachedEventReference)
                return "AttachedEvent";
            if (mi is AttachedPropertyReference)
                return "AttachedProperty";
            throw new ArgumentException ();
        }

        private static string GetDocTypeName (TypeReference type, bool useTypeProjection = true)
        {
            return docTypeFormatter.GetName (type, useTypeProjection: useTypeProjection);
        }

        internal static string GetDocTypeFullName (TypeReference type, bool useTypeProjection = true, bool isTypeofOperator = false)
        {
            return DocTypeFullMemberFormatter.Default.GetName (type, useTypeProjection: useTypeProjection, isTypeofOperator: isTypeofOperator);
        }

        internal static string GetXPathForMember (DocumentationMember member)
        {
            StringBuilder xpath = new StringBuilder ();
            xpath.Append ("//Members/Member[@MemberName=\"")
                .Append (member.MemberName)
                .Append ("\"]");
            if (member.Parameters != null && member.Parameters.Count > 0)
            {
                xpath.Append ("/Parameters[count(Parameter) = ")
                    .Append (member.Parameters.Count);
                for (int i = 0; i < member.Parameters.Count; ++i)
                {
                    xpath.Append (" and Parameter [").Append (i + 1).Append ("]/@Type=\"");
                    xpath.Append (member.Parameters[i]);
                    xpath.Append ("\"");
                }
                xpath.Append ("]/..");
            }
            return xpath.ToString ();
        }

        public static string GetXPathForMember (XPathNavigator member)
        {
            StringBuilder xpath = new StringBuilder ();
            xpath.Append ("//Type[@FullName=\"")
                .Append (member.SelectSingleNode ("../../@FullName").Value)
                .Append ("\"]/");
            xpath.Append ("Members/Member[@MemberName=\"")
                .Append (member.SelectSingleNode ("@MemberName").Value)
                .Append ("\"]");
            XPathNodeIterator parameters = member.Select ("Parameters/Parameter");
            if (parameters.Count > 0)
            {
                xpath.Append ("/Parameters[count(Parameter) = ")
                    .Append (parameters.Count);
                int i = 0;
                while (parameters.MoveNext ())
                {
                    ++i;
                    xpath.Append (" and Parameter [").Append (i).Append ("]/@Type=\"");
                    xpath.Append (parameters.Current.Value);
                    xpath.Append ("\"");
                }
                xpath.Append ("]/..");
            }
            return xpath.ToString ();
        }

        public static string GetXPathForMember (MemberReference member)
        {
            StringBuilder xpath = new StringBuilder ();
            xpath.Append ("//Type[@FullName=\"")
                .Append (member.DeclaringType.FullName)
                .Append ("\"]/");
            xpath.Append ("Members/Member[@MemberName=\"")
                .Append (GetMemberName (member))
                .Append ("\"]");

            IList<ParameterDefinition> parameters = null;
            if (member is MethodDefinition)
                parameters = ((MethodDefinition)member).Parameters;
            else if (member is PropertyDefinition)
            {
                parameters = ((PropertyDefinition)member).Parameters;
            }
            if (parameters != null && parameters.Count > 0)
            {
                xpath.Append ("/Parameters[count(Parameter) = ")
                    .Append (parameters.Count);
                for (int i = 0; i < parameters.Count; ++i)
                {
                    xpath.Append (" and Parameter [").Append (i + 1).Append ("]/@Type=\"");
                    xpath.Append (GetDocParameterType (parameters[i].ParameterType));
                    xpath.Append ("\"");
                }
                xpath.Append ("]/..");
            }
            return xpath.ToString ();
        }

        private static IEnumerable<XmlElement> QueryXmlElementsByXpath(XmlElement parentNode, string xPath)
        {
            return parentNode.SelectNodes(xPath).SafeCast<XmlElement>().ToArray();
        }

        public static IEnumerable<Eiimembers> GetTypeEiiMembers(TypeDefinition type)
        {
            if (!DocUtils.IsDelegate(type))
            {
                var eiiTypeMembers = type.GetMembers()
                        .Where(m =>
                        {
                            if (m is TypeDefinition) return false;
                            string cssig = FormatterManager.MemberFormatters[0].GetDeclaration(m);
                            if (cssig == null) return false;

                            string sig = FormatterManager.MemberFormatters[1].GetDeclaration(m);
                            if (sig == null) return false;

                            if (!IsMemberNotPrivateEII(m))
                                return false;
                            return true;
                        })
                        .Where(t => DocUtils.IsExplicitlyImplemented(t))
                        .Select(t => new Eiimembers { Fingerprint = DocUtils.GetFingerprint(t), Interfaces = DocUtils.GetOverrides(t).ToList<MemberReference>() });
                return eiiTypeMembers;
            }
            else
            { return new List<Eiimembers>(); }
        }
    }
}
