using System.Web.Mvc;

namespace puck.Areas.admin
{
    public class adminAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "admin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "admin_default",
                "admin/{controller}/{action}/{id}",
                new {controller="api", action = "Index", id = UrlParameter.Optional }
                ,namespaces:new string[]{"puck.core.Controllers"}
            );
        }
    }
}
