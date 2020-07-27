using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Grasshopper.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Rhino;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NoahiRhino
{
    public static class Noah
    {
        public static void LaunchGrasshopper()
        {
            if (!(RhinoApp.GetPlugInObject("Grasshopper") is GH_RhinoScriptInterface gh)) return;
            
            if (gh.IsEditorLoaded()) return;

            gh.DisableBanner();
            gh.LoadEditor();

            Instances.AutoHideBanner = false;
            Instances.AutoShowBanner = false;
            Instances.ComponentServer.DestroyLoadingUI();
        }

        public static NoahClient GetNoahClientInstance()
        {
            var doc = RhinoDoc.ActiveDoc;
            if (!doc.IsAvailable) throw new Exception("Rhino active Document is not available!");

            if (!doc.RuntimeData.TryGetValue("NoahClient", out object data)) throw new Exception("Cannot get NoahClient.");

            if (!(data is NoahClient client)) throw new Exception("RuntimeData is not NoahClient.");

            return client;
        }

        public static void LoadDoc(string file)
        {
            var io = new GH_DocumentIO();
            if (!io.Open(file)) throw new Exception("读取文档失败");

            GH_DocumentServer server = Instances.DocumentServer;

            var doc = io.Document;

            if (server == null) throw new Exception("读取文档失败");

            server.AddDocument(doc);

            GH_Canvas activeCanvas = Instances.ActiveCanvas;
            if (activeCanvas == null) throw new Exception("读取文档失败");

            activeCanvas.Document = doc;
            activeCanvas.Document.IsModified = false;
            activeCanvas.Refresh();
        }

        public static void RunDll(string file, string json)
        {
            if (string.IsNullOrEmpty(file)) throw new Exception("没有指定程序文件");
            if (!System.IO.File.Exists(file)) throw new Exception("指定程序文件不存在");
            var ext = System.IO.Path.GetExtension(file);
            JObject dataSet = JsonConvert.DeserializeObject<JObject>(json);
            var dataGroup = new Dictionary<string, string>();

            foreach (var data in dataSet["data"])
            {
                if (!(data is JProperty prop)) continue;
                dataGroup.Add(prop.Name, prop.Value.ToString());
            }

            // 参数类型转换
            var parameters = JArray.Parse(dataSet["param"].ToString());

            switch (ext)
            {
                case ".dll":
                    {
                        string name = System.IO.Path.GetFileNameWithoutExtension(file);
                        Assembly assem = Assembly.LoadFrom(file);
                        var type = assem.GetType($"{name}.Program", true, true);
                        var res = type.GetMethod("Main").Invoke(null, new object[] { new object[] { parameters.ToObject<List<object>>(), dataGroup } });
                        // TODO 回收结果
                        break;
                    }
                default:
                    {
                        throw new Exception($"不支持的程序类型{ext}");
                    }
            }
        }

        public static void AssignDataToDoc(string dataSetJson)
        {
            JObject dataSet = JsonConvert.DeserializeObject<JObject>(dataSetJson);
            GH_Canvas activeCanvas = Instances.ActiveCanvas;
            if (activeCanvas == null) throw new Exception("读取文档失败");

            GH_Document doc = activeCanvas.Document;

            if (doc == null) return;

            var hooks = doc.ClusterInputHooks();

            foreach (var hook in hooks)
            {
                GH_Structure<IGH_Goo> m_data;

                string key = hook.NickName;

                if (string.IsNullOrEmpty(key)) key = hook.Name;
                if (string.IsNullOrEmpty(key)) key = hook.CustomName;
                if (string.IsNullOrEmpty(key)) key = hook.CustomNickName;

                if (!key.StartsWith("@", StringComparison.Ordinal)) continue;

                key = key.Substring(1);

                if (!dataSet.TryGetValue(key, out var data)) continue;

                m_data = SingleDataStructrue(data);

                hook.ClearPlaceholderData();
                hook.SetPlaceholderData(m_data);
                //hook.ExpireSolution(true);
            }

            // for data placeholder inside cluster (deep = 1)

            var clusters = new List<GH_Cluster>();
            foreach (var obj in doc.Objects)
            {
                if (!(obj is GH_Cluster cluster)) continue;
                clusters.Add(cluster);
            }

            if (clusters.Count == 0) return;


            foreach (var cluster in clusters)
            {
                foreach (var obj in cluster.Document("").Objects)
                {
                    if (!(obj is IGH_Param param)) continue;

                    string nickname = param.NickName;

                    if (string.IsNullOrEmpty(nickname)) nickname = param.Name;
                    if (!nickname.StartsWith("@", StringComparison.Ordinal)) continue;
                    nickname = nickname.Substring(1);
                    if (!dataSet.TryGetValue(nickname, out var data)) continue;
                                       

                    Utility.InvokeMethod(param, "Script_ClearPersistentData");
                    Utility.InvokeMethod(param, "Script_AddPersistentData", new List<object>() { data });

                    //param.ExpireSolution(true);
                    //cluster.ExpireSolution(true);
                }
            }

            doc.NewSolution(true);

            activeCanvas.Document.IsModified = false;
            activeCanvas.Refresh();

            GH_Structure<IGH_Goo> SingleDataStructrue(object value)
            {

                GH_Structure<IGH_Goo> m_data = new GH_Structure<IGH_Goo>();

                GH_Number castNumber = null;
                GH_String castString = null;
                GH_Curve castCurve = null;
                if (GH_Convert.ToGHCurve(value, GH_Conversion.Both, ref castCurve))
                {
                    m_data.Append(new GH_ObjectWrapper(castCurve));
                }
                else if (GH_Convert.ToGHNumber(value, GH_Conversion.Both, ref castNumber))
                {
                    m_data.Append(new GH_ObjectWrapper(castNumber));
                }
                else if (GH_Convert.ToGHString(value, GH_Conversion.Both, ref castString))
                {
                    m_data.Append(new GH_ObjectWrapper(castString));
                }
                else
                {
                    m_data.Append((IGH_Goo)value);
                }

                return m_data;
            }
        }
    }
}
