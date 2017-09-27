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

    [Route("xrefs")]
    public class ManageController : Controller
    {
        private XRefSpecDBContext _db;

        public ManageController(XRefSpecDBContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> AddXrefs([FromBody]IEnumerable<XRefSpec> specList)
        {
            if (specList == null)
            {
                return BadRequest();
            }

            _db.XRefSpecObjects.AddRange(specList.Select(s => s.ToXRefSpecObject()));
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("uploads")]
        public async Task<IActionResult> UploadXRefMap([FromBody]UploadXRefMapRequest request)
        {
            var url = request.url;
            if (url == null)
            {
                return BadRequest();
            }

            var tasks = new List<Task>();
            using (var client = new WebClient())
            {
                var result = await client.DownloadStringTaskAsync(new System.Uri(url));
                using (var sr = new StringReader(result))
                {
                    var xm = YamlUtility.Deserialize<XRefMap>(sr);
                    return await AddXrefs(xm?.References);
                }
            }
        }
    }
}