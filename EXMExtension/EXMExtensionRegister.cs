using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Pipelines.Loader;
using Sitecore.Pipelines;
using Sitecore.StringExtensions;

namespace EXMExtension
{
    public class ExmExtensionRegister : InitializeRoutes
    {
        protected override void RegisterRoutes(RouteCollection routes, PipelineArgs args)
        {
            var controller = LoadController();
            try
            {
                if (controller.IsNullOrEmpty())
                    throw new Exception("The controller is not configured");
                routes.MapRoute(controller, controller+"/{action}/{id}", new
                {
                    controller = controller,
                    action = "Index",
                    id = UrlParameter.Optional
                });
            }
            catch (Exception e)
            {
                Log.Warn(e.Message, this);
            }
            
        }

        private string LoadController()
        {
            var controllerNode = Factory.GetConfigNode("ExmTools/ControllerRegister");
            Log.Debug("The controller value "+controllerNode.InnerText, this);
            return controllerNode.InnerText;
        }

	}
}
