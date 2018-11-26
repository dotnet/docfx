// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DfmHttpService
{
    using System.Collections.Generic;

    using CommandLine;

    public class Program
    {
        private static readonly Parser Parser = new Parser(s =>
        {
            s.IgnoreUnknownArguments = true;
            s.CaseSensitive = false;
        });

        public static void Main(string[] args)
        {
            var options = new Options();
            Parser.ParseArguments(args, options);

            HandleRequest(options);
        }

        private static void HandleRequest(Options options)
        {
            var handler = new CompositeHandler(
                new List<IHttpHandler>
                {
                    new TestServerAvailableHandler(),
                    new DfmPreviewHandler(options.WorkspacePath, options.IsDfmLatest),
                    new DfmTokenTreeHandler(options.WorkspacePath),
                    new DeleteTempPreviewFileHandler(),
                    new ExitHandler()
                });

            var service = new DfmHttpServer(handler, "localhost", options.Port);
            service.Start();
            service.WaitForExit();
        }
    }
}
