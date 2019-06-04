using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebApplication1
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute("display", "display/{str}/{num}",
            defaults: new { controller = "WebSimulator", action = "decide" });

            routes.MapRoute("displayForSec", "display/{ip}/{port}/{sec}",
            defaults: new { controller = "WebSimulator", action = "displayPosPerSec" });

            routes.MapRoute("saveToFile", "save/{ip}/{port}/{perSecond}/{NumSeconds}/{filename}",
            defaults: new { controller = "WebSimulator", action = "saveFlightData" });

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "WebSimulator", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
