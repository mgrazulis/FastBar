using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(FastBar.Startup))]
namespace FastBar
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
