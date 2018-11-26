// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XRefService.Manage.Controllers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Net;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Common;

    using XRefService.Common.Dao;
    using XRefService.Common.Models;
    using XRefService.Common.Utilities;
    using XRefService.Manage.Models;
    using System;

    [Route("xrefs")]
    public class ManageController : Controller
    {
        private XRefSpecDBContext _db;

        public ManageController(XRefSpecDBContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> AddXrefs([FromBody]IList<XRefSpec> specList)
        {
            if (specList == null)
            {
                return BadRequest("Please provide list of xref specs in the request body.");
            }

            _db.XRefSpecObjects.AddRange(specList.Select(s => s.ToXRefSpecObject()));
            await _db.SaveChangesAsync();
            return Ok($"Successfully uploaded {specList.Count} xref specs.");
        }

        [HttpPost("uploads")]
        public IActionResult UploadXRefMap([FromBody]UploadXRefMapRequest request)
        {
            var url = request.url;
            if (string.IsNullOrEmpty(url))
            {
                return BadRequest("Please provide the url of the xrefmap.yml file in request body: { \"url\". \"{url}\"");
            }

            // Start uploading
            // TODO: track status
            Task.Run(() => Upload(url));
            return Accepted();
        }

        private async Task<IActionResult> Upload(string url)
        {
            using (var client = new WebClient())
            {
                using (var stream = await client.OpenReadTaskAsync(url))
                {
                    using (var sr = new StreamReader(stream))
                    {
                        var xm = YamlUtility.Deserialize<XRefMap>(sr);
                        return await AddXrefs(xm?.References);
                    }
                }
            }
        }
    }
}