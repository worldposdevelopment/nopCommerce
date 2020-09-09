using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Plugins;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nop.Plugin.Misc.WebAPI
{
    public class WebAPIPlugin : IMiscPlugin
    {
       private readonly IWebHelper _webHelper;

        public PluginDescriptor PluginDescriptor { get; set; }

        public WebAPIPlugin(IWebHelper webHelper)
        {
            _webHelper = webHelper;
        }
        public string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin";
        }

        public void Install()
        {

        }

        public void PreparePluginToUninstall()
        {

        }

        public void Uninstall()
        {
    
        }

        public void Update(string currentVersion, string targetVersion)
        {

        }
    }
}
