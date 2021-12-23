using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Mono.Documentation.Updater.Frameworks
{
    public class FrameworkEntry
    {
        SortedSet<FrameworkTypeEntry> types = new SortedSet<FrameworkTypeEntry> ();

        IList<FrameworkEntry> allcachedframeworks;
        IList<FrameworkEntry> allframeworks;
		ISet<AssemblySet> allAssemblies = new SortedSet<AssemblySet> ();

        public int Index = 0;
        int _fxCount;
        public int FrameworksCount {
            get => _fxCount < 1 ? allframeworks.Count : _fxCount;
        }

        public FrameworkEntry (IList<FrameworkEntry> frameworks, IList<FrameworkEntry> cachedfx) : this(frameworks, -1, cachedfx) {}

        public FrameworkEntry (IList<FrameworkEntry> frameworks, int fxCount, IList<FrameworkEntry> cachedFx)
        {
            allframeworks = frameworks;
            if (allframeworks == null)
            {
                allframeworks = new List<FrameworkEntry> (1);
                allframeworks.Add (this);
                Index = 0;
                allcachedframeworks = allframeworks;
            }
            else
            {
                Index = allframeworks.Count;

                allcachedframeworks = cachedFx;
            }
            _fxCount = fxCount;
        }

        public string Name { get; set; }
        public string Version { get; set; }
        public string Id { get; set; }

        /// <summary>Gets a value indicating whether this <see cref="T:Mono.Documentation.Updater.Frameworks.FrameworkEntry"/> is last framework being processed.</summary>
        public bool IsLastFramework {
            get => Index == FrameworksCount - 1;
        }

        /// <param name="assemblyName">should be the assembly name (without version and file extension)</param>
        public bool IsLastFrameworkForAssembly(string assemblyName)
        {
            if (this == Empty) return true;

            var retval = AllFrameworksWithAssembly(assemblyName);

            if (!retval.Any ()) return false;

            return retval.Last().Name == this.Name;
        }

        /// <param name="assemblyName">should be the assembly name (without version and file extension)</param>
        public bool IsFirstFrameworkForAssembly(string assemblyName)
        {
            if (this == Empty) return true;

            var retval = AllFrameworksWithAssembly(assemblyName);

            if (!retval.Any()) return false;

            return retval.First().Name == this.Name;
        }

        public bool IsLastFrameworkForType (FrameworkTypeEntry typeEntry)
        {
            if (this == Empty) return true;

            var fxlist = this.allcachedframeworks.Where (f => f.FindTypeEntry (typeEntry) != null).ToArray ();

            if (!fxlist.Any ()) return false;

            var lastListed = fxlist.Last ();
            return lastListed.Name == this.Name;
        }

        public bool IsLastFrameworkForMember (FrameworkTypeEntry typeEntry, string memberSig, string docidsig)
        {
            if (this == Empty) return true;

            var fxlist = this.allcachedframeworks.Where (f => {
                var fxType = f.FindTypeEntry (typeEntry);
                if (fxType == null) return false;

                return fxType.ContainsCSharpSig (memberSig) || fxType.ContainsDocId(docidsig);
            }).ToArray ();

            if (!fxlist.Any ()) 
                return false;

            var lastListed = fxlist.Last ();
            return lastListed.Name == this.Name;
        }

        public FrameworkEntry[] AllFrameworksWithAssembly(string assemblyName)
        {
            var fxlist = this.allcachedframeworks
                .Where(f => f.allAssemblies
                   .Any(ass => ass.Assemblies
                      .Any(a => a.Name.Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))));
            return fxlist.ToArray();
        }

        public string AllFrameworksStringWithAssembly(string assemblyName)
        {
            if (this == Empty) return this.Name;
            return string.Join (";", AllFrameworksWithAssembly(assemblyName).Select (f => f.Name).ToArray ());
        }

        public string AllFrameworksWithType(FrameworkTypeEntry typeEntry)
        {
            if (this == Empty) return this.Name;

            var fxlist = this.allcachedframeworks.Where (f => f.FindTypeEntry (typeEntry) != null);
            return string.Join (";", fxlist.Select (f => f.Name).ToArray ());
        }

        public string AllFrameworksWithMember (FrameworkTypeEntry typeEntry, string memberSig, string memberDocId)
        {
            return AllFrameworksThatMatch ((fx) =>
             {
                 var fxtype = fx.FindTypeEntry (typeEntry);
                 if (fxtype == null) return false;
                 return fxtype.ContainsCSharpSig (memberSig) || fxtype.ContainsDocId (memberDocId);
             });
        }

        public string AllFrameworksThatMatch(Func<FrameworkEntry, bool> clause)
        {
            if (this == Empty) return this.Name;

            var fxlist = this.allcachedframeworks.Where(clause).ToArray();
            return string.Join(";", fxlist.Select(f => f.Name).ToArray());
        }

        string _allFxString = "";
        public string AllFrameworksString {
            get 
            {
                Lazy<string> fxString = new Lazy<string>(() => string.Join (";", allcachedframeworks.Select (f => f.Name).ToArray ()));

                if (!this.IsLastFramework) return fxString.Value;
                if (string.IsNullOrWhiteSpace(_allFxString)) 
                {
                    _allFxString = fxString.Value;
                }
                return _allFxString;
            }
        }

        public readonly List<Tuple<string,string>> AssemblyNames = new List<Tuple<string, string>> ();

        public void AddProcessedAssembly (AssemblyDefinition assembly)
        {
            AssemblyNames.Add (new Tuple<string, string>(assembly.Name.Name, assembly.Name.Version.ToString()));
        }

        public IEnumerable<FrameworkEntry> PreviousFrameworks {
            get => allframeworks.Where (f => f.Index < this.Index);
        }

		public ISet<AssemblySet> AllProcessedAssemblies { get => allAssemblies; }

		public void AddAssemblySet (AssemblySet assemblySet)
		{
			allAssemblies.Add (assemblySet);
		}

		/// <summary>Gets a value indicating whether this <see cref="T:Mono.Documentation.Updater.Frameworks.FrameworkEntry"/> is first framework being processed.</summary>
		public bool IsFirstFramework { 
            get => this.Index == 0;
        }

        public bool IsFirstFrameworkForType (FrameworkTypeEntry typeEntry)
        {
            if (this == Empty) return true;

            var firstFx = this.allcachedframeworks.FirstOrDefault (f => f.FindTypeEntry (typeEntry) != null);

            return firstFx == null || firstFx.Name == this.Name;
        }

        public bool IsFirstFrameworkForMember (FrameworkTypeEntry typeEntry, string memberSig, string docidsig)
        {
            if (this == Empty) return true;

            var firstFx = this.allcachedframeworks.FirstOrDefault (f =>
            {
                var fxType = f.FindTypeEntry (typeEntry);
                if (fxType == null) return false;

                return fxType.ContainsCSharpSig (memberSig) || fxType.ContainsDocId (docidsig);
            });

            return firstFx == null || firstFx.Name == this.Name;
        }

        /// <summary>Only Use in Unit Tests</summary>
        public string Replace="";

        /// <summary>Only Use in Unit Tests</summary>
        public string With ="";

        public IEnumerable<DocumentationImporter> Importers { get; set; }

        public ISet<FrameworkTypeEntry> Types { get { return this.types; } }
        Dictionary<string, FrameworkTypeEntry> typeMap = new Dictionary<string, FrameworkTypeEntry> ();

        public FrameworkTypeEntry FindTypeEntry (FrameworkTypeEntry type) 
        {
            return FindTypeEntry (Str(type.Name));    
        }

        /// <param name="name">The value from <see cref="FrameworkTypeEntry.Name"/>.</param>
        public FrameworkTypeEntry FindTypeEntry (string name) {
            FrameworkTypeEntry entry;
            typeMap.TryGetValue (Str(name), out entry);
            return entry;
        }

		public IEnumerable<FrameworkEntry> Frameworks { get { return this.allframeworks; } }

		public static readonly FrameworkEntry Empty = new EmptyFrameworkEntry () { Name = "Empty" };

		public virtual FrameworkTypeEntry ProcessType (TypeDefinition type, AssemblyDefinition source)
		{
            FrameworkTypeEntry entry;

            if (!typeMap.TryGetValue (Str(type.FullName), out entry))
            {
				var docid = DocCommentId.GetDocCommentId (type);
                string nstouse = GetNamespace (type);
                entry = new FrameworkTypeEntry (this) { Id = Str (docid), Name = Str (type.FullName), Namespace = nstouse };
                types.Add (entry);

                typeMap.Add (Str (entry.Name), entry);
            }
            entry.NoteAssembly(type.Module.Assembly, source);
            entry.NoteAssembly(source, source);
            entry.TimesProcessed++;
            return entry;
        }

        private string GetNamespace (TypeDefinition type)
        {
            var nstouse = Str (type.Namespace);
            if (string.IsNullOrWhiteSpace (nstouse) && type.DeclaringType != null)
            {
                return GetNamespace(type.DeclaringType);
            }

            return nstouse;
        }

        string Str(string value) {
            if (!string.IsNullOrWhiteSpace (Replace))
                return value.Replace (Replace, With);
            return value;
        }

        public override string ToString () => Str(this.Name);

		class EmptyFrameworkEntry : FrameworkEntry
		{
			public EmptyFrameworkEntry () : base (null, 1, null) { }
			public override FrameworkTypeEntry ProcessType (TypeDefinition type, AssemblyDefinition source) { return FrameworkTypeEntry.Empty; }


		}
	}
}
