using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Eto.Forms;
using Rhino;
using Rhino.UI;
using System;
using System.Threading.Tasks;
using WebSocketSharp;
using NoahiRhino.Utils;
using System.Reflection;
using System.Collections.Generic;
using NoahiRhino.UI;
using Rhino.Runtime;
using Rhino.Geometry;
using Gacore.Core;
using SIO = System.IO;
using YamlDotNet.Serialization;
using Gacore.Utils;

namespace NoahiRhino
{
    public class NoahClient
    {
        internal int Port;
        internal Guid Guid;
        private WebSocket Client;
        private readonly ViewportMonitor vm = new ViewportMonitor();
        private int RetryCnt = 0;
        private readonly int MaxRetry = 5;

        public NoahClient(int port)
        {
            Port = port;
            Guid = Guid.NewGuid();

            Init();
        }

        public void Connect()
        {
            Client.Connect();
        }

        public void Close()
        {
            Client.Close(CloseStatusCode.Normal, "");
        }

        private void Init()
        {
            Client = new WebSocket("ws://localhost:" + Port.ToString() + "/data/server/?platform=rhino6&id=" + Guid.ToString());

            Client.OnMessage += Socket_OnMessage;
            Client.OnError += Socket_OnError;
            Client.OnOpen += Socket_OnOpen;
            Client.OnClose += Socket_OnClose;
        }

        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            RhinoApp.WriteLine("Noah Client connecting is closed");
            if (RetryCnt == MaxRetry) Exit();
            Reconnect();
        }

