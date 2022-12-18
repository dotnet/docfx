// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.IO;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    internal sealed class ServeCommand : ISubCommand
    {
        private readonly ServeCommandOptions _options;
        public bool AllowReplay => false;

        public string Name { get; } = nameof(ServeCommand);

        public ServeCommand(ServeCommandOptions options)
        {
            _options = options;
        }

        public void Exec(SubCommandRunningContext context)
        {
            Serve(_options.Folder,
                _options.Host,
                _options.Port.HasValue ? _options.Port.Value.ToString() : null);
        }

        public static void Serve(string folder, string host, string port)
        {
            if (string.IsNullOrEmpty(folder)) folder = Directory.GetCurrentDirectory();
            folder = Path.GetFullPath(folder);
            host = string.IsNullOrWhiteSpace(host) ? "localhost" : host;
            port = string.IsNullOrWhiteSpace(port) ? "8080" : port;
            var url = $"http://{host}:{port}";
            if (!Directory.Exists(folder))
            {
                throw new ArgumentException("Site folder does not exist. You may need to build it first. Example: \"docfx docfx_project/docfx.json\"", nameof(folder));
            }
            
            var fileServerOptions = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                FileProvider = new PhysicalFileProvider(folder),
            };

            // Fix the issue that .JSON file is 404 when running docfx serve
            fileServerOptions.StaticFileOptions.ServeUnknownFileTypes = true;

            if (!File.Exists(Path.Combine(folder, "index.html")) && File.Exists(Path.Combine(folder, "toc.html")))
            {
                File.Copy(Path.Combine(folder, "toc.html"), Path.Combine(folder, "index.html"));
            }

            try
            {
                var builder = WebApplication.CreateBuilder();
                builder.WebHost
                    .ConfigureLogging(options => options.ClearProviders())
                    .UseUrls(url);

                using var app = builder.Build();
                app.UseFileServer(fileServerOptions);
                app.Start();
                
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
