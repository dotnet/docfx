// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace XRefService.Get.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using System.Data.Entity;

    using Microsoft.AspNetCore.Mvc;

    using XRefService.Common.Dao;
    using XRefService.Common.Utilities;

    [Route("xrefs")]
    public class GetController : Controller
    {
        private readonly XRefSpecDBContext _db;

        public GetController(XRefSpecDBContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Route("{uid}")]
        public async Task<IActionResult> GetByUid(string uid)
        {
            string hashedUid = MD5Encryption.CalculateMD5Hash(uid);
            var ut = await (from o in _db.XRefSpecObjects
                            where o.HashedUid == hashedUid
                            orderby o.Uid
                            select o.XRefSpecJson).ToListAsync();
            
            return StatusCode(200, "[" + string.Join(",", ut) + "]");
        }

        [HttpGet]
        public async Task<IActionResult> GetByPattern([FromQuery]string pattern, [FromQuery]int count = 100)
        {
            var xrefs = await (from o in _db.XRefSpecObjects
                               where o.Uid.Contains(pattern)
                               orderby o.Uid
                               select o.XRefSpecJson).Take(count).ToListAsync();

            return StatusCode(200, "[" + string.Join(",", xrefs) + "]");
        }
    }
}

