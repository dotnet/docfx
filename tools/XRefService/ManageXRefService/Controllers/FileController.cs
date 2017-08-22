// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ManageXRefRestService.Controllers
{
    using XRefService.Common.Dao;
    using XRefService.Common.Models;
    using XRefService.Common.Utilities;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    [Route("uids")]
    public class FileController : Controller
    {
        private XRefSpecDBContext _db;
        private IHostingEnvironment _environment;

        public FileController(XRefSpecDBContext db, IHostingEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        [HttpPost("add")]
        public IActionResult Create([FromBody]XRefSpec spec)
        {
            if(spec == null)
            {
                return BadRequest();
            }

            _db.XRefSpecObjects.Add(new XRefSpecObject
            {
                HashedUid = MD5Encryption.CalculateMD5Hash(spec.Uid),
                Uid = spec.Uid,
                XRefSpecJson = JsonUtility.Serialize(spec)
            });
            _db.SaveChanges();
            return Ok();
        }

        [HttpPost("adds")]
        public IActionResult CreateList([FromBody]XRefSpec[] specList)
        {
            if (specList == null || specList.Length == 0)
            {
                return BadRequest();
            }

            foreach (var spec in specList)
            {
                _db.XRefSpecObjects.Add(new XRefSpecObject
                {
                    HashedUid = MD5Encryption.CalculateMD5Hash(spec.Uid),
                    Uid = spec.Uid,
                    XRefSpecJson = JsonUtility.Serialize(spec)
                });
            }
            _db.SaveChanges();
            return Ok();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(ICollection<IFormFile> files)
        {
            var tasks = new List<Task>();
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    tasks.Add(SaveDataAsync(file));
                }
            }
            await Task.WhenAll(tasks);
            return StatusCode(200, "upload successfully");
        }

        private Task SaveDataAsync(IFormFile file)
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                XRefMap xrefMap = YamlUtility.Deserialize<XRefMap>(reader);
                foreach (var spec in xrefMap.References)
                {
                    XRefSpecObject xrefSpec = new XRefSpecObject();
                    xrefSpec.Uid = spec["uid"];
                    xrefSpec.HashedUid = MD5Encryption.CalculateMD5Hash(xrefSpec.Uid);
                    xrefSpec.XRefSpecJson = JsonUtility.Serialize(spec);
                    _db.XRefSpecObjects.Add(xrefSpec);
                }
                return _db.SaveChangesAsync();
            }
        }

    }
}