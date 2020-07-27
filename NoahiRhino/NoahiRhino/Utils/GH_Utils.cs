
using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Plugin;
using Rhino;
using System.Text;
using System.Threading.Tasks;

namespace NoahiRhino.Utils
{
    public static class GH_Utils
    {
        public static void RunHeadless()
        {
            if (!(RhinoApp.GetPlugInObject("Grasshopper") is GH_RhinoScriptInterface gh)) return;

            if (gh.IsEditorLoaded()) return;

            gh.DisableBanner();
            gh.LoadEditor();

            Instances.AutoHideBanner = false;
            Instances.AutoShowBanner = false;
            Instances.ComponentServer.DestroyLoadingUI();
        }

        public static GH_Document Compute(string file, GH_Document.SolutionEndEventHandler action = null, Dictionary<string, IList<object>> allData = null)
        {
            GH_Document doc = null;
            GH_DocumentServer server = Instances.DocumentServer;

            if (server == null) throw new Exception("读取文档失败");

            doc = server.ToList().Find(it => it.Properties.ProjectFileName == file);

            if (doc == null)
            {
                var io = new GH_DocumentIO();
                if (!io.Open(file)) throw new Exception("读取文档失败");
                doc = io.Document;

                doc.Properties.ProjectFileName = file;

                server.AddDocument(doc);
            }

            GH_Canvas activeCanvas = Instances.ActiveCanvas;
            if (activeCanvas == null) throw new Exception("读取文档失败");

            activeCanvas.Document = doc;
            activeCanvas.Document.IsModified = false;
            activeCanvas.Refresh();

            if (allData != null)
            {
                foreach (var hook in doc.ClusterInputHooks())
                {
                    string key = hook.NickName;
                    if (string.IsNullOrEmpty(key)) continue;

                    if (!allData.TryGetValue(key, out IList<object> val) || val == null) continue;

                    var value = SimpleDataStructure(val);

                    hook.ClearPlaceholderData();
                    hook.SetPlaceholderData(value);
                    hook.ExpireSolution(false);
                }
            }

            if (action != null)
            {
                doc.SolutionEnd -= action;
                doc.SolutionEnd += action;
            }

            doc.Enabled = true;
            doc.NewSolution(true);

            return doc;
        }

        private static GH_Structure<IGH_Goo> SimpleDataStructure(IList<object> value)
        {
            GH_Structure<IGH_Goo> m_data = new GH_Structure<IGH_Goo>();
            int i = 0;
            foreach (var val in value)
            {
                GH_Number castNumber = null;
                GH_String castString = null;
                GH_Curve castCurve = null;
                GH_Brep castBrep = null;
                GH_Vector castVector = null;
                GH_Mesh castMesh = null;
                GH_Point castPoint = null;

                if (val is Guid id)
                {
                    m_data.Append(new GH_ObjectWrapper(new GH_Guid(id)));
                }
                else if (val is List<object> list)
                {
                    foreach (var tmp in list)
                    {
                        m_data.Append(new GH_ObjectWrapper(tmp), new GH_Path(i));
                    }
                }
                else if (GH_Convert.ToGHPoint_Primary(val, ref castPoint))
                {
                    m_data.Append(new GH_ObjectWrapper(castPoint));
                }
                else if (GH_Convert.ToGHCurve(val, GH_Conversion.Both, ref castCurve))
                {
                    m_data.Append(new GH_ObjectWrapper(castCurve));
                }
                else if (GH_Convert.ToGHBrep_Primary(val, ref castBrep))
                {
                    m_data.Append(new GH_ObjectWrapper(castBrep));
                }
                else if (GH_Convert.ToGHMesh_Primary(val, ref castMesh))
                {
                    m_data.Append(new GH_ObjectWrapper(castMesh));
                }
                else if (GH_Convert.ToGHVector_Primary(val, ref castVector))
                {
                    m_data.Append(new GH_ObjectWrapper(castVector));
                }
                else if (GH_Convert.ToGHNumber(val, GH_Conversion.Both, ref castNumber))
                {
                    m_data.Append(new GH_ObjectWrapper(castNumber));
                }
                else if (GH_Convert.ToGHString(val, GH_Conversion.Both, ref castString))
                {
                    m_data.Append(new GH_ObjectWrapper(castString));
                }
                else
                {
                    m_data.Append((IGH_Goo)val);
                }
                ++i;
            }

            return m_data;
        }

        public static Dictionary<string, List<IGH_Goo>> DocStoreOutput(GH_Document _doc)
        {
            var resultDict = new Dictionary<string, List<IGH_Goo>>();
            var hooks = _doc.ClusterOutputHooks();
            if (hooks.Length < 1) throw new Exception("导出节点缺失");

            foreach (var hook in hooks)
            {
                string name = hook.NickName;
                if (string.IsNullOrEmpty(name)) continue;
                var volaData = hook.VolatileData;
                if (volaData.IsEmpty) continue;
                var allData = volaData.AllData(true);

                List<IGH_Goo> results = new List<IGH_Goo>();

                foreach (var data in allData)
                {
                    results.Add(data);
                }

                resultDict.Add(name, results);
            }
            return resultDict;
        }
    }
}