        public async void Exit()
        {
            RhinoApp.WriteLine("_(:_」∠)_ Could not connect to Noah Client, Rhino will exit in 3 second.");
            await Task.Delay(300);
                                 
            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                DialogResult dialogResult = MessageBox.Show(
                    RhinoEtoApp.MainWindow,
                    "Noah Server 已断线并且重联5次都失败了，是否关闭Rhino",
                    "重联失败",
                    MessageBoxButtons.YesNo,
                    MessageBoxType.Error);
                if (dialogResult == DialogResult.Yes)
                {
                    RhinoApp.Exit();
                }
            }));
        }

        public async void Reconnect()
        {
            while (RetryCnt < MaxRetry)
            {
                ++RetryCnt;

                RhinoApp.WriteLine("Retrying to connect Noah Client " + RetryCnt + " times.");
                Connect();

                if (Client.ReadyState == WebSocketState.Open) return;

                await Task.Delay(100);
            }
        }

        private void Socket_OnOpen(object sender, EventArgs e)
        {
            RetryCnt = 0;
        }

        private void Socket_OnError(object sender, ErrorEventArgs e)
        {
            RhinoApp.WriteLine(e.Message);
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            RhinoApp.WriteLine(e.Data);
            try
            {
                ClientEventArgs eve = JsonConvert.DeserializeObject<ClientEventArgs>(e.Data);
                switch (eve.route)
                {
                    case ClientEventType.TaskGetInput:
                        {
                            string type = eve.data["geoType"].ToString();
                            string pick = null;
                            switch(type)
                            {
                                case "Point":
                                    {
                                        pick = "pt";
                                        break;
                                    }
                                case "Curve":
                                    {
                                        pick = "crv";
                                        break;
                                    }
                                case "Surface":
                                    {
                                        pick = "srf";
                                        break;
                                    }
                                case "Brep":
                                    {
                                        pick = "brp";
                                        break;
                                    }
                                case "ID":
                                    {
                                        pick = "id";
                                        break;
                                    }
                            }

                            RhinoApp.InvokeOnUiThread(new Action(() =>
                            {
                                var res = GH_Utils.PickFunction(pick);

                                var data = JsonConvert.SerializeObject(new JObject
                                {
                                    ["route"] = "TaskSetInput",
                                    ["id"] = eve.data["paramId"],
                                    ["data"] = ""
                                });

                                Client.Send(data);
                            }));

                            break;
                        }
                    case ClientEventType.TaskProcess:
                        {
                            if (!vm.Enabled) vm.Enabled = true;

                            vm.Geometries.Clear();

                            string file = eve.data["file"].ToString();
                            if (string.IsNullOrEmpty(file)) throw new Exception("没有指定程序文件");
                            if (!System.IO.File.Exists(file)) throw new Exception("指定程序文件不存在");
                            var ext = System.IO.Path.GetExtension(file);
                            
                            var dataGroup = new Dictionary<string, string>();

                            foreach(var data in eve.data["data"])
                            {
                                if (!(data is JProperty prop)) continue;
                                dataGroup.Add(prop.Name, prop.Value.ToString());
                            }

                            // 参数类型转换
                            var param = eve.data["params"].ToString();
                            JArray paramArray = JArray.Parse(param);
                            var parameters = new List<List<object>>();
                            foreach (var p in paramArray)
                            {
                                if (!p.HasValues) continue;
                                string type = p["type"].ToString();
                                JArray valueArray = JArray.Parse(p["value"].ToString());

                                var values = new List<object>();

                                foreach(var val in valueArray)
                                {
                                    var obj = IO.DecodeCommonObjectFromBase64(val.ToString());
                                    if (obj == null) continue;
                                    vm.Geometries.Add(obj as GeometryBase);
                                    values.Add(obj as object);
                                }
                                
                                if (values.Count < 1) continue;

                                parameters.Add(values);
                            }

                            switch (ext)
                            {
                                case ".dll":
                                    {
                                        
                                        string name = System.IO.Path.GetFileNameWithoutExtension(file);
                                        Assembly assem = Assembly.LoadFrom(file);
                                        var type = assem.GetType($"{name}.Program", true, true);
                                        var res = type.GetMethod("Main").Invoke(null, new object[] { new object[] { parameters, dataGroup } });
                                        if (!(res is object[] results))
                                        {
                                            RhinoApp.WriteLine("回收输出的时候失败！");
                                            break;
                                        }

                                        foreach(var obj in results)
                                        {
                                            if (!(obj is CommonObject common) || !(common is GeometryBase geo)) continue;
                                            vm.Geometries.Add(geo);
                                        }

                                        // TODO 回收结果
                                        break;
                                    }
                                case ".gh":
                                    {
                                        GH_Utils.RunHeadless();
                                        // TODO 1
                                        GH_Utils.ComputeGHFile(file);
                                        // TODO 回收输出
                                        break;
                                    }
                                case ".py":
                                    {
                                        var python = Rhino.Runtime.PythonScript.Create();
                                        python.SetVariable("params", parameters);
                                        python.SetVariable("data", dataGroup);
                                        python.ExecuteFile(file);
                                        var output = python.GetVariable("output");
                                        // TODO 回收输出
                                        break;
                                    }
                                case ".yml":
                                    {
                                        Actions actions = null;


                                        if (ext == ".ga")
                                        {
                                            //actions = DeserializeFromGAFile(filename);
                                        }
                                        else if (ext == ".yml")
                                        {
                                            actions = ParseActionsFromYamlFile(file);
                                        }
                                        else throw new Exception($"{ext}格式不支持GA");

                                        //actions.SetInput(dialog.Results);
                                        actions.Solve();
  
                                        break;
                                    }
                                default:
                                    {
                                        throw new Exception($"不支持的程序类型{ext}");
                                    }
                            }
                            RhinoDoc.ActiveDoc.Views.Redraw();
                            break;
                        }
                    default: break;
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine(ex.Message);
                RhinoApp.WriteLine(ex.StackTrace);
                RhinoApp.WriteLine(ex.Source);
            }
        }

        public static Actions ParseActionsFromYamlFile(string filename)
        {
            string dir = SIO.Path.GetDirectoryName(filename);
            string yaml = SIO.File.ReadAllText(filename);

            var actions = ParseActionsFromYaml(yaml);

            actions.Init(dir);

            return actions;
        }

        public static Actions ParseActionsFromYaml(string yaml)
        {
            var deserializer = new Deserializer();

            var actions = deserializer.Deserialize<Actions>(yaml);

            if (actions == null) throw new Exception("actions parse failed");

            return actions;
        }
    }

    public class ClientEventArgs
    {
        public ClientEventType route;
        public JObject data;
    }

    public enum ClientEventType
    {
        TaskGetInput,
        TaskProcess
    }
}
