using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Interop.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NoahiCAD
{
    public class ACAD
    {
        IAcadApplication app = null;
        readonly string AcadID = "AutoCAD.Application";

        public async Task<object> StartCAD(dynamic input)
        {
            try
            {
                GetAutoCAD();
            }
            catch (COMException cx)
            {
                try
                {
                    StartAutoCad();
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }

            AcadModelSpace model = app.ActiveDocument.Database.ModelSpace;

            List<string> tmp = new List<string> { };

            foreach(dynamic t in model)
            {
                tmp.Add(t.Layer);
            }

            dynamic rec = app.ActiveDocument.Database.ModelSpace.Count;

            return tmp;
        }

        void GetAutoCAD()
        {
            // try to Get an instance
            app = Marshal.GetActiveObject(AcadID) as AcadApplication;
        }

        void StartAutoCad()
        {
            var t = Type.GetTypeFromProgID(AcadID, true);
            var obj = Activator.CreateInstance(t, true) as AcadApplication;
            app = obj;
        }
    }
}
