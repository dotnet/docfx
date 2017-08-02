// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.ExternalPackageGenerators.Msdn
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Glob;
    using Microsoft.DocAsCode.Plugins;

    internal sealed class Program
    {

        #region Fields
        private static readonly Regex GenericMethodPostFix = new Regex(@"``\d+$", RegexOptions.Compiled);

        private static string MsdnUrlTemplate = "https://msdn.microsoft.com/en-us/library/{0}(v={1}).aspx";
        private static string MtpsApiUrlTemplate = "http://services.mtps.microsoft.com/ServiceAPI/content/{0}/en-us;{1}/common/mtps.links";
        private static int HttpMaxConcurrency = 64;

        private static int[] RetryDelay = new[] { 1000, 3000, 10000 };
        private static int StatisticsFreshPerSecond = 5;

        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        private readonly Regex NormalUid = new Regex(@"^[a-zA-Z0-9_\.]+$", RegexOptions.Compiled);
        private readonly HttpClient _client = new HttpClient();

        private readonly Cache<string> _shortIdCache;
        private readonly Cache<Dictionary<string, string>> _commentIdToShortIdMapCache;
        private readonly Cache<StrongBox<bool?>> _checkUrlCache;

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
        private string[] _currentPackages = new string[2];
        #endregion

        #region Entry Point

        private static int Main(string[] args)
        {
            if (args.Length != 4)
            {
                PrintUsage();
                return 2;
            }
            try
            {
                ReadConfig();
                ServicePointManager.DefaultConnectionLimit = HttpMaxConcurrency;
                ThreadPool.SetMinThreads(HttpMaxConcurrency, HttpMaxConcurrency);
                var p = new Program(args[0], args[1], args[2], args[3]);
                p.PackReference();
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void ReadConfig()
        {
            MsdnUrlTemplate = ConfigurationManager.AppSettings["MsdnUrlTemplate"] ?? MsdnUrlTemplate;
            MtpsApiUrlTemplate = ConfigurationManager.AppSettings["MtpsApiUrlTemplate"] ?? MtpsApiUrlTemplate;
            if (ConfigurationManager.AppSettings["HttpMaxConcurrency"] != null)
            {
                if (int.TryParse(ConfigurationManager.AppSettings["HttpMaxConcurrency"], out int value) && value > 0)
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
                if (int.TryParse(ConfigurationManager.AppSettings["StatisticsFreshPerSecond"], out int value) && value > 0)
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

        public Program(string packageDirectory, string baseDirectory, string globPattern, string msdnVersion)
        {
            _packageFile = packageDirectory;
            _baseDirectory = baseDirectory;
            _globPattern = globPattern;
            _msdnVersion = msdnVersion;

            _shortIdCache = new Cache<string>(nameof(_shortIdCache), LoadShortIdAsync);
            _commentIdToShortIdMapCache = new Cache<Dictionary<string, string>>(nameof(_commentIdToShortIdMapCache), LoadCommentIdToShortIdMapAsync);
            _checkUrlCache = new Cache<StrongBox<bool?>>(nameof(_checkUrlCache), IsUrlOkAsync);

            _maxHttp = HttpMaxConcurrency;
            _maxEntry = (int)(HttpMaxConcurrency * 1.1) + 1;

            _semaphoreForHttp = new SemaphoreSlim(_maxHttp);
            _semaphoreForEntry = new SemaphoreSlim(_maxEntry);
        }

        #endregion

        #region Methods

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("{0} <outputFile> <baseDirectory> <globPattern> <msdnVersion>", AppDomain.CurrentDomain.FriendlyName);
            Console.WriteLine("    outputFile      The output xref archive file.");
            Console.WriteLine("                    e.g. \"msdn\"");
            Console.WriteLine("    baseDirectory   The base directory contains develop comment xml file.");
            Console.WriteLine("                    e.g. \"c:\\\"");
            Console.WriteLine("    globPattern     The glob pattern for develop comment xml file.");
            Console.WriteLine("                    '\' is considered as ESCAPE character, make sure to transform '\' in file path to '/'");
            Console.WriteLine("                    e.g. \"**/*.xml\"");
            Console.WriteLine("    msdnVersion     The version in msdn.");
            Console.WriteLine("                    e.g. \"vs.110\"");
        }

        private void PackReference()
        {
            var task = PackReferenceAsync();
            PrintStatistics(task).Wait();
        }

        private async Task PackReferenceAsync()
        {
            var files = GetAllFiles();
            if (files.Count == 0)
            {
                return;
            }
            var dir = Path.GetDirectoryName(_packageFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            bool updateMode = File.Exists(_packageFile);
            using (var writer = XRefArchive.Open(_packageFile, updateMode ? XRefArchiveMode.Update : XRefArchiveMode.Create))
            {
                if (!updateMode)
                {
                    writer.CreateMajor(new XRefMap { HrefUpdated = true, Redirections = new List<XRefMapRedirection>() });
                }

                if (files.Count == 1)
                {
                    await PackOneReferenceAsync(files[0], writer);
                    return;
                }
                // left is smaller files, right is bigger files.
                var left = 0;
                var right = files.Count - 1;
                var leftTask = PackOneReferenceAsync(files[left], writer);
                var rightTask = PackOneReferenceAsync(files[right], writer);
                while (left <= right)
                {
                    var completed = await Task.WhenAny(new[] { leftTask, rightTask }.Where(t => t != null));
                    await completed; // throw if any error.
                    if (completed == leftTask)
                    {
                        left++;
                        if (left < right)
                        {
                            leftTask = PackOneReferenceAsync(files[left], writer);
                        }
                        else
                        {
                            leftTask = null;
                        }
                    }
                    else
                    {
                        right--;
                        if (left < right)
                        {
                            rightTask = PackOneReferenceAsync(files[right], writer);
                        }
                        else
                        {
                            rightTask = null;
                        }
                    }
                }
            }
        }

        private async Task PackOneReferenceAsync(string file, XRefArchive writer)
        {
            var currentPackage = Path.GetFileName(file);
            lock (_currentPackages)
            {
                _currentPackages[Array.IndexOf(_currentPackages, null)] = currentPackage;
            }
            var entries = new List<XRefMapRedirection>();
            await GetItems(file).ForEachAsync(pair =>
            {
                lock (writer)
                {
                    entries.Add(
                        new XRefMapRedirection
                        {
                            UidPrefix = pair.EntryName,
                            Href = writer.CreateMinor(
                                new XRefMap
                                {
                                    Sorted = true,
                                    HrefUpdated = true,
                                    References = pair.ViewModel,
                                },
                                new[] { pair.EntryName })
                        });
                    _entryCount++;
                    _apiCount += pair.ViewModel.Count;
                }
            });
            lock (writer)
            {
                var map = writer.GetMajor();
                map.Redirections = (from r in map.Redirections.Concat(entries)
                                    orderby r.UidPrefix.Length descending
                                    select r).ToList();
                writer.UpdateMajor(map);
            }
            lock (_currentPackages)
            {
                _currentPackages[Array.IndexOf(_currentPackages, currentPackage)] = null;
            }
        }

        private async Task PrintStatistics(Task mainTask)
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
                    Console.SetCursorPosition(0, Console.CursorTop - 3 - _currentPackages.Length);
                }
                lock (_currentPackages)
                {
                    for (int i = 0; i < _currentPackages.Length; i++)
                    {
                        Console.WriteLine("Packing: " + (_currentPackages[i] ?? string.Empty).PadRight(Console.WindowWidth - "Packing: ".Length - 1));
                    }
                }
                Console.WriteLine(
                    "Elapsed time: {0}, generated type: {1}, generated api: {2}",
                    Stopwatch.Elapsed.ToString(@"hh\:mm\:ss"),
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

        private IObservable<EntryNameAndViewModel> GetItems(string file)
        {
            return from entry in
                       (from entry in GetAllCommentId(file)
                        select entry).AcquireSemaphore(_semaphoreForEntry).ToObservable(Scheduler.Default)
                   from vm in Observable.FromAsync(() => GetXRefSpecAsync(entry)).Do(_ => _semaphoreForEntry.Release())
                   where vm.Count > 0
                   select new EntryNameAndViewModel(entry.EntryName, vm);
        }

        private async Task<List<XRefSpec>> GetXRefSpecAsync(ClassEntry entry)
        {
            var result = new List<XRefSpec>(entry.Items.Count);
            var type = entry.Items.Find(item => item.Uid == entry.EntryName);
            XRefSpec typeSpec = null;
            if (type != null)
            {
                typeSpec = await GetXRefSpecAsync(type);
                if (typeSpec != null)
                {
                    result.Add(typeSpec);
                }
            }
            int size = 0;
            foreach (var specsTask in
                from block in
                    (from item in entry.Items
                     where item != type
                     select item).BlockBuffer(() => size = size * 2 + 1)
                select Task.WhenAll(from item in block select GetXRefSpecAsync(item)))
            {
                result.AddRange(from s in await specsTask where s != null select s);
            }
            result.AddRange(await GetOverloadXRefSpecAsync(result));
            if (typeSpec != null && typeSpec.Href != null)
            {
                // handle enum field, or other one-page-member
                foreach (var item in result)
                {
                    if (item.Href == null)
                    {
                        item.Href = typeSpec.Href;
                    }
                }
            }
            else
            {
                result.RemoveAll(item => item.Href == null);
            }
            result.Sort(XRefSpecUidComparer.Instance);
            return result;
        }

        private async Task<XRefSpec> GetXRefSpecAsync(CommentIdAndUid pair)
        {
            var alias = GetAliasWithMember(pair.CommentId);
            if (alias != null)
            {
                var url = string.Format(MsdnUrlTemplate, alias, _msdnVersion);
                // verify alias exists
                var vr = (await _checkUrlCache.GetAsync(pair.CommentId + "||||" + url)).Value;
                if (vr == true)
                {
                    return new XRefSpec
                    {
                        Uid = pair.Uid,
                        CommentId = pair.CommentId,
                        Href = url,
                    };
                }
            }
            var shortId = await _shortIdCache.GetAsync(pair.CommentId);
            if (string.IsNullOrEmpty(shortId))
            {
                if (pair.CommentId.StartsWith("F:"))
                {
                    // work around for enum field.
                    shortId = await _shortIdCache.GetAsync(
                        "T:" + pair.CommentId.Remove(pair.CommentId.LastIndexOf('.')).Substring(2));
                    if (string.IsNullOrEmpty(shortId))
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            return new XRefSpec
            {
                Uid = pair.Uid,
                CommentId = pair.CommentId,
                Href = string.Format(MsdnUrlTemplate, shortId, _msdnVersion),
            };
        }

        private async Task<XRefSpec[]> GetOverloadXRefSpecAsync(List<XRefSpec> specs)
        {
            var pairs = (from spec in specs
                         let overload = GetOverloadIdBody(spec)
                         where overload != null
                         group Tuple.Create(spec, overload) by overload.Uid into g
                         select g.First());
            return await Task.WhenAll(from pair in pairs select GetOverloadXRefSpecCoreAsync(pair));
        }

        private async Task<XRefSpec> GetOverloadXRefSpecCoreAsync(Tuple<XRefSpec, CommentIdAndUid> pair)
        {
            var dict = await _commentIdToShortIdMapCache.GetAsync(pair.Item1.CommentId);
            if (dict.TryGetValue(pair.Item2.CommentId, out string shortId))
            {
                return new XRefSpec
                {
                    Uid = pair.Item2.Uid,
                    CommentId = pair.Item2.CommentId,
                    Href = string.Format(MsdnUrlTemplate, shortId, _msdnVersion),
                };
            }
            else
            {
                return new XRefSpec(pair.Item1)
                {
                    Uid = pair.Item2.Uid
                };
            }
        }

        private static CommentIdAndUid GetOverloadIdBody(XRefSpec pair)
        {
            switch (pair.CommentId[0])
            {
                case 'M':
                case 'P':
                    var body = pair.Uid;
                    var index = body.IndexOf('(');
                    if (index != -1)
                    {
                        body = body.Remove(index);
                    }
                    body = GenericMethodPostFix.Replace(body, string.Empty);
                    return new CommentIdAndUid("Overload:" + body, body + "*");
                default:
                    return null;
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
                    case 'O':
                        return "T" + result.Substring("Overload".Length);
                    default:
                        return "N" + result.Substring(1);
                }
            }
            return null;
        }

        private string GetAlias(string commentId)
        {
            if (!commentId.StartsWith("T:") && !commentId.StartsWith("N:"))
            {
                return null;
            }
            var uid = commentId.Substring(2);
            if (NormalUid.IsMatch(uid))
            {
                return uid.ToLower();
            }
            return null;
        }

        private string GetAliasWithMember(string commentId)
        {
            var uid = commentId.Substring(2);
            var parameterIndex = uid.IndexOf('(');
            if (parameterIndex != -1)
            {
                uid = uid.Remove(parameterIndex);
            }
            uid = GenericMethodPostFix.Replace(uid, string.Empty);
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
            if (alias != null)
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
            do
            {
                var containingCommentId = GetContainingCommentId(currentCommentId);
                if (containingCommentId == null)
                {
                    return string.Empty;
                }
                var dict = await _commentIdToShortIdMapCache.GetAsync(containingCommentId);
                if (dict.TryGetValue(commentId, out string shortId))
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

        private IEnumerable<ClassEntry> GetAllCommentId(string file)
        {
            return from commentId in GetAllCommentIdCore(file)
                   where commentId.StartsWith("N:") || commentId.StartsWith("T:") || commentId.StartsWith("E:") || commentId.StartsWith("F:") || commentId.StartsWith("M:") || commentId.StartsWith("P:")
                   let uid = commentId.Substring(2)
                   let lastDot = uid.Split('(')[0].LastIndexOf('.')
                   group new CommentIdAndUid(commentId, uid) by commentId.StartsWith("N:") || commentId.StartsWith("T:") || lastDot == -1 ? uid : uid.Remove(lastDot) into g
                   select new ClassEntry(g.Key, g.ToList());
        }

        private List<string> GetAllFiles()
        {
            // just guess: the bigger files contains more apis/types.
            var files = (from file in FileGlob.GetFiles(_baseDirectory, new string[] { _globPattern }, null)
                         let fi = new FileInfo(file)
                         orderby fi.Length
                         select file).ToList();
            if (files.Count > 0)
            {
                Console.WriteLine("Loading comment id from:");
                foreach (var file in files)
                {
                    Console.WriteLine(file);
                }
            }
            else
            {
                Console.WriteLine("File not found.");
            }
            return files;
        }

        private IEnumerable<string> GetAllCommentIdCore(string file)
        {
            if (".xml".Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase))
            {
                return GetAllCommentIdFromXml(file);
            }
            if (".txt".Equals(Path.GetExtension(file), StringComparison.OrdinalIgnoreCase))
            {
                return GetAllCommentIdFromText(file);
            }
            throw new NotSupportedException($"Unable to read comment id from file: {file}.");
        }

        private IEnumerable<string> GetAllCommentIdFromXml(string file)
        {
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

        private IEnumerable<string> GetAllCommentIdFromText(string file)
        {
            return File.ReadLines(file);
        }

        private async Task<StrongBox<bool?>> IsUrlOkAsync(string pair)
        {
            var index = pair.IndexOf("||||");
            var commentId = pair.Remove(index);
            var url = pair.Substring(index + "||||".Length);

            using (var response = await _client.GetWithRetryAsync(url, _semaphoreForHttp, RetryDelay))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return new StrongBox<bool?>(null);
                }
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var xr = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore }))
                {
                    while (xr.ReadToFollowing("meta"))
                    {
                        if (xr.GetAttribute("name") == "ms.assetid")
                        {
                            return new StrongBox<bool?>(commentId.Equals(xr.GetAttribute("content"), StringComparison.OrdinalIgnoreCase));
                        }
                    }
                }
                return new StrongBox<bool?>(false);
            }
        }

        #endregion

    }
}
