// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using global::Owin;
    using Microsoft.DocAsCode.EntityModel;
    using Owin.FileSystems;
    using Owin.Hosting;
    using Owin.StaticFiles;
    using System;
    using System.IO;

    internal class ServeCommand : ICommand
    {
        public ServeCommandOptions _options { get; }
        public Options _rootOptions { get; }
        public ServeCommand(Options options, CommandContext context)
        {
            _options = options.ServeCommand;
            _rootOptions = options;
        }

        public ParseResult Exec(RunningContext context)
        {
            Serve(_options.Folder, _options.Port.HasValue ? _options.Port.Value.ToString() : null);
            return ParseResult.SuccessResult;
        }

        public static void Serve(string folder, string port)
        {
            if (string.IsNullOrEmpty(folder)) folder = Environment.CurrentDirectory;
            folder = Path.GetFullPath(folder);
            port = string.IsNullOrWhiteSpace(port) ? "8080" : port;
            var url = $"http://localhost:{port}";
            var fileServerOptions = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                FileSystem = new PhysicalFileSystem(folder),
            };

            if (!File.Exists(Path.Combine(folder, "index.html")) && File.Exists(Path.Combine(folder, "toc.html")))
            {
                File.Copy(Path.Combine(folder, "toc.html"), Path.Combine(folder, "index.html"));
            }

            try
            {
                WebApp.Start(url, builder => builder.UseFileServer(fileServerOptions));

                Console.WriteLine($"Serving \"{folder}\" on {url}");
                Console.ReadLine();
            }
            catch (System.Reflection.TargetInvocationException)
            {
                Logger.LogError($"Error serving \"{folder}\" on {url}, check if the port is already being in use.");
            }
        }
    }
}
