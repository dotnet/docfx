// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: ApplicationPart("Microsoft.Docs.Template")]

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class RazorTemplate
    {
        private static readonly Lazy<TestServer> s_server = new Lazy<TestServer>(StartServer);

        public static async Task<string> Render(string template, object model)
        {
            var httpContext = await s_server.Value.SendAsync(context =>
            {
                context.Items["template"] = template;
                context.Items["model"] = model;
            });

            using var reader = new StreamReader(httpContext.Response.Body);
            var body = reader.ReadToEnd();
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
                    .UseEnvironment(Environments.Production)
                    .ConfigureServices(ConfigureServices)
                    .Configure(Configure));

            void ConfigureServices(IServiceCollection services)
            {
                services.AddMvc().ConfigureApplicationPartManager(parts =>
                        {
                            // Ensure we only have one private TemplateController
                            parts.FeatureProviders.Remove(parts.FeatureProviders.First(fp => fp is IApplicationFeatureProvider<ControllerFeature>));
                            parts.FeatureProviders.Add(new TemplateControllerProvider());
                        });
            }

            void Configure(IApplicationBuilder app)
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapRazorPages();
                    endpoints.MapControllerRoute(
                        name: "content",
                        pattern: "{controller=Template}/{action=Get}");
                });
            }
        }

        private class TemplateControllerProvider : IApplicationFeatureProvider<ControllerFeature>
        {
            public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
            {
                feature.Controllers.Add(typeof(TemplateController).GetTypeInfo());
            }
        }

        private class TemplateController : Controller
        {
            public IActionResult Get()
            {
                var template = (string)HttpContext.Items["template"];
                var model = HttpContext.Items["model"];

                Debug.Assert(template != null);
                Debug.Assert(model != null);

                return View(template, model);
            }
        }
    }
}
