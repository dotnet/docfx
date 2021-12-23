using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace Mono.Documentation.Updater.Frameworks
{
    /// <summary>
    /// Represents a set of assemblies that we want to document
    /// </summary>
    public class AssemblySet : IDisposable
    {
        BaseAssemblyResolver resolver = new Frameworks.MDocResolver ();
        CachedResolver cachedResolver;
        IMetadataResolver metadataResolver;

        HashSet<string> assemblyPaths = new HashSet<string> ();
        Dictionary<string, bool> assemblyPathsMap = new Dictionary<string, bool> ();
        HashSet<string> assemblySearchPaths = new HashSet<string> ();
        HashSet<string> forwardedTypes = new HashSet<string> ();
        IEnumerable<string> importPaths;
        public IEnumerable<DocumentationImporter> Importers { get; private set; }

		FrameworkEntry fx;
        public FrameworkEntry Framework 
		{
			get => fx;
            set 
			{
				fx = value;
                fx.AddAssemblySet (this);
			}
		}

        /// <summary>This is meant only for unit test access</summary>
        public IDictionary<string, bool> AssemblyMapsPath
        {
            get => assemblyPathsMap;
        }

        private IDictionary<string, HashSet<MDocResolver.TypeForwardEventArgs>> forwardedTypesTo = new Dictionary<string, HashSet<MDocResolver.TypeForwardEventArgs>> ();


        public AssemblySet (IEnumerable<string> paths) : this ("Default", paths, new string[0]) { }

        public AssemblySet (string name, IEnumerable<string> paths, IEnumerable<string> resolverSearchPaths, IEnumerable<string> imports = null, string version = null, string id = null)
        {
            cachedResolver = cachedResolver ?? new CachedResolver (resolver);
            metadataResolver = metadataResolver ?? new Frameworks.MDocMetadataResolver (cachedResolver);
            ((MDocResolver)resolver).TypeExported += (sender, e) =>
            {
                TrackTypeExported (e);
            };

            Name = name;
            Version = version;
            Id = id;

            foreach (var path in paths)
            {
                assemblyPaths.Add (path);
                string pathName = Path.GetFileName (path);
                if (!assemblyPathsMap.ContainsKey (pathName))
                    assemblyPathsMap.Add (pathName, true);
            }

            // add default search paths
            var assemblyDirectories = paths
                .Where (p => p.Contains (Path.DirectorySeparatorChar))
                .Select (p => Path.GetDirectoryName (p));

            foreach (var searchPath in assemblyDirectories.Union(resolverSearchPaths))
                assemblySearchPaths.Add (searchPath);

            char oppositeSeparator = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
            Func<string, string> sanitize = p =>
                p.Replace (oppositeSeparator, Path.DirectorySeparatorChar);

            foreach (var searchPath in assemblySearchPaths.Select (sanitize))
                resolver.AddSearchDirectory (searchPath);

            this.importPaths = imports;
            if (this.importPaths != null)
            {
                this.Importers = this.importPaths.Select (p => MDocUpdater.Instance.GetImporter (p, supportsEcmaDoc: false)).ToArray ();
            }
            else
                this.Importers = new DocumentationImporter[0];
        }

        private void TrackTypeExported (MDocResolver.TypeForwardEventArgs e)
        {
            if (e.ForType == null) return;

            // keep track of types that have been exported for this assemblyset
            if (!forwardedTypesTo.ContainsKey (e.ForType))
            {
                forwardedTypesTo.Add (e.ForType, new HashSet<MDocResolver.TypeForwardEventArgs> ());
            }

            forwardedTypesTo[e.ForType].Add (e);
        }

        public string Name { get; private set; }
        public string Version { get; private set; }
        public string Id { get; private set; }

        IEnumerable<AssemblyDefinition> assemblies;
        public IEnumerable<AssemblyDefinition> Assemblies
        {
            get
            {
                if (this.assemblies == null)
                    this.assemblies = this.LoadAllAssemblies ().Where (a => a != null).ToArray ();

                return this.assemblies;
            }
        }
        public IEnumerable<string> AssemblyPaths { get { return this.assemblyPaths; } }

        /// <summary>Adds all subdirectories to the search directories for the resolver to look in.</summary>
        public void RecurseSearchDirectories ()
        {
            var directories = resolver
                .GetSearchDirectories ()
                .Select (d => new DirectoryInfo (d))
                .Where (d => d.Exists)
                .Select (d => d.FullName)
                .Distinct ()
                .ToDictionary (d => d, d => d);

            var subdirs = directories.Keys
                .SelectMany (d => Directory.GetDirectories (d, ".", SearchOption.AllDirectories))
                .Where (d => !directories.ContainsKey (d));

            foreach (var dir in subdirs)
                resolver.AddSearchDirectory (dir);
        }

        /// <returns><c>true</c>, if in set was contained in the set of assemblies, <c>false</c> otherwise.</returns>
        /// <param name="name">An assembly file name</param>
        public bool Contains (string name)
        {
            return assemblyPathsMap.ContainsKey (name);//assemblyPaths.Any (p => Path.GetFileName (p) == name);
        }

        /// <summary>Tells whether an already enumerated AssemblyDefinition, contains the type.</summary>
        /// <param name="name">Type name</param>
        public bool ContainsForwardedType (string name)
        {
            return forwardedTypes.Contains (name);
        }

        /// <summary>
        /// Forwardeds the assemblies.
        /// </summary>
        /// <returns>The assemblies.</returns>
        /// <param name="type">Type.</param>
        public IEnumerable<AssemblyNameReference> FullAssemblyChain(TypeDefinition type)
        {
            if (forwardedTypesTo.ContainsKey (type.FullName))
            {
                var list = forwardedTypesTo[type.FullName];
                var assemblies = (new[] { type.Module.Assembly.Name })
                    .Union (list.Select (f => f.To))
                    .Union (list.Select (f => f.From))
                    .Distinct (anc);
                return assemblies;
            }
            else
                return new[] { type.Module.Assembly.Name };
        }

        AssemblyNameComparer anc = new AssemblyNameComparer ();
        class AssemblyNameComparer : IEqualityComparer<AssemblyNameReference>
        {
            public bool Equals (AssemblyNameReference x, AssemblyNameReference y)
            {
                return x.FullName.Equals (y.FullName, StringComparison.Ordinal);
            }

            public int GetHashCode (AssemblyNameReference obj)
            {
                return obj.FullName.GetHashCode ();
            }
        }

        public void Dispose () 
        {
            this.assemblies = null;
            cachedResolver?.Dispose();
            cachedResolver = null;
        }

		public override string ToString ()
		{
			return string.Format ("[AssemblySet: Name={0}, Assemblies={1}]", Name, assemblyPaths.Count);
		}

		IEnumerable<AssemblyDefinition> LoadAllAssemblies ()
		{
			foreach (var path in this.assemblyPaths) {
                var assembly = MDocUpdater.Instance.LoadAssembly (path, metadataResolver, cachedResolver);
				if (assembly != null) {
                    foreach (var type in assembly.MainModule.ExportedTypes.Where (t => t.IsForwarder).Cast<ExportedType>())
                    {
                        forwardedTypes.Add (type.FullName);
                        TrackTypeExported (new MDocResolver.TypeForwardEventArgs (assembly.Name, (AssemblyNameReference)type.Scope, type?.FullName));
                    }
				}
				yield return assembly;
			}
		}

        /// <summary>
        /// Return the type forwardings of given type.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>Type forwardings.</returns>
        public HashSet<MDocResolver.TypeForwardEventArgs> ForwardingChains(TypeDefinition type)
        {
            if (forwardedTypesTo.ContainsKey(type.FullName))
            {
                return forwardedTypesTo[type.FullName];
            }
            else
                return new HashSet<MDocResolver.TypeForwardEventArgs>();
        }

        public bool IsTypeForwardingTo(FrameworkTypeEntry typeEntry)
        {
            return forwardedTypesTo.ContainsKey(typeEntry.Name);
        }
    }
}
