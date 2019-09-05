using System.Web.Mvc;

namespace puck.Areas.puck
{
    public class puckAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "puck";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "puck_default",
                "puck/{controller}/{action}/{id}",
                new {controller="api", action = "Index", id = UrlParameter.Optional }
                ,namespaces:new string[]{"puck.core.Controllers"}
            );
        }
    }
}
