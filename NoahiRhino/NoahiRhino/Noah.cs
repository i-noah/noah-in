using Grasshopper.Plugin;
using RestSharp;
using Rhino;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoahiRhino
{
    public class Noah
    {
        public Noah()
        {
            void LaunchGh()
            {
                if (!(RhinoApp.GetPlugInObject("Grasshopper") is GH_RhinoScriptInterface gh)) return;

                gh.DisableBanner();

                if (!gh.IsEditorLoaded())
                {
                    gh.LoadEditor();
                }
            }
        }
    }
}
