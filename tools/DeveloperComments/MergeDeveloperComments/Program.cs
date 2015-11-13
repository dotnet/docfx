// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.DeveloperComments.MergeDeveloperComments
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Xml;
    using System.Xml.Linq;

    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.EntityModel.ViewModels;

    internal sealed class Program : IDisposable
    {

        #region Fields

        private const int MaxQueueCount = 64;
        private const int ConsumerCount = 4;

        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, List<UidAndComment>> _aggregator = new Dictionary<string, List<UidAndComment>>();
        private readonly Queue<string> _queue = new Queue<string>(MaxQueueCount);
        private readonly string[] _workingSet = new string[ConsumerCount];
        private readonly ManualResetEventSlim _producerCompleted = new ManualResetEventSlim(false);
        private readonly CountdownEvent _consumerCompleted = new CountdownEvent(ConsumerCount);
        private readonly List<Exception> _exceptions = new List<Exception>();
        private readonly string _yamlDirectory;
        private readonly string _developerCommentDirectory;

        #endregion

        #region Entry Point

        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                PrintUsage();
                return 2;
            }
            try
            {
                using (var p = new Program(args[0], args[1]))
                {
                    p.PatchDeveloperComments();
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        #endregion

        #region Constructor

        public Program(string yamlDirectory, string developCommentDirectory)
        {
            _yamlDirectory = yamlDirectory;
            _developerCommentDirectory = developCommentDirectory;
        }

        #endregion

        #region Methods

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("{0} <yamlDirectory> <developCommentDirectory>", AppDomain.CurrentDomain.FriendlyName);
        }

        private void PatchDeveloperComments()
        {
            CheckParameters();
            new Thread(ProduceDeveloperComments).Start();
            for (int i = 0; i < ConsumerCount; i++)
            {
                new Thread(ConsumeDeveloperComments).Start();
            }
            _producerCompleted.Wait();
            while (!_consumerCompleted.Wait(1))
            {
                lock (_syncRoot)
                {
                    Monitor.PulseAll(_syncRoot);
                }
            }
            if (_exceptions.Count > 0)
            {
                throw new AggregateException(_exceptions);
            }
        }

        private void CheckParameters()
        {
            bool hasError = false;
            if (!Directory.Exists(_yamlDirectory))
            {
                Console.WriteLine($"Directory not found: {_yamlDirectory}");
                hasError = true;
            }
            else if (!File.Exists(Path.Combine(_yamlDirectory, ".manifest")))
            {
                Console.WriteLine($"Manifest file not found in directory: {_yamlDirectory}");
                hasError = true;
            }
            if (!Directory.Exists(_developerCommentDirectory))
            {
                Console.WriteLine($"Directory not found: {_developerCommentDirectory}");
                hasError = true;
            }
            if (hasError)
            {
                throw new Exception();
            }
        }

        private void ProduceDeveloperComments()
        {
            try
            {
                ProduceDeveloperCommentsCore();
            }
            catch (Exception ex)
            {
                lock (_syncRoot)
                {
                    _exceptions.Add(ex);
                }
            }
            finally
            {
                _producerCompleted.Set();
            }
        }

        private void ProduceDeveloperCommentsCore()
        {
            var map = GetYamlFileMap();
            foreach (var uidAndReader in from f in EnumerateDevelopCommentFiles()
                                         from uidAndReader in EnumerateDeveloperComments(f)
                                         select uidAndReader)
            {
                string yamlFile;
                if (!map.TryGetValue(uidAndReader.Uid, out yamlFile))
                {
                    continue;
                }
                var uidAndElement = uidAndReader.ToUidAndElement();
                lock (_syncRoot)
                {
                    List<UidAndComment> list;
                    if (_aggregator.TryGetValue(yamlFile, out list))
                    {
                        list.Add(uidAndElement);
                    }
                    else
                    {
                        while (_queue.Count == MaxQueueCount)
                        {
                            Monitor.Wait(_syncRoot);
                        }
                        if (_queue.Count == 0)
                        {
                            Monitor.PulseAll(_syncRoot);
                        }
                        _queue.Enqueue(yamlFile);
                        _aggregator.Add(yamlFile, new List<UidAndComment> { uidAndElement });
                    }
                }
            }
        }

        private Dictionary<string, string> GetYamlFileMap() =>
            JsonUtility.Deserialize<Dictionary<string, string>>(Path.Combine(_yamlDirectory, ".manifest"));

        private IEnumerable<string> EnumerateDevelopCommentFiles() =>
            Directory.EnumerateFiles(_developerCommentDirectory, "*.xml", SearchOption.AllDirectories);

        private IEnumerable<UidAndReader> EnumerateDeveloperComments(string file)
        {
            Console.WriteLine($"Loading developer comments form file: {file}");
            return from reader in
                       new Func<XmlReader>(() => XmlReader.Create(file))
                       .EmptyIfThrow()
                       .ProtectResource()
                   where reader.ReadToFollowing("members")
                   from apiReader in reader.Elements("member")
                   let commentId = apiReader.GetAttribute("name")
                   where commentId != null && commentId.Length > 2 && commentId[1] == ':'
                   select new UidAndReader { Uid = commentId.Substring(2), Reader = apiReader };
        }

        private void ConsumeDeveloperComments()
        {
            try
            {
                ConsumeDeveloperCommentsCore();
            }
            catch (Exception ex)
            {
                lock (_syncRoot)
                {
                    _exceptions.Add(ex);
                }
            }
            finally
            {
                _consumerCompleted.Signal();
            }
        }

        private void ConsumeDeveloperCommentsCore()
        {
            string yamlFile = null;
            while (true)
            {
                List<UidAndComment> list;
                lock (_syncRoot)
                {
                    if (yamlFile != null)
                    {
                        for (int i = 0; i < _workingSet.Length; i++)
                        {
                            if (_workingSet[i] == yamlFile)
                            {
                                _workingSet[i] = null;
                            }
                        }
                    }
                    yamlFile = GetYamlFileFromQueue();
                    if (yamlFile == null)
                    {
                        return;
                    }
                    list = _aggregator[yamlFile];
                    _aggregator.Remove(yamlFile);
                }
                try
                {
                    PatchYamlFile(yamlFile, list);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred in file {yamlFile}:\r\n{ex}");
                }
            }
        }

        private string GetYamlFileFromQueue()
        {
            while (true)
            {
                if (_queue.Count == 0)
                {
                    if (_producerCompleted.Wait(0))
                    {
                        return null;
                    }
                    Monitor.Wait(_syncRoot);
                    continue;
                }
                if (_queue.Count < MaxQueueCount / 2)
                {
                    Monitor.Pulse(_syncRoot);
                }
                var yamlFile = _queue.Dequeue();
                if (Array.IndexOf(_workingSet, yamlFile) != -1)
                {
                    _queue.Enqueue(yamlFile);
                    if (_queue.Count < ConsumerCount)
                    {
                        Monitor.Wait(_syncRoot);
                    }
                    continue;
                }
                for (int i = 0; i < _workingSet.Length; i++)
                {
                    if (_workingSet[i] == null)
                    {
                        _workingSet[i] = yamlFile;
                        return yamlFile;
                    }
                }
                Environment.FailFast("Bug in consumer!");
            }
        }

        private void PatchYamlFile(string yamlFile, List<UidAndComment> list)
        {
            var path = Path.Combine(_yamlDirectory, yamlFile);
            if (!File.Exists(path))
            {
                Console.WriteLine($"Cannot find file {yamlFile}.");
                return;
            }
            var pageVM = YamlUtility.Deserialize<PageViewModel>(path);
            foreach (var uidAndComment in list)
            {
                var item = pageVM.Items.Find(x => x.Uid == uidAndComment.Uid);
                if (item == null)
                {
                    Console.WriteLine($"Cannot find {uidAndComment.Uid} in file {yamlFile}.");
                    continue;
                }
                Console.WriteLine($"Patching yaml: {yamlFile}");
                PatchViewModel(item, uidAndComment.Comment);
                YamlUtility.Serialize(path, pageVM);
            }
        }

        private void PatchViewModel(ItemViewModel item, string comment)
        {
            var summary = TripleSlashCommentParser.GetSummary(comment, TripleSlashCommentParserContext.Instance);
            if (!string.IsNullOrEmpty(summary))
            {
                item.Summary = summary;
            }
            var remarks = TripleSlashCommentParser.GetRemarks(comment, TripleSlashCommentParserContext.Instance);
            if (!string.IsNullOrEmpty(remarks))
            {
                item.Remarks = remarks;
            }
            var exceptions = TripleSlashCommentParser.GetExceptions(comment, TripleSlashCommentParserContext.Instance);
            if (exceptions != null && exceptions.Count > 0)
            {
                item.Exceptions = exceptions;
            }
            var sees = TripleSlashCommentParser.GetSees(comment, TripleSlashCommentParserContext.Instance);
            if (sees != null && sees.Count > 0)
            {
                item.Sees = sees;
            }
            var seeAlsos = TripleSlashCommentParser.GetSeeAlsos(comment, TripleSlashCommentParserContext.Instance);
            if (seeAlsos != null && seeAlsos.Count > 0)
            {
                item.SeeAlsos = seeAlsos;
            }
            var example = TripleSlashCommentParser.GetExample(comment, TripleSlashCommentParserContext.Instance);
            if (!string.IsNullOrEmpty(example))
            {
                item.Example = example;
            }
            // todo more.
        }

        #region IDisposable

        public void Dispose()
        {
            _consumerCompleted.Dispose();
            _producerCompleted.Dispose();
        }

        #endregion

        #endregion

        #region Nested Class

        internal sealed class UidAndReader
        {
            public string Uid { get; set; }
            public XmlReader Reader { get; set; }
            public UidAndComment ToUidAndElement() => new UidAndComment { Uid = Uid, Comment = Reader.ReadOuterXml() };
        }

        internal sealed class UidAndComment
        {
            public string Uid { get; set; }
            public string Comment { get; set; }
        }

        internal sealed class TripleSlashCommentParserContext : ITripleSlashCommentParserContext
        {
            public static readonly TripleSlashCommentParserContext Instance = new TripleSlashCommentParserContext
            {
                AddReferenceDelegate = s => { },
                Normalize = true,
            };

            public Action<string> AddReferenceDelegate { get; set; }
            public bool Normalize { get; set; }
            public bool PreserveRawInlineComments { get; set; }
        }

        #endregion
    }
}
