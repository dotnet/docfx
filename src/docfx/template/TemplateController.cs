// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Docs.Build
{
    public class TemplateController : Controller
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
