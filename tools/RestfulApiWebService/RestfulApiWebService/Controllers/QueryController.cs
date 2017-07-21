using Microsoft.DocAsCode.Build.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Data.Entity;

using System.Web.Script.Serialization;
using Microsoft.DocAsCode.Plugins;
using DB.Models;
using System.Diagnostics;

namespace RestfulApiService.Controllers
{
    [RoutePrefix("uids")]
    public class QueryController : ApiController
    {
        private dataEntities db = new dataEntities();

        [HttpGet]
        [Route("{uid}")]
        public async Task<IHttpActionResult> GetByUid(string uid)
        {
            var ut = await db.uidts.Where(b => b.uid == uid)
                                .Select(c => c.objectStr)
                                .ToListAsync();
           
            List<XRefSpec> xf = Newtonsoft.Json.JsonConvert.DeserializeObject<List<XRefSpec>>("[" + string.Join(",", ut) + "]");
            return Ok(xf);
        }
           
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> PostByUids([FromBody]string[] uids)
        {
            //Stopwatch stopWatch = new Stopwatch();
            
            List<XRefSpec> xfs = new List<XRefSpec>();
            var getUidTasks = new List<Task<uidt>>();
            //stopWatch.Start();
            foreach (string uid in uids)
            {
                dataEntities tempDB = new dataEntities();
                getUidTasks.Add(tempDB.uidts.Where(b => b.uid == uid)
                               .Select(c => c)
                               .FirstOrDefaultAsync());
            }

            var uidList = await Task.WhenAll(getUidTasks);
            foreach(var temp in uidList)
            {
                XRefSpec xf = null;
                if (temp != null) xf = Newtonsoft.Json.JsonConvert.DeserializeObject<XRefSpec>(temp.objectStr);
                xfs.Add(xf);
            }
            //stopWatch.Stop();
            //TimeSpan ts = stopWatch.Elapsed;
            //string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            //ts.Hours, ts.Minutes, ts.Seconds,
            //ts.Milliseconds / 10);
            //Console.WriteLine("RunTime " + elapsedTime);

            return Ok(xfs);
        }

    }
}
