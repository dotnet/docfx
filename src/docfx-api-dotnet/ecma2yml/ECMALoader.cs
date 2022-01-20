using ECMA2Yaml.IO;
using ECMA2Yaml.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Path = System.IO.Path;

namespace ECMA2Yaml
{
    public partial class ECMALoader
    {
        private List<string> _errorFiles = new List<string>();
        private ECMADocsTransform _docsTransform = new ECMADocsTransform();
        private FileAccessor _fileAccessor = null;

        public ConcurrentBag<string> FallbackFiles { get; private set; }

        public ECMALoader(FileAccessor fileAccessor)
        {
            _fileAccessor = fileAccessor;
            FallbackFiles = new ConcurrentBag<string>();
        }

        public ECMAStore LoadFolder(string sourcePath, bool isUWPMode = false)
        {
            //if (!System.IO.Directory.Exists(sourcePath))
            //{
            //    OPSLogger.LogUserWarning(string.Format("Source folder does not exist: {0}", sourcePath));
            //    return null;
            //}

            var filterStore = LoadFilters(sourcePath);
            var typeMappingStore = LoadTypeMap(sourcePath);
            var pkgInfoMapping = LoadPackageInformationMapping(sourcePath);

            ConcurrentBag<Namespace> namespaces = new ConcurrentBag<Namespace>();
            ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };
            //foreach(var nsFile in ListFiles(sourcePath, "ns-*.xml"))
            Parallel.ForEach(ListFiles(sourcePath, "ns-*.xml"), opt, nsFile =>
            {
                var ns = LoadNamespace(sourcePath, nsFile);
                if (ns == null)
                {
                    OPSLogger.LogUserError(LogCode.ECMA2Yaml_Namespace_LoadFailed, nsFile.AbsolutePath);
                }
                else if (ns.Types == null)
                {
                    OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_Namespace_NoTypes, nsFile.AbsolutePath);
                }
                else
                {
                    namespaces.Add(ns);
                    if (nsFile.IsVirtual)
                    {
                        FallbackFiles.Add(nsFile.AbsolutePath);
                    }
                }
            });

            if (namespaces.Count == 0)
            {
                return null;
            }

            if (_errorFiles.Count > 0)
            {
                OPSLogger.LogUserError(LogCode.ECMA2Yaml_File_LoadFailed, null, _errorFiles.Count);
                return null;
            }

            var frameworks = LoadFrameworks(sourcePath);
            if (frameworks == null || frameworks.DocIdToFrameworkDict.Count == 0)
            {
                OPSLogger.LogUserError(LogCode.ECMA2Yaml_Framework_NotFound, null, "any API, please check your FrameworkIndex folder");
                return null;
            }

