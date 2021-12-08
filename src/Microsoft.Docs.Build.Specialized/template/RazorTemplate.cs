// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.TestHost;

[assembly: ApplicationPart("Microsoft.Docs.Build.Specialized")]

namespace Microsoft.Docs.Build;

public class RazorTemplate
{
    private static readonly Lazy<TestServer> s_server = new(StartServer);

    public static async Task<string> Render(string? template, object model)
    {
        var httpContext = await s_server.Value.SendAsync(context =>
        {
            context.Items["template"] = template;
            context.Items["model"] = model;
        });

        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();
        var statusCode = httpContext.Response.StatusCode;

        if (statusCode != 200)
        {
            var message = $"Render '{template}' failed with status code {statusCode}:\n{body}";
            throw new InvalidOperationException(message);
        }

        return body.Replace("\r", "");
    }

    private static TestServer StartServer()
    {
        return new TestServer(
            new WebHostBuilder()
                .UseEnvironment(Environments.Production)
                .ConfigureServices(ConfigureServices)
                .Configure(Configure));

        static void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().ConfigureApplicationPartManager(parts =>
            {
                // Ensure we only have one private TemplateController
                parts.FeatureProviders.Remove(parts.FeatureProviders.First(fp => fp is IApplicationFeatureProvider<ControllerFeature>));
                parts.FeatureProviders.Add(new TemplateControllerProvider());
            });
        }

        static void Configure(IApplicationBuilder app)
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
            var template = HttpContext.Items["template"] as string;
            var model = HttpContext.Items["model"];

            return View(template, model);
        }
    }
}
