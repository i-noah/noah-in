using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Launcher6
{
    public class Launcher
    {
        public async Task<object> StartRhino(dynamic input)
        {
            dynamic rhino = null;
            try
            {
                const string rhino_id = "Rhino.Interface.6";
                var type = Type.GetTypeFromProgID(rhino_id);
                rhino = Activator.CreateInstance(type);
            }
            catch
            {
                // ignored
                return "Failed to create Rhino application";
            }

            if (null == rhino)
            {
                return "Failed to create Rhino application";
            }

            // Wait until Rhino is initialized before calling into it
            const int bail_milliseconds = 15 * 1000;
            var time_waiting = 0;
            while (0 == rhino.IsInitialized())
            {
                Thread.Sleep(100);
                time_waiting += 100;
                if (time_waiting > bail_milliseconds)
                {
                    return "Rhino initialization failed";
                }
            }

            rhino.Visible = 1;

            string launcher = $@"#-*- coding:utf8 -*-
import clr
from System.Reflection import Assembly

ext = Assembly.LoadFrom(r'C:\Users\KaivnD\Desktop\noah-in\NoahiRhino\NoahiRhino\bin\Debug\NoahiRhino.dll')
clr.AddReference(ext)

import json

from NoahiRhino.Noah import LaunchGrasshopper, LoadDoc, AssignDataToDoc

LaunchGrasshopper()
LoadDoc(r'{input.file}')
AssignDataToDoc('{input.data}')
            ";

            string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", "") + ".py");

            File.WriteAllText(tmpPath, launcher);

            rhino.RunScript($"_-RunPythonScript {tmpPath}", 0);

            File.Delete(tmpPath);

            return "Done";
        }
    }
}
