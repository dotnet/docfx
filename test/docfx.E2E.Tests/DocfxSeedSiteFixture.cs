// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.E2E.Tests
{
    using System;
    using System.IO;

    using global::Owin;
    using Microsoft.Owin.FileSystems;
    using Microsoft.Owin.Hosting;
    using Microsoft.Owin.StaticFiles;
    using Microsoft.Owin.StaticFiles.ContentTypes;
    using Newtonsoft.Json.Linq;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Firefox;

    public class DocfxSeedSiteFixture : IDisposable
    {
        private const string ConfigFile = "config.E2E.Tests.json";
        private const string RootUrl = "http://localhost";

        public string Url { get; }
        public IWebDriver Driver { get; }

        public DocfxSeedSiteFixture()
        {
            JObject token = JObject.Parse(File.ReadAllText(ConfigFile));
            var folder = (string)token.SelectToken("site");
            var port = (int)token.SelectToken("port");
            Url = $"{RootUrl}:{port}";

            Driver = new FirefoxDriver();
            Driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(10));
            Driver.Manage().Window.Maximize();

            try
            {
                var contentTypeProvider = new FileExtensionContentTypeProvider();
                // register yaml MIME as OWIN doesn't host it by default.
                // http://stackoverflow.com/questions/332129/yaml-mime-type
                contentTypeProvider.Mappings[".yml"] = "application/x-yaml";
                var fileServerOptions = new FileServerOptions
                {
                    EnableDirectoryBrowsing = true,
                    FileSystem = new PhysicalFileSystem(folder),
                    StaticFileOptions =
                    {
                        ContentTypeProvider = contentTypeProvider
                    }
                };
                WebApp.Start(Url, builder => builder.UseFileServer(fileServerOptions));
            }
            catch (System.Reflection.TargetInvocationException)
            {
                Console.WriteLine($"Error serving \"{folder}\" on \"{Url}\", check if the port is already being in use.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error serving \"{folder}\" on \"{Url}\": {e}");
            }
        }

        public void Dispose()
        {
            Driver.Quit();
        }
    }

}