            var filteredNS = Filter(namespaces, filterStore, frameworks);
            var store = new ECMAStore(filteredNS.OrderBy(ns => ns.Name).ToArray(), frameworks)
            {
                FilterStore = filterStore,
                TypeMappingStore = isUWPMode ? typeMappingStore : null,
                PkgInfoMapping = pkgInfoMapping,
                UWPMode = isUWPMode
            };
            return store;
        }

        private List<Namespace> Filter(IEnumerable<Namespace> namespaces, FilterStore filterStore, FrameworkIndex frameworks)
        {
            var filteredNS = namespaces.ToList();
            foreach (var ns in filteredNS)
            {
                if (ns.Types?.Count > 0)
                {
                    ns.Types = ns.Types.Where(t =>
                    {
                        bool expose = true;
                        if (filterStore?.TypeFilters?.Count > 0)
                        {
                            var applicableFilters = filterStore.TypeFilters.Where(tf => tf.Filter(t).HasValue).ToList();
                            if (applicableFilters.Count == 1)
                            {
                                expose = applicableFilters.First().Filter(t).Value;
                            }
                            else if (applicableFilters.Count > 1)
                            {
                                var filtersPerNS = applicableFilters.GroupBy(tf => tf.Namespace).Select(tfg => tfg.FirstOrDefault(tf => tf.Name != "*") ?? tfg.First()).ToList();
                                foreach (var filter in filtersPerNS)
                                {
                                    expose = expose && filter.Filter(t).Value;
                                }
                            }
                        }

                        return t.DocId == null || (expose && frameworks.DocIdToFrameworkDict.ContainsKey(t.DocId));
                        
                    }).ToList();

                }
            }
            filteredNS = filteredNS.Where(ns => ns.Types?.Count > 0 && frameworks.DocIdToFrameworkDict.ContainsKey(ns.CommentId)).ToList();
            foreach (var ns in filteredNS)
            {
                foreach (var t in ns.Types)
                {
                    if (t.Members?.Count > 0)
                    {
                        t.Members = t.Members.Where(m =>
                        {
                            bool expose = true;
                            if (filterStore?.MemberFilters?.Count > 0)
                            {
                                foreach (var filter in filterStore.MemberFilters.Where(mf => mf.Filter(m).HasValue))
                                {
                                    expose = expose && filter.Filter(m).Value;
                                }
                            }

                            return m.DocId == null || (expose && frameworks.DocIdToFrameworkDict.ContainsKey(m.DocId));

                        }).ToList();
                    }
                }
            }
            //workaournd for https://github.com/mono/api-doc-tools/issues/89, bug #1022788
            foreach (var ns in filteredNS)
            {
                foreach (var t in ns.Types)
                {
                    if (t.Signatures.IsPublishSealedClass)
                    {
                        t.Members = t.Members.Where(m => !m.Signatures.IsProtected).ToList();
                    }
                }
            }
            return filteredNS;
        }

        private Namespace LoadNamespace(string basePath, FileItem nsFile)
        {
            XDocument nsDoc = XDocument.Load(nsFile.AbsolutePath);
            Namespace ns = new Namespace();
            ns.Id = ns.Name = nsDoc.Root.Attribute("Name").Value;
            ns.Types = LoadTypes(basePath, ns);
            ns.Docs = LoadDocs(nsDoc.Root.Element("Docs"), nsFile.AbsolutePath);
            ns.SourceFileLocalPath = nsFile.AbsolutePath;
            ns.ItemType = ItemType.Namespace;
            // Metadata
            LoadMetadata(ns, nsDoc.Root);
            return ns;
        }

        private List<Models.Type> LoadTypes(string basePath, Namespace ns)
        {
            string nsFolder = Path.Combine(basePath, ns.Name);
            List<Models.Type> types = new List<Models.Type>();
            foreach (var typeFile in ListFiles(nsFolder, "*.xml"))
            {
                try
                {
                    var t = LoadType(typeFile);
                    if (t != null)
                    {
                        t.Parent = ns;
                        types.Add(t);
                        if (typeFile.IsVirtual)
                        {
                            FallbackFiles.Add(typeFile.AbsolutePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OPSLogger.LogUserError(LogCode.ECMA2Yaml_InternalError, typeFile.AbsolutePath, ex.ToString());
                    _errorFiles.Add(typeFile.AbsolutePath);
                }
            }
            return types;
        }

        private Models.Type LoadType(FileItem typeFile)
        {
            string xmlContent = _fileAccessor.ReadAllText(typeFile.RelativePath);
            xmlContent = xmlContent.Replace("TextAntiAliasingQuality&nbsp;property.</summary>", "TextAntiAliasingQuality property.</summary>");
            xmlContent = xmlContent.Replace("DefaultValue('&#x0;')</AttributeName>", "DefaultValue('\\0')</AttributeName>");
            xmlContent = xmlContent.Replace("\0", "\\0");

            XDocument tDoc = XDocument.Parse(xmlContent, LoadOptions.PreserveWhitespace);
            XElement tRoot = tDoc.Root;
            if (tRoot.Name.LocalName != "Type")
            {
                return null;
            }
            Models.Type t = new Models.Type();
            t.Name = tRoot.Attribute("Name").Value.Replace('+', '.');
            t.FullName = tRoot.Attribute("FullName").Value.Replace('+', '.');
            t.SourceFileLocalPath = typeFile.AbsolutePath;

            //TypeSignature
            t.Signatures = new VersionedSignatures(tRoot.Elements("TypeSignature"));
            t.Modifiers = t.Signatures.CombinedModifiers;
            t.DocId = t.Signatures.DocId;

            //AssemblyInfo
            t.AssemblyInfo = tRoot.Elements("AssemblyInfo")?.SelectMany(a => ParseAssemblyInfo(a)).ToList();

            //TypeParameters
            var tpElement = tRoot.Element("TypeParameters");
            if (tpElement != null)
            {
                t.TypeParameters = ParameterBase.LoadVersionedParameters<TypeParameter>(tpElement.Elements("TypeParameter"));
            }

            //Parameters
            var pElement = tRoot.Element("Parameters");
            if (pElement != null)
            {
                t.Parameters = ParameterBase.LoadVersionedParameters<Parameter>(pElement.Elements("Parameter"));
            }

            var rvalElement = tRoot.Element("ReturnValue");
            if (rvalElement != null)
            {
                t.ReturnValueType = MonikerizeReturnValue(rvalElement);
            }

            //BaseTypeName
            t.BaseTypes = LoadBaseType(tRoot.Element("Base"));

            //Interfaces
            var interfacesElement = tRoot.Element("Interfaces");
            if (interfacesElement != null)
            {
                t.Interfaces = interfacesElement.Elements("Interface")
                    .Select(i => new VersionedString(LoadFrameworkAlternate(i), i?.Element("InterfaceName")?.Value))
                    .ToList();
            }

            //Attributes
            var attrs = tRoot.Element("Attributes");
            if (attrs != null)
            {
                t.Attributes = attrs.Elements("Attribute").Select(a => LoadAttribute(a)).ToList();
            }

            //TypeForwardingChain
            var forwardingChain = tRoot.Element("TypeForwardingChain");
            if (forwardingChain != null)
            {
                var fwds = forwardingChain.Elements("TypeForwarding").Select(fwd => LoadTypeForwarding(fwd)).ToList();
                t.TypeForwardingChain = new TypeForwardingChain(fwds);
            }

            //Members
            var membersElement = tRoot.Element("Members");
            if (membersElement != null)
            {
                t.Members = membersElement.Elements("Member")?.Select(m => LoadMember(t, m)).ToList();
                t.Members.Sort((m1, m2) =>
                {
                    if (m1.IsEII == m2.IsEII)
                    {
                        return string.Compare(m1.Name, m2.Name);
                    }
                    else
                    {
                        return m1.IsEII ? 1 : -1; //non-EII first, EII later
                    }
                });
                if (t.Members != null)
                {
                    foreach (var m in t.Members)
                    {
                        m.SourceFileLocalPath = typeFile.AbsolutePath;
                    }
                }
                t.Overloads = membersElement.Elements("MemberGroup")?.Select(m => LoadMemberGroup(t, m)).ToList();
                if (t.Overloads != null)
                {
                    var distinctList = new List<Member>();
                    foreach (var og in t.Overloads.GroupBy(o => o.Name))
                    {
                        if (og.Count() > 1)
                        {
                            OPSLogger.LogUserWarning(LogCode.ECMA2Yaml_MemberGroup_Duplicated, typeFile.AbsolutePath, og.Key);
                        }
                        og.First().SourceFileLocalPath = typeFile.AbsolutePath;
                        distinctList.Add(og.First());
                    }
                    t.Overloads = distinctList;
                }
            }

            //Docs
            t.Docs = LoadDocs(tRoot.Element("Docs"), typeFile.AbsolutePath);

            //MemberType
            t.ItemType = InferTypeOfType(t);

            if (t.ItemType == ItemType.Enum && t.Members?.Count > 0)
            {
                foreach (var m in t.Members)
                {
                    if (!string.IsNullOrEmpty(m.Docs.Remarks))
                    {
                        OPSLogger.LogUserSuggestion(LogCode.ECMA2Yaml_Enum_NoRemarks, typeFile.AbsolutePath);
                    }
                }
            }

            // Metadata
            LoadMetadata(t, tRoot);

            return t;
        }

        private static ItemType InferTypeOfType(Models.Type t)
        {
            var signature = t.Signatures.Dict[ECMADevLangs.CSharp].FirstOrDefault()?.Value;
            if (t.BaseTypes == null && signature.Contains(" interface "))
            {
                return ItemType.Interface;
            }
            else if ((t.BaseTypes == null || t.BaseTypes.Any(bt => bt.Name == "System.Enum"))
                && signature.Contains(" enum "))
            {
                return ItemType.Enum;
            }
            else if ((t.BaseTypes == null || t.BaseTypes.Any(bt => bt.Name == "System.Delegate"))
                && signature.Contains(" delegate "))
            {
                return ItemType.Delegate;
            }
            else if ((t.BaseTypes == null || t.BaseTypes.Any(bt => bt.Name == "System.ValueType"))
                && signature.Contains(" struct "))
            {
                return ItemType.Struct;
            }
            else if (signature.Contains(" class "))
            {
                return ItemType.Class;
            }
            else
            {
                throw new Exception("Unable to identify the type of Type " + t.Name);
            }
        }

        private List<BaseType> LoadBaseType(XElement bElement)
        {
            if (bElement == null)
            {
                return null;
            }
            List<BaseType> baseTypes = new List<BaseType>();
            var nameElements = bElement.Elements("BaseTypeName");
            if (nameElements != null)
            {
                foreach (var nameElement in nameElements)
                {
                    BaseType bt = new BaseType();
                    bt.Name = nameElement.Value;
                    bt.Monikers = LoadFrameworkAlternate(nameElement);
                    if (bt.Name.Contains("<"))
                    {
                        var btaElements = bElement.Element("BaseTypeArguments")?.Elements("BaseTypeArgument");
                        if (btaElements != null)
                        {
                            bt.TypeArguments = btaElements.Select(e => new BaseTypeArgument()
                            {
                                TypeParamName = e.Attribute("TypeParamName").Value,
                                Value = e.Value
                            }).ToList();
                        }
                    }
                    baseTypes.Add(bt);
                }
            }

            return baseTypes;
        }

        private Member LoadMember(Models.Type t, XElement mElement)
        {
            Member m = new Member();
            m.Parent = t;
            m.Name = mElement.Attribute("MemberName").Value;
            m.ItemType = (ItemType)Enum.Parse(typeof(ItemType), mElement.Element("MemberType").Value);
            if (m.Name.StartsWith("op_") && m.ItemType == ItemType.Method)
            {
                m.ItemType = ItemType.Operator;
            }
            var memberValue = mElement.Element("MemberValue")?.Value;
            if (!string.IsNullOrEmpty(memberValue))
            {
                m.Metadata[OPSMetadata.LiteralValue] = memberValue;
            }

            m.Signatures = new VersionedSignatures(mElement.Elements("MemberSignature"), m.ItemType);
            m.Modifiers = m.Signatures.CombinedModifiers;
            m.DocId = m.Signatures.DocId;
            m.AssemblyInfo = mElement.Elements("AssemblyInfo")?.SelectMany(a => ParseAssemblyInfo(a)).ToList();

            //TypeParameters
            var tpElement = mElement.Element("TypeParameters");
            if (tpElement != null)
            {
                m.TypeParameters = ParameterBase.LoadVersionedParameters<TypeParameter>(tpElement.Elements("TypeParameter"));
            }

            //Parameters
            var pElement = mElement.Element("Parameters");
            if (pElement != null)
            {
                m.Parameters = ParameterBase.LoadVersionedParameters<Parameter>(pElement.Elements("Parameter"));
            }

            //Attributes
            var attrs = mElement.Element("Attributes");
            if (attrs != null)
            {
                m.Attributes = attrs.Elements("Attribute").Select(a => LoadAttribute(a)).ToList();
            }

            // Load monikerized return type data
            var returnValueElement = mElement.Element("ReturnValue");

            m.ReturnValueType = MonikerizeReturnValue(returnValueElement);

            var implements = mElement.Element("Implements");
            if (implements != null)
            {
                m.Implements = implements.Elements("InterfaceMember")?.Select(LoadMonikerizedValue).ToList();
            }

            //Docs
            m.Docs = LoadDocs(mElement.Element("Docs"), t.SourceFileLocalPath);

            LoadMetadata(m, mElement);

            return m;
        }

        public static ReturnValue MonikerizeReturnValue(XElement returnValueElement)
        {
            var returnTypes = returnValueElement?.Elements("ReturnType").Select(r => new { ReturnType = r.Value, Monikers = LoadFrameworkAlternate(r) });
            if (returnTypes == null || !returnTypes.Any()) return null;

            Func<VersionedReturnType, VersionedReturnType> toParam = s =>
            {
                var returnType = s;
                if (returnType.Value.EndsWith("&"))
                {
                    returnType.Value = returnType.Value.TrimEnd('&');
                    returnType.RefType = "ref";
                }
                returnType.Value = returnType.Value.Replace('+', '.');
                return returnType;
            };
            var returns = returnTypes.Select(r => new VersionedReturnType
            {
                Value = r.ReturnType,
                Monikers = r.Monikers
            }).Select(toParam);

            return new ReturnValue() { VersionedTypes = returns.ToArray() };
        }

        private Member LoadMemberGroup(Models.Type t, XElement mElement)
        {
            Member m = new Member
            {
                Parent = t,
                Name = mElement.Attribute("MemberName").Value,
                AssemblyInfo = mElement.Elements("AssemblyInfo")?.SelectMany(a => ParseAssemblyInfo(a)).ToList(),
                Docs = LoadDocs(mElement.Element("Docs"), t.SourceFileLocalPath)
            };
            return m;
        }

        private VersionedValue<TypeForwarding> LoadTypeForwarding(XElement fwdElement)
        {
            var fwd = new TypeForwarding()
            {
                From = new AssemblyInfo()
                {
                    Name = fwdElement.Attribute("From")?.Value,
                    Version = fwdElement.Attribute("FromVersion")?.Value
                },
                To = new AssemblyInfo()
                {
                    Name = fwdElement.Attribute("To")?.Value,
                    Version = fwdElement.Attribute("ToVersion")?.Value
                }
            };
            var monikers = LoadFrameworkAlternate(fwdElement);
            return new VersionedValue<TypeForwarding>(monikers, fwd);
        }
    }
}
