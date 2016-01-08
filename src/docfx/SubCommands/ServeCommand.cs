// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.IO;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Owin.StaticFiles;
    using Owin.FileSystems;
    using Owin.Hosting;
    using global::Owin;

    internal sealed class ServeCommand : ISubCommand
    {
        private readonly ServeCommandOptions _options;
        public ServeCommand(ServeCommandOptions options)
        {
            _options = options;
        }

        public void Exec(SubCommandRunningContext context)
        {
            Serve(_options.Folder, _options.Port.HasValue ? _options.Port.Value.ToString() : null);
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
