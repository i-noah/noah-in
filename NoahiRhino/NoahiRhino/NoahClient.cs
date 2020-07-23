using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Eto.Forms;
using Rhino;
using Rhino.UI;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using Eto.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.GUI.Canvas;
using Grasshopper;
using Rhino.Input;
using NoahiRhino.Utils;

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
                RhinoApp.WriteLine(eve.ToString());
                switch (eve.route)
                {
                    case ClientEventType.TaskGetInput:
                        {
                            RhinoApp.InvokeOnUiThread(new Action(() => 
                            {
                                var crvs = Pick.Curves();

                                if (crvs == null) return;

                                var structrue = new GH_Structure<IGH_Goo>();
                                
                                foreach(var crv in crvs)
                                {
                                    structrue.Append(new GH_Curve(crv));
                                }

                                var data = JsonConvert.SerializeObject(new JObject
                                {
                                    ["route"] = "TaskSetInput",
                                    ["id"] = eve.data,
                                    ["data"] = IO.SerializeGrasshopperData(structrue)
                                });

                                RhinoApp.WriteLine(data);

                                Client.Send(data);
                            }));
                            break;
                        }
                    case ClientEventType.TaskProcess:
                        {
                            string data = eve.data;
                            try
                            {
                                byte[] byteArray = Convert.FromBase64String(data);
                                var dataStructure = IO.DeserializeGrasshopperData(byteArray);
                                if (dataStructure.IsEmpty) break;
                                var allData = dataStructure.AllData(true);
                                
                                foreach(var obj in allData)
                                {
                                    GH_Curve crv = null;
                                    if (!GH_Convert.ToGHCurve_Primary(obj, ref crv) || crv == null) continue;

                                    Rhino.RhinoDoc.ActiveDoc.Objects.AddCurve(crv.Value);
                                }

                            } catch
                            {
                                break;
                            }
                            break;
                        }
                    default: break;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    public class ClientEventArgs
    {
        public ClientEventType route;
        public string data;
    }

    public enum ClientEventType
    {
        TaskGetInput,
        TaskProcess
    }
}
