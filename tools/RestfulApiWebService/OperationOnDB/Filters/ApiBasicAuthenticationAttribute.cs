using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace OperationOnDB.Filters
{
    public class ApiBasicAuthenticationAttribute: ActionFilterAttribute
    {
        public string BasicRealm { get; set; }
        protected string Username { get; set; }
        protected string Password { get; set; }

        public ApiBasicAuthenticationAttribute(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
          
                
            
            if(actionContext.Request.Headers.Authorization != null)
            {
                string authToken = actionContext.Request.Headers.Authorization.Parameter;
                string decodedToken = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(authToken));
                string username = decodedToken.Substring(0, decodedToken.IndexOf(":"));
                string password = decodedToken.Substring(decodedToken.IndexOf(":") + 1);
                if (username == Username && password == Password) return;
            }

            actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            actionContext.Response.Headers.Add("WWW-Authenticate", String.Format("Basic realm=\"{0}\"", BasicRealm ?? "Ryadel"));
            //var req = filterContext.Request;
            //var auth = req.Headers["Authorization"];
            //if (!String.IsNullOrEmpty(auth))
            //{
            //    var cred = System.Text.ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(auth.Substring(6))).Split(':');
            //    var user = new { Name = cred[0], Pass = cred[1] };
            //    if (user.Name == Username && user.Pass == Password) return;
            //}
            //var res = filterContext.HttpContext.Response;
            //res.AddHeader("WWW-Authenticate", String.Format("Basic realm=\"{0}\"", BasicRealm ?? "Ryadel"));
            ////res.End();
            //filterContext.Result = new HttpUnauthorizedResult();
        }

    }
}