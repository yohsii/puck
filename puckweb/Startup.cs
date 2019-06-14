using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(puckweb.Startup))]
namespace puckweb
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
