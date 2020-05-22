using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Rhino;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoahiRhino
{
    public class Tasker
    {
        public Guid ID { get; set; }
        public string TaskFile { get; set; }

        public Tasker()
        {

        }

        public void Run()
        {
            RhinoApp.InvokeOnUiThread(new Action(() => { LoadGhDocument(TaskFile); }));
        }

        private void LoadGhDocument(string file)
        {

            if (!System.IO.File.Exists(file))
            {
                return;
            }
            GH_DocumentIO io = new GH_DocumentIO();
            io.Open(file);

            GH_Document doc = GH_Document.DuplicateDocument(io.Document);

            if (doc == null)
            {
                return;
            }

            GH_DocumentServer server = Instances.DocumentServer;

            if (server == null)
            {
                return;
            }

            server.AddDocument(doc);

            doc.Properties.ProjectFileName = ID.ToString();

            GH_Canvas activeCanvas = Instances.ActiveCanvas;
            if (activeCanvas == null)
            {
                return;
            }

            activeCanvas.Document = doc;
            activeCanvas.Document.IsModified = false;
            activeCanvas.Refresh();

            doc.SolutionEnd += Doc_SolutionEnd;
        }

        private void Doc_SolutionEnd(object sender, GH_SolutionEventArgs e)
        {

        }
    }
}
