using puck.core.Abstract;
using puck.core.Helpers;
using puck.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace puck.Controllers
{
    public class SignupController : puck.core.Controllers.BaseController
    {
        //
        // GET: /Signup/
        I_Puck_Repository r;
        public SignupController(I_Puck_Repository r)
        {
            this.r = r;
        }
        public ActionResult Index()
        {
            //example of how to get current node
            var currentNode = QueryHelper<Page>.Current();            
            //example of how to get current revisions based on url
            var currentRevisions = 
                r.CurrentRevisionsByPath(QueryHelper<Page>.PathPrefix() + Request.Url.AbsolutePath.ToLower()).ToList();
            //return control back to puck for routing
            return base.Puck();
        }

    }
}
