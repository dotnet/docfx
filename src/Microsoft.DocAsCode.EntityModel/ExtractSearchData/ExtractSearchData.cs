namespace Microsoft.DocAsCode.EntityModel.ExtractSearchData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.EntityModel.ViewModels;
    using Microsoft.DocAsCode.Utility;

    public class ExtractSearchData
    {
        private static readonly Regex RgxWord = new Regex(@"\w{3,}", RegexOptions.Compiled);
        private static readonly Regex RgxMarkdownTitle1 = new Regex(@"^#+(.+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex RgxMarkdownTitle2 = new Regex(@"([^\n]+)\r?\n(-|=)+(\r?\n|$)", RegexOptions.Compiled | RegexOptions.Singleline);
        
        private static readonly string Prefix = string.Empty;
        private static readonly string WordsJoinSpliter = " ";

        public static void GenerateSearchDataFile(string rootPath, string searchDataFile = "search-data.json")
        {
            searchDataFile = Path.Combine(rootPath, searchDataFile);
            var searchItemList = TraversePath(rootPath);
            var searchData = new Dictionary<string, SearchItem>();
            foreach (var searchItem in searchItemList)
            {
                searchData[searchItem.Path] = searchItem;
            }
            JsonUtility.Serialize(searchDataFile, searchData, Formatting.Indented);
        }

        public static SearchItem ExtractIndexOfFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            var text = File.ReadAllText(filePath);
            var ext = Path.GetExtension(filePath);
            SearchItem item = null;
            switch (ext.ToLower())
            {
                case ".md":
                    item = ExtractIndexFromMarkdown(text);
                    break;
                case ".yml":
                    item = ExtractIndexFromYml(text);
                    break;
                default:
                    var msg = $"{filePath} -- {ext} is not currently searchable";
                    Logger.Log(LogLevel.Warning, msg);
                    break;
            }
            return item;
        }

        public static SearchItem ExtractIndexFromMarkdown(string text)
        {
            var match = PriorMatch(RgxMarkdownTitle1.Match(text), RgxMarkdownTitle2.Match(text));
            string title;
            if (match == null)
            {
                title = string.Empty;
            }
            else
            {
                var sentence = match.Groups[1].Value;
                title = string.Join(WordsJoinSpliter, SplitWords(sentence));
            }
            var keywords = string.Join(WordsJoinSpliter, SplitWords(text));
            return new SearchItem { Title = title, Keywords = keywords };
        }

        public static SearchItem ExtractIndexFromYml(string text)
        {
            var input = new StringReader(text);
            var item = YamlUtility.Deserialize<PageViewModel>(input);
            if (item == null) return null;
            var title = string.Join(WordsJoinSpliter, SplitItemsWords(item.Items, t => t.Name));
            var keyword = string.Join(WordsJoinSpliter,
                SplitItemsWords(item.Items, t => t.FullName + " " + t.Summary + " " + t.Remarks));
            return new SearchItem { Title = title, Keywords = keyword };
        }

        private static IEnumerable<SearchItem> TraversePath(string rootPath, string tocName = "toc.yml")
        {
            var searchItemList = new List<SearchItem>();
            TraversePathCore(Prefix, rootPath, tocName, searchItemList);
            return searchItemList;
        }

        private static void TraversePathCore(string prefix, string curPath, string tocName, ICollection<SearchItem> searchItemList)
        {
            var tocFilePath = Path.Combine(curPath, tocName);
            if (!File.Exists(tocFilePath))
            {
                var msg = $"{tocFilePath} not found";
                Logger.Log(LogLevel.Info, msg);
                return;
            }

            var items = YamlUtility.Deserialize<TocViewModel>(tocFilePath);
            foreach (var item in items)
            {
                AddItem(item, prefix, curPath, tocName, searchItemList);
            }
        }

        private static void AddItem(TocItemViewModel item, string prefix, string curPath, string tocName, ICollection<SearchItem> searchItemList)
        {
            if (item.Href == null) return;
            // markdown or yml file
            if (!string.IsNullOrEmpty(Path.GetExtension(item.Href)))
            {
                var filePath = Path.Combine(curPath, item.Href);
                try
                {
                    var searchItem = ExtractIndexOfFile(filePath);
                    if (searchItem != null)
                    {
                        searchItem.Display = item.Name;
                        searchItem.Path =
                            prefix != Prefix ?
                            Path.Combine(prefix, "!" + item.Href).ToNormalizedPath() :
                            Path.Combine(prefix, item.Href).ToNormalizedPath();
                        searchItemList.Add(searchItem);
                    }
                }
                catch (Exception e)
                {
                    Logger.Log(LogLevel.Warning, $"File {filePath} is not valid, ignored: {e.Message}");
                }
            }
            // folder
            else
            {
                var subPath = Path.Combine(curPath, item.Href);
                TraversePathCore(Path.Combine(prefix, item.Href).ToNormalizedPath(), subPath, tocName, searchItemList);
            }

            if (item.Items == null) return;
            foreach (var subItem in item.Items)
            {
                AddItem(subItem, prefix, curPath, tocName, searchItemList);
            }
        }

        private static IEnumerable<string> SplitItemsWords<T>(IEnumerable<T> items, Func<T, string> makeSentence)
        {
            return
                from item in items
                from word in SplitWords(makeSentence(item))
                select word;
        }

        private static IEnumerable<string> SplitWords(string sentence)
        {
            return
                from Match matchWord in RgxWord.Matches(sentence)
                select matchWord.Value;
        }
        private static Match PriorMatch(Match match1, Match match2)
        {
            if (!match1.Success && !match2.Success) return null;
            if (!match1.Success) return match2;
            if (!match2.Success) return match1;
            return match1.Index < match2.Index ? match1 : match2;
        }
    }
}