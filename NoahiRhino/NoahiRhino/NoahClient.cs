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

namespace NoahiRhino
{
    public class NoahClient
    {
        internal int Port;
        internal Guid Guid;
        private WebSocket Client;

        private int RetryCnt = 0;
        private int MaxRetry = 5;

        public NoahClient(int port)
        {
            Port = port;
            Guid = Guid.NewGuid();

            Init();
        }

        public void Connect()
        {
            Client.Connect();

            //Client.Send("{\"route\": \"none\", \"msg\": \"This is Rhino\"}");
        }

        public void Close()
        {
            Client.Close();
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
            try
            {
                ClientEventArgs eve = JsonConvert.DeserializeObject<ClientEventArgs>(e.Data);
                switch (eve.route)
                {
                    case ClientEventType.TaskGetInput:
                        {
                            RhinoApp.InvokeOnUiThread(new Action(() => 
                            {
                                var crvs = Pick.Curves();

                                if (crvs == null) return;

                                var crvList = new JArray();

                                foreach(var crv in crvs)
                                {
                                    string crvdata = IO.EncodeCommonObjectToBase64(crv);
                                    if (crvdata == null) continue;
                                    crvList.Add(crvdata);
                                }

                                var data = JsonConvert.SerializeObject(new JObject
                                {
                                    ["route"] = "TaskSetInput",
                                    ["id"] = eve.data["id"],
                                    ["data"] = crvList
                                });

                                Client.Send(data);
                            }));
                            break;
                        }
                    case ClientEventType.TaskProcess:
                        {
                            string file = eve.data["file"][0].ToString();
                            if (string.IsNullOrEmpty(file)) throw new Exception("没有指定程序文件");
                            if (!System.IO.File.Exists(file)) throw new Exception("指定程序文件不存在");
                            var ext = System.IO.Path.GetExtension(file);

                            switch(ext)
                            {
                                case ".dll":
                                    {
                                        string name = System.IO.Path.GetFileNameWithoutExtension(file);
                                        Assembly assem = Assembly.LoadFrom(file);
                                        var type = assem.GetType($"{name}.Program", true, true);
                                        // TODO 传入参数
                                        var res = type.GetMethod("Main").Invoke(null, new object[] { });
                                        // TODO 回收结果
                                        break;
                                    }
                                default:
                                    {
                                        throw new Exception($"不支持的程序类型{ext}");
                                    }
                            }

                            break;
                        }
                    default: break;
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine(ex.Message);
            }
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
