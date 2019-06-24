// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal class XrefMap
    {
        // TODO: key could be uid+moniker+locale
        private readonly IReadOnlyDictionary<string, List<Lazy<IXrefSpec>>> _map;
        private readonly IReadOnlyDictionary<string, List<Lazy<IXrefSpec>>> _internalXrefMap;
        private readonly Context _context;

        private static ThreadLocal<Stack<(string uid, string propertyName, Document parent)>> t_recursionDetector = new ThreadLocal<Stack<(string, string, Document)>>(() => new Stack<(string, string, Document)>());

        public XrefMap(Context context, IReadOnlyDictionary<string, List<Lazy<IXrefSpec>>> map, IReadOnlyDictionary<string, List<Lazy<IXrefSpec>>> internalXrefMap)
        {
            _internalXrefMap = internalXrefMap;
            _context = context;
            _map = map;
        }

        public (Error error, string href, string display, IXrefSpec xrefSpec) Resolve(string uid, SourceInfo<string> href, string displayPropertyName, Document relativeTo, string moniker = null)
        {
            if (t_recursionDetector.Value.Contains((uid, displayPropertyName, relativeTo)))
            {
                var referenceMap = t_recursionDetector.Value.Select(x => x.parent).ToList();
                referenceMap.Reverse();
                referenceMap.Add(relativeTo);
                throw Errors.CircularReference(referenceMap).ToException();
            }

            try
            {
                t_recursionDetector.Value.Push((uid, displayPropertyName, relativeTo));
                return ResolveCore(uid, href, displayPropertyName, moniker);
            }
            finally
            {
                Debug.Assert(t_recursionDetector.Value.Count > 0);
                t_recursionDetector.Value.Pop();
            }
        }

        public void OutputXrefMap(Context context)
        {
            var models = new XrefMapModel();
            models.References.AddRange(ExpandInternalXrefSpecs());
            context.Output.WriteJson(models, "xrefmap.json");
        }

        private (Error error, string href, string display, IXrefSpec xrefSpec) ResolveCore(
            string uid, SourceInfo<string> href, string displayPropertyName, string moniker = null)
        {
            string resolvedHref;
            string displayPropertyValue;
            string name;
            if (TryResolve(uid, moniker, out var spec))
            {
                var (_, query, fragment) = UrlUtility.SplitUrl(spec.Href);
                resolvedHref = UrlUtility.MergeUrl(spec.Href, query, fragment.Length == 0 ? "" : fragment.Substring(1));

                name = spec.GetXrefPropertyValue("name");
                displayPropertyValue = spec.GetXrefPropertyValue(displayPropertyName);
            }
            else
            {
                return (Errors.XrefNotFound(href), null, null, null);
            }

            // fallback order:
            // xrefSpec.displayPropertyName -> xrefSpec.name -> uid
            string display = !string.IsNullOrEmpty(displayPropertyValue) ? displayPropertyValue : (!string.IsNullOrEmpty(name) ? name : uid);
            return (null, resolvedHref, display, spec);
        }

        private bool TryResolve(string uid, string moniker, out IXrefSpec spec)
        {
            spec = null;
            if (_map.TryGetValue(uid, out var specs))
            {
                spec = GetXrefSpec(uid, moniker, specs.Select(x => x.Value).ToList());

                if (spec is null)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        private IXrefSpec GetXrefSpec(string uid, string moniker, List<IXrefSpec> specs)
        {
            if (!TryGetValidXrefSpecs(uid, specs, out var validSpecs))
                return default;

            if (!string.IsNullOrEmpty(moniker))
            {
                foreach (var spec in validSpecs)
                {
                    if (spec.Monikers.Contains(moniker))
                    {
                        return spec;
                    }
                }

                return GetLatestInternalXrefMap(validSpecs);
            }

            // For uid with and without moniker range, take the one without moniker range
            var uidWithoutMoniker = validSpecs.SingleOrDefault(item => item.Monikers.Count == 0);
            if (uidWithoutMoniker != null)
            {
                return uidWithoutMoniker;
            }

            // For uid with moniker range, take the latest moniker if no moniker defined while resolving
            if (specs.Count > 1)
            {
                return GetLatestInternalXrefMap(validSpecs);
            }
            else
            {
                return validSpecs.Single();
            }
        }

        private IEnumerable<ExternalXrefSpec> ExpandInternalXrefSpecs()
        {
            var loadedInternalSpecs = new List<ExternalXrefSpec>();
            foreach (var (uid, specsWithSameUid) in _internalXrefMap)
            {
                if (TryGetValidXrefSpecs(uid, specsWithSameUid.Select(x => x.Value).ToList(), out var validInternalSpecs))
                {
                    var internalSpec = GetLatestInternalXrefMap(validInternalSpecs);
                    var spec = (internalSpec as InternalXrefSpec).ToExternalXrefSpec(_context, internalSpec.DeclairingFile);

                    // DHS appends branch infomation from cookie cache to URL, which is wrong for UID resolved URL
                    // output xref map with URL appending "?branch=master" for master branch
                    spec.Href += internalSpec.DeclairingFile.Docset.Repository?.Branch == "master" ? "?branch=master" : "";
                    loadedInternalSpecs.Add(spec);
                }
            }
            return loadedInternalSpecs;
        }

        private IXrefSpec GetLatestInternalXrefMap(List<IXrefSpec> specs)
            => specs.SingleOrDefault(x => x.Monikers?.Any() != true)
               ?? specs.Where(x => x.Monikers?.Any() != false).OrderByDescending(item => item.Monikers.FirstOrDefault(), _context.MonikerProvider.Comparer).FirstOrDefault();

        private bool TryGetValidXrefSpecs(string uid, List<IXrefSpec> specsWithSameUid, out List<IXrefSpec> validSpecs)
        {
            validSpecs = new List<IXrefSpec>();

            // no conflicts
            if (specsWithSameUid.Count() == 1)
            {
                validSpecs.AddRange(specsWithSameUid);
                return true;
            }

            // multiple uid conflicts without moniker range definition, drop the uid and log an error
            var conflictsWithoutMoniker = specsWithSameUid.Where(item => item.Monikers.Count == 0);
            if (conflictsWithoutMoniker.Count() > 1)
            {
                var orderedConflict = conflictsWithoutMoniker.OrderBy(item => item.Href);
                _context.ErrorLog.Write(Errors.UidConflict(uid, orderedConflict.Select(x => x.DeclairingFile.FilePath)));
                return false;
            }
            else if (conflictsWithoutMoniker.Count() == 1)
            {
                validSpecs.Add(conflictsWithoutMoniker.Single());
            }

            // uid conflicts with overlapping monikers, drop the uid and log an error
            var conflictsWithMoniker = specsWithSameUid.Where(x => x.Monikers.Count > 0);
            if (CheckOverlappingMonikers(specsWithSameUid, out var overlappingMonikers))
            {
                _context.ErrorLog.Write(Errors.MonikerOverlapping(overlappingMonikers));
                return false;
            }

            // define same uid with non-overlapping monikers, add them all
            else
            {
                validSpecs.AddRange(conflictsWithMoniker);
                return true;
            }
        }

        private bool CheckOverlappingMonikers(IEnumerable<IXrefSpec> specsWithSameUid, out HashSet<string> overlappingMonikers)
        {
            bool isOverlapping = false;
            overlappingMonikers = new HashSet<string>();
            var monikerHashSet = new HashSet<string>();
            foreach (var spec in specsWithSameUid)
            {
                foreach (var moniker in spec.Monikers)
                {
                    if (!monikerHashSet.Add(moniker))
                    {
                        overlappingMonikers.Add(moniker);
                        isOverlapping = true;
                    }
                }
            }
            return isOverlapping;
        }
    }
}
