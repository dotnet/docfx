namespace Microsoft.DocAsCode.ExternalPackageGenerators.Msdn
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Utility;
    using System;
    using System.Configuration;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reactive.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    internal sealed class Program
    {

        #region Consts/Fields
        private static string MsdnUrlTemplate = "https://msdn.microsoft.com/en-us/library/{0}(v={1}).aspx";
        private static string MtpsApiUrlTemplate = "http://services.mtps.microsoft.com/ServiceAPI/content/{0}/en-us;{1}/common/mtps.links";
        private static int HttpMaxConcurrency = 64;

        private static int[] RetryDelay = new[] { 1000, 3000, 10000 };
        private static int StatisticsFreshPerSecond = 5;

        private readonly Regex NormalUid = new Regex(@"^[a-zA-Z0-9\.]+$", RegexOptions.Compiled);
        private readonly HttpClient _client = new HttpClient();

        private readonly Cache<string> _shortIdCache;
        private readonly Cache<Dictionary<string, string>> _commentIdToShortIdMapCache;
        private readonly Cache<StrongBox<bool>> _checkUrlCache;

        private readonly string _packageFile;
        private readonly string _baseDirectory;
        private readonly string _globPattern;
        private readonly string _msdnVersion;

        private readonly int _maxHttp;
        private readonly int _maxEntry;

        private readonly SemaphoreSlim _semaphoreForHttp;
        private readonly SemaphoreSlim _semaphoreForEntry;

        private int _entryCount;
        private int _apiCount;
        #endregion

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
                ReadConfig();
                ServicePointManager.DefaultConnectionLimit = HttpMaxConcurrency;
                ThreadPool.SetMinThreads(HttpMaxConcurrency, HttpMaxConcurrency);
                ThreadPool.SetMaxThreads(HttpMaxConcurrency * 2, HttpMaxConcurrency * 2);
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

        private static void ReadConfig()
        {
            MsdnUrlTemplate = ConfigurationManager.AppSettings["MsdnUrlTemplate"] ?? MsdnUrlTemplate;
            MtpsApiUrlTemplate = ConfigurationManager.AppSettings["MsdnUrlTemplate"] ?? MtpsApiUrlTemplate;
            if (ConfigurationManager.AppSettings["HttpMaxConcurrency"] != null)
            {
                int value;
                if (int.TryParse(ConfigurationManager.AppSettings["HttpMaxConcurrency"], out value) && value > 0)
                {
                    HttpMaxConcurrency = value;
                }
                else
                {
                    Console.WriteLine("Bad config: HttpMaxConcurrency, using default value:{0}", HttpMaxConcurrency);
                }
            }
            if (ConfigurationManager.AppSettings["RetryDelayInMillisecond"] != null)
            {
                string[] texts;
                texts = ConfigurationManager.AppSettings["RetryDelayInMillisecond"].Split(',');
                var values = new int[texts.Length];
                for (int i = 0; i < texts.Length; i++)
                {
                    if (!int.TryParse(texts[i].Trim(), out values[i]))
                    {
                        break;
                    }
                }
                if (values.All(v => v > 0))
                {
                    RetryDelay = values;
                }
                else
                {
                    Console.WriteLine("Bad config: RetryDelayInMillisecond, using default value:{0}", string.Join(", ", RetryDelay));
                }
            }
            if (ConfigurationManager.AppSettings["StatisticsFreshPerSecond"] != null)
            {
                int value;
                if (int.TryParse(ConfigurationManager.AppSettings["StatisticsFreshPerSecond"], out value) && value > 0)
                {
                    StatisticsFreshPerSecond = value;
                }
                else
                {
                    Console.WriteLine("Bad config: StatisticsFreshPerSecond, using default value:{0}", HttpMaxConcurrency);
                }
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
            _checkUrlCache = new Cache<StrongBox<bool>>(nameof(_checkUrlCache), IsUrlOkAsync);

            _maxHttp = HttpMaxConcurrency;
            _maxEntry = (int)(HttpMaxConcurrency * 1.5) + 1;

            _semaphoreForHttp = new SemaphoreSlim(_maxHttp);
            _semaphoreForEntry = new SemaphoreSlim(_maxEntry);
        }

        #endregion

        #region Methods

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
            var stopwatch = Stopwatch.StartNew();
            using (var writer = ExternalReferencePackageWriter.Append(_packageFile, new Uri("https://msdn.microsoft.com/")))
            {
                var task = GetItems().ForEachAsync(pair =>
                {
                    lock (this)
                    {
                        writer.AddOrUpdateEntry(pair.EntryName + ".yml", pair.ViewModel);
                        _entryCount++;
                        _apiCount += pair.ViewModel.Count;
                    }
                });
                PrintStatistics(task, stopwatch).Wait();
            }
        }

        private async Task PrintStatistics(Task mainTask, Stopwatch stopwatch)
        {
            bool isFirst = true;
            var queue = new Queue<Tuple<int, int>>(10 * StatisticsFreshPerSecond);
            while (!mainTask.IsCompleted)
            {
                await Task.Delay(1000 / StatisticsFreshPerSecond);
                if (queue.Count >= 10 * StatisticsFreshPerSecond)
                {
                    queue.Dequeue();
                }
                var last = Tuple.Create(_entryCount, _apiCount);
                queue.Enqueue(last);
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 3);
                }
                Console.WriteLine(
                    "Elapsed time: {0}, generated type: {1}, generated api: {2}",
                    stopwatch.Elapsed.ToString(@"hh\:mm\:ss"),
                    _entryCount.ToString(),
                    _apiCount.ToString());
                Console.WriteLine(
                    "Status: http:{0,4}, type entry:{1,4}",
                    (_maxHttp - _semaphoreForHttp.CurrentCount).ToString(),
                    (_maxEntry - _semaphoreForEntry.CurrentCount).ToString());
                Console.WriteLine(
                    "Generating per second: type:{0,7}, api:{1,7}",
                    ((double)(last.Item1 - queue.Peek().Item1) * StatisticsFreshPerSecond / queue.Count).ToString("F1"),
                    ((double)(last.Item2 - queue.Peek().Item2) * StatisticsFreshPerSecond / queue.Count).ToString("F1"));
            }
            try
            {
                await mainTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private IObservable<EntryNameAndViewModel> GetItems()
        {
            return from entry in
                       (from list in GetAllCommentId()
                        from entry in list
                        select entry).ToObservable()
                   from vm in Observable.FromAsync(() => GetReferenceVMAsync(entry))
                   where vm.Count > 0
                   select new EntryNameAndViewModel(entry.EntryName, vm);
        }

        private async Task<List<ReferenceViewModel>> GetReferenceVMAsync(ClassEntry entry)
        {
            await _semaphoreForEntry.WaitAsync();
            List<ReferenceViewModel> result = new List<ReferenceViewModel>(entry.Items.Count);
            try
            {
                foreach (var item in entry.Items)
                {
                    result.Add(await GetViewModelItemAsync(item));
                }
            }
            finally
            {
                _semaphoreForEntry.Release();
            }
            var type = result.Find(item => item.Uid == entry.EntryName);
            if (type != null && type.Href != null)
            {
                // handle enum field, or other one-page-member
                foreach (var item in result)
                {
                    if (item.Href == null)
                    {
                        item.Href = type.Href;
                    }
                }
            }
            else
            {
                result.RemoveAll(item => item.Href == null);
            }
            return result;
        }

        private async Task<ReferenceViewModel> GetViewModelItemAsync(CommentIdAndUid pair)
        {
            var alias = GetAlias(pair.CommentId);
            if (alias != null)
            {
                var url = string.Format(MsdnUrlTemplate, alias, _msdnVersion);
                // verify alias exists
                if ((await _checkUrlCache.GetAsync(url)).Value)
                {
                    return new ReferenceViewModel
                    {
                        Uid = pair.Uid,
                        Href = url,
                    };
                }
            }
            var shortId = await _shortIdCache.GetAsync(pair.CommentId);
            if (string.IsNullOrEmpty(shortId))
            {
                return new ReferenceViewModel
                {
                    Uid = pair.Uid,
                };
            }
            return new ReferenceViewModel
            {
                Uid = pair.Uid,
                Href = string.Format(MsdnUrlTemplate, shortId, _msdnVersion),
            };
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
                return uid.ToLower();
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
                using (var response = await _client.GetWithRetryAsync(string.Format(MsdnUrlTemplate, alias, _msdnVersion), _semaphoreForHttp, RetryDelay))
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
                using (var response = await _client.GetWithRetryAsync(string.Format(MtpsApiUrlTemplate, shortId, _msdnVersion), _semaphoreForHttp, RetryDelay))
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

        private async Task<StrongBox<bool>> IsUrlOkAsync(string url)
        {
            using (var response = await _client.GetWithRetryAsync(url, _semaphoreForHttp, RetryDelay))
            {
                return new StrongBox<bool>(response.StatusCode == HttpStatusCode.OK);
            }
        }

        #endregion

    }
}
