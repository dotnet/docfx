using DB.Models;
using Microsoft.DocAsCode.Build.Engine;
using OperationOnDB.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace OperationOnDB.Controllers
{
    [ApiBasicAuthenticationAttribute("user", "user",
    BasicRealm = "localhost")]
    [RoutePrefix("uids")]
    public class ApiFileController : ApiController
    {
        private dataEntities db = new dataEntities();


        [HttpPost]
        [Route("add")]
        public string UploadFiles()
        {
            int iUploadedCnt = 0;
            string path = System.Web.Hosting.HostingEnvironment.MapPath("~/uploads");

            System.Web.HttpFileCollection hfc = System.Web.HttpContext.Current.Request.Files;

            for(int iCnt = 0; iCnt < hfc.Count; iCnt++)
            {
                System.Web.HttpPostedFile hpf = hfc[iCnt];
                if(hpf.ContentLength > 0)
                {
                    string fullPath = Path.Combine(path, Path.GetFileName(hpf.FileName));
                    if (!File.Exists(fullPath))
                    {
                        hpf.SaveAs(fullPath);
                        SaveData(fullPath);
                        iUploadedCnt++;
                        continue;
                    }
                    return Path.GetFileName(hpf.FileName) + " does not exists";
                }
            }

            if(iUploadedCnt > 0)
            {
                return iUploadedCnt + " Files Uploaded Successfully";
            }
            else
            {
                return "Uploaded Failed";
            }
        }

        private void SaveData(string path)
        {

            XRefMap xref = Microsoft.DocAsCode.Common.YamlUtility.Deserialize<XRefMap>(path);
            foreach (var spec in xref.References)
            {
                uidt t = new uidt();
                t.uid = spec["uid"];
                t.objectStr = Newtonsoft.Json.JsonConvert.SerializeObject(spec);
                db.uidts.Add(t);
                db.SaveChanges();
            }
        }

    }
}
