namespace Microsoft.DocAsCode.ExternalPackageGenerators.Msdn
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Utility;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reactive.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    internal class Program
    {
        private const string MsdnUrlTemplate = "https://msdn.microsoft.com/en-us/library/{0}(v={1}).aspx";
        private const string MtpsApiUrlTemplate = "http://services.mtps.microsoft.com/ServiceAPI/content/{0}/en-us;{1}/common/mtps.links";
        private const int HttpMaxConcurrency = 32;

        private readonly Regex NormalUid = new Regex(@"^[a-zA-Z0-9\.]+$", RegexOptions.Compiled);
        private readonly HttpClient _client = new HttpClient();
        private readonly Cache<string> _shortIdCache;
        private readonly Cache<Dictionary<string, string>> _commentIdToShortIdMapCache;
        private readonly string _packageFile;
        private readonly string _baseDirectory;
        private readonly string _globPattern;
        private readonly string _msdnVersion;

        #region Entry Point

        private static int Main(string[] args)
        {
            if (args.Length != 4)
            {
                PrintUsage();
                return 1;
            }
            try
            {
                ServicePointManager.DefaultConnectionLimit = HttpMaxConcurrency;
                var p = new Program(args[0], args[1], args[2], args[3]);
                p.PackReference();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 2;
            }
        }

        #endregion

        #region Constructor

        public Program(string packageFile, string baseDirectory, string globPattern, string msdnVersion)
        {
            _packageFile = packageFile;
            _baseDirectory = baseDirectory;
            _globPattern = globPattern;
            _msdnVersion = msdnVersion;

            _shortIdCache = new Cache<string>(nameof(_shortIdCache), LoadShortIdAsync);
            _commentIdToShortIdMapCache = new Cache<Dictionary<string, string>>(nameof(_commentIdToShortIdMapCache), LoadCommentIdToShortIdMapAsync);
        }

        #endregion

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("{0} <packageFile> <baseDirectory> <globPattern> <msdnVersion>", AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("    packageFile    The output package file.");
            Console.WriteLine("                   e.g. \"msdn.rpk\"");
            Console.WriteLine("    baseDirectory  The base directory contains develop comment xml file.");
            Console.WriteLine("                   e.g. \"c:\\\"");
            Console.WriteLine("    globPattern    The glob pattern for develop comment xml file.");
            Console.WriteLine("                   '\' is considered as ESCAPE character, make sure to transform '\' in file path to '/'");
            Console.WriteLine("                   e.g. \"**/*.xml\"");
            Console.WriteLine("    msdnVersion    The version in msdn.");
            Console.WriteLine("                   e.g. \"vs.110\"");
        }

        private void PackReference()
        {
            var items = GetItems();
            using (var writer = ExternalReferencePackageWriter.Append(_packageFile, new Uri("https://msdn.microsoft.com/")))
            using (var enumrator = items.GetEnumerator())
            {
                while (enumrator.MoveNext())
                {
                    writer.AddOrUpdateEntry(enumrator.Current.EntryName + ".yml", enumrator.Current.ViewModel);
                }
            }
        }

        private IObservable<EntryNameAndViewModel> GetItems()
        {
            var semaphore = new SemaphoreSlim(HttpMaxConcurrency);
            return from item in GetRawItems()
                   from checkedVM in Observable.FromAsync(() => CheckAsync(item.ViewModel, semaphore))
                   where checkedVM.Count > 0
                   select new EntryNameAndViewModel(item.EntryName, checkedVM);
        }

        private IObservable<EntryNameAndViewModel> GetRawItems()
        {
            var semaphore = new SemaphoreSlim(HttpMaxConcurrency);
            return from list in GetAllCommentId().ToObservable()
                   from entry in list
                   from vm in Observable.FromAsync(() => GetReferenceVMAsync(entry, _msdnVersion, semaphore))
                   where vm.Count > 0
                   select new EntryNameAndViewModel(entry.EntryName, vm);
        }

        private async Task<List<ReferenceViewModel>> GetReferenceVMAsync(ClassEntry entry, string msdnVersion, SemaphoreSlim semaphore)
        {
            var urls = await Task.WhenAll(from item in entry.Items select GetMsdnUrlAsync(item, semaphore));
            return (from pair in entry.Items.Zip(urls, (item, url) => new { item, url })
                    where pair.url != null
                    select new ReferenceViewModel
                    {
                        Uid = pair.item.Uid,
                        Href = pair.url
                    }).ToList();
        }

        private async Task<string> GetMsdnUrlAsync(CommentIdAndUid pair, SemaphoreSlim semaphore)
        {
            if (NormalUid.IsMatch(pair.Uid))
            {
                return string.Format(MsdnUrlTemplate, pair.Uid.ToLower(), _msdnVersion);
            }
            else
            {
                await semaphore.WaitAsync();
                try
                {
                    var shortId = await _shortIdCache.GetAsync(pair.CommentId);
                    if (string.IsNullOrEmpty(shortId))
                    {
                        return null;
                    }
                    return string.Format(MsdnUrlTemplate, shortId, _msdnVersion);
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        private string GetContainingCommentId(string commentId)
        {
            var result = commentId;
            var index = result.IndexOf('(');
            if (index != -1)
            {
                result = result.Remove(index);
            }
            index = result.LastIndexOf('.');
            if (index != -1)
            {
                result = result.Remove(index);
                switch (result[0])
                {
                    case 'E':
                    case 'F':
                    case 'M':
                    case 'P':
                        return "T" + result.Substring(1);
                    default:
                        return "N" + result.Substring(1);
                }
            }
            return null;
        }

        private string GetAlias(string commentId)
        {
            if (commentId.StartsWith("M:") || commentId.StartsWith("P:"))
            {
                // method/property maybe have overloads.
                return null;
            }
            var uid = commentId.Substring(2);
            if (NormalUid.IsMatch(uid))
            {
                return uid;
            }
            return null;
        }

        private async Task<string> LoadShortIdAsync(string commentId)
        {
            string alias = GetAlias(commentId);
            string currentCommentId = commentId;
            if (alias == null)
            {
                do
                {
                    var containingCommentId = GetContainingCommentId(currentCommentId);
                    if (containingCommentId == null)
                    {
                        return string.Empty;
                    }
                    var dict = await _commentIdToShortIdMapCache.GetAsync(containingCommentId);
                    string shortId;
                    if (dict.TryGetValue(commentId, out shortId))
                    {
                        return shortId;
                    }
                    else
                    {
                        // maybe case not match.
                        shortId = dict.FirstOrDefault(p => string.Equals(p.Key, commentId, StringComparison.OrdinalIgnoreCase)).Value;
                        if (shortId != null)
                        {
                            return shortId;
                        }
                    }
                    currentCommentId = containingCommentId;
                } while (commentId[0] == 'T'); // handle nested type
            }
            else
            {
                using (var response = await _client.GetWithRetryAsync(string.Format(MsdnUrlTemplate, alias, _msdnVersion), 1000))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var xr = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore }))
                        {
                            while (xr.ReadToFollowing("meta"))
                            {
                                if (xr.GetAttribute("name") == "Search.ShortId" ||
                                    xr.GetAttribute("name") == "ms.shortidmsdn")
                                {
                                    return xr.GetAttribute("content");
                                }
                            }
                        }
                    }
                }
            }
            return string.Empty;
        }

        private async Task<Dictionary<string, string>> LoadCommentIdToShortIdMapAsync(string containingCommentId)
        {
            var result = new Dictionary<string, string>();
            var shortId = await _shortIdCache.GetAsync(containingCommentId);
            if (!string.IsNullOrEmpty(shortId))
            {
                using (var response = await _client.GetWithRetryAsync(string.Format(MtpsApiUrlTemplate, shortId, _msdnVersion), 1000))
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var xr = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore }))
                        {
                            foreach (var item in ReadApiContent(xr))
                            {
                                result[item.Item1] = item.Item2;
                            }
                        }
                    }
                }
            }
            return result;
        }

        private IEnumerable<Tuple<string, string>> ReadApiContent(XmlReader xr)
        {
            while (xr.ReadToFollowing("div"))
            {
                if (xr.GetAttribute("class") == "link-data")
                {
                    using (var subtree = xr.ReadSubtree())
                    {
                        string assetId = null;
                        string shortId = null;
                        while (subtree.ReadToFollowing("span"))
                        {
                            var className = subtree.GetAttribute("class");
                            if (className == "link-short-id")
                            {
                                shortId = subtree.ReadElementContentAsString();
                            }
                            if (className == "link-asset-id")
                            {
                                assetId = subtree.ReadElementContentAsString();
                            }
                        }
                        if (shortId != null && assetId != null && assetId.StartsWith("AssetId:"))
                        {
                            yield return Tuple.Create(assetId.Substring("AssetId:".Length), shortId);
                        }
                    }
                }
            }
        }

        private IEnumerable<List<ClassEntry>> GetAllCommentId()
        {
            return from file in GlobPathHelper.GetFiles(_baseDirectory, _globPattern)
                   select (from commentId in GetAllCommentId(file)
                           where commentId.StartsWith("T:") || commentId.StartsWith("E:") || commentId.StartsWith("F:") || commentId.StartsWith("M:") || commentId.StartsWith("P:")
                           let uid = commentId.Substring(2)
                           group new CommentIdAndUid(commentId, uid) by commentId.StartsWith("T:") ? uid : uid.Remove(uid.Split('(')[0].LastIndexOf('.')) into g
                           select new ClassEntry(g.Key, g.ToList())).ToList();
        }

        private IEnumerable<string> GetAllCommentId(string file)
        {
            Console.WriteLine("Loading comment id from {0} ...", file);
            return from reader in
                       new Func<XmlReader>(() => XmlReader.Create(file))
                       .EmptyIfThrow()
                       .ProtectResource()
                   where reader.ReadToFollowing("members")
                   from apiReader in reader.Elements("member")
                   let commentId = apiReader.GetAttribute("name")
                   where commentId != null
                   select commentId;
        }

        private async Task<List<ReferenceViewModel>> CheckAsync(List<ReferenceViewModel> vm, SemaphoreSlim semaphore)
        {
            return (from pair in
                       (await Task.WhenAll(from item in vm select IsUrlOkAsync(item.Href, semaphore)))
                       .Zip(vm, (isOK, item) => new { IsOK = isOK, Item = item })
                    where pair.IsOK
                    select pair.Item).ToList();
        }

        private async Task<bool> IsUrlOkAsync(string url, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                using (var response = await _client.GetWithRetryAsync(url, 1000))
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
