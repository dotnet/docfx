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

            Driver = new FirefoxDriver();
            Driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(10));
            Driver.Manage().Window.Maximize();

            try
            {
                var fileServerOptions = new FileServerOptions
                {
                    EnableDirectoryBrowsing = true,
                    FileSystem = new PhysicalFileSystem(folder),
                };
                Url = $"{RootUrl}:{port}";
                WebApp.Start(Url, builder => builder.UseFileServer(fileServerOptions));
            }
            catch (System.Reflection.TargetInvocationException)
            {
                Console.WriteLine($"Error serving \"{folder}\" on \"{Url}\", check if the port is already being in use.");
            }

        }

        public void Dispose()
        {
            Driver.Quit();
        }
    }

}
