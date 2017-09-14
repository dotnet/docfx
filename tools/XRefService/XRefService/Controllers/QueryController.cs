// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace RestfulApiService.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Data.Entity;

    using XRefService.Common.Dao;
    using XRefService.Common.Utilities;
    using XRefService.Common.Models;

    using Microsoft.AspNetCore.Mvc;
    
    using XRefService.Models;
    using XRefService.Utils;

    [Route("uids")]
    public class QueryController : Controller
    {
        private readonly XRefSpecDBContext _db;
        private readonly Func<XRefSpecObject, SimXRefSpec> sim = xrf => 
        {
            var sx = new SimXRefSpec();
            sx.Create(xrf);
            return sx;
        };

        public QueryController(XRefSpecDBContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Route("{uid}")]
        public async Task<IActionResult> GetByUid(string uid)
        {
            string hashedUid = MD5Encryption.CalculateMD5Hash(uid);
            var ut = await _db.XRefSpecObjects.Where(b => b.HashedUid == hashedUid)
                               .Select(c => c.XRefSpecJson)
                               .ToListAsync();
            
            return StatusCode(200, "[" + string.Join(",", ut) + "]");
        }

        [HttpGet]
        [Route("~/intellisense/{uid}")]
        public async Task<IActionResult> GetByUidForExtension(string uid)
        {
            //sql: select top 20 xrefspec from table where xrefspec.contain uid
            var ut = await _db.XRefSpecObjects.Where(b => b.Uid.Contains(uid))
                                .Select(sim)
                                .Take(20).OrderBy(b => b.Uid)
                                .AsAsyncQueryable<SimXRefSpec>()
                                .ToListAsync();

            return StatusCode(200, ut);
        }

    }
}

