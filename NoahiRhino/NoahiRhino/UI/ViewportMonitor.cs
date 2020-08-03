using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Rhino.DocObjects;

namespace NoahiRhino.UI
{
    public class ViewportMonitor : DisplayConduit
    {
        public List<Mesh> DisplayMeshes = new List<Mesh>();

        public List<Polyline> DisplayPolyline = new List<Polyline>();
        public Dictionary<Point3d, double> DisplayText = new Dictionary<Point3d, double>();
        public List<GeometryBase> Geometries = new List<GeometryBase>();

        protected override void PreDrawObjects(DrawEventArgs e)
        {
            var box = BoundingBox.Empty;

            foreach (var mesh in DisplayMeshes)
            {
                var b = mesh.GetBoundingBox(false);
                box.Union(b);
                e.Display.DrawMeshWires(mesh, Color.AliceBlue);
                e.Display.DrawMeshFalseColors(mesh);
            }

            e.Viewport.GetFrustumFarPlane(out Plane plane);

            foreach (var text in DisplayText)
            {
                string t = text.Value.ToString("f2");
                Point3d point = text.Key;
                plane.Origin = point;

                Text3d drawText = new Text3d(t, plane, 0.5);
                e.Display.Draw3dText(drawText, Color.Black);
                drawText.Dispose();
            }

            foreach (var line in DisplayPolyline)
            {
                e.Display.DrawPolyline(line, Color.Black);
            }
            
            foreach(var obj in Geometries)
            {
                if (obj == null) continue;

                var mat = new DisplayMaterial
                {
                    Diffuse = Color.AliceBlue,
                    Transparency = 0.5
                };

                var b = obj.GetBoundingBox(false);

                box.Union(b);

                switch (obj.ObjectType)
                {
                    case ObjectType.Brep:
                        {
                            var mesh = new Mesh();
                            Array.ForEach(Mesh.CreateFromBrep(obj as Brep, MeshingParameters.FastRenderMesh), m => mesh.Append(m));
                            e.Display.DrawMeshShaded(mesh, mat);
                            break;
                        }
                    case ObjectType.Curve:
                        e.Display.DrawCurve(obj as Curve, Color.AliceBlue);
                        break;
                    case ObjectType.Point:
                        e.Display.DrawPoint((obj as Rhino.Geometry.Point).Location, Color.AliceBlue);
                        break;
                    case ObjectType.Surface:
                        {
                            var mesh = Mesh.CreateFromSurface(obj as Surface, MeshingParameters.FastRenderMesh);
                            e.Display.DrawMeshShaded(mesh, mat);
                            break;
                        }
                    case ObjectType.Mesh:
                        e.Display.DrawMeshShaded(obj as Mesh, mat);
                        break;
                    default:
                        break;
                }

            }

            e.Viewport.SetClippingPlanes(box);
        }
    }
}
