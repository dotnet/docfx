// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Docs.Build
{
    internal class Template
    {
        private static readonly Lazy<TestServer> s_server = new Lazy<TestServer>(StartServer);

        public static Task<string> Render<T>(T model) => Render(typeof(T).Name, model);

        public static async Task<string> Render(string template, object model)
        {
            var httpContext = await s_server.Value.SendAsync(context =>
            {
                context.Items["template"] = template;
                context.Items["model"] = model;
            });

            var body = new StreamReader(httpContext.Response.Body).ReadToEnd();
            var statusCode = httpContext.Response.StatusCode;

            if (statusCode != 200)
            {
                var message = $"Render '{template}' failed with status code {statusCode}:\n{body}";
                throw new InvalidOperationException(message);
            }

            return body;
        }

        private static TestServer StartServer()
        {
            return new TestServer(
                new WebHostBuilder()
                    .ConfigureServices(ConfigureServices)
                    .Configure(Configure));

            void ConfigureServices(IServiceCollection services)
            {
                services.AddMvc();
            }

            void Configure(IApplicationBuilder app)
            {
                app.UseMvc(
                    routes => routes.MapRoute(
                        name: "content",
                        template: "{*url}",
                        defaults: new { controller = "Template", action = "Get" }));
            }
        }
    }
}
