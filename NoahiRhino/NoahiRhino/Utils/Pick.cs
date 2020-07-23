using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoahiRhino.Utils
{
    public static class Pick
    {
        public static List<Curve> Curves()
        {
            GetObject go;
            while (true)
            {
                go = new GetObject();
                go.AcceptNothing(true);
                go.AcceptEnterWhenDone(true);
                go.SetCommandPrompt("请选择一条或多条曲线，之后回车或者右键确认");
                go.GeometryFilter = ObjectType.Curve | ObjectType.EdgeFilter;

                if (go.GetMultiple(1, 0) != GetResult.Object) return null;

                List<Curve> ghCurveList1 = new List<Curve>();
                Array.ForEach(go.Objects(), (ObjRef obj) => ghCurveList1.Add(obj.Curve()));
                if (ghCurveList1.Count == 0) return null;
                return ghCurveList1;
            }
        }

        public static List<Guid> Guids(ObjectType filter)
        {
            GetObject go;
            while (true)
            {
                go = new GetObject();
                go.AcceptNothing(true);
                go.AcceptEnterWhenDone(true);
                go.SetCommandPrompt("请选择一个或多个，之后回车确认");
                go.GeometryFilter = filter;
                if (go.GetMultiple(1, 0) != GetResult.Object) return null;

                List<Guid> ghCurveList1 = new List<Guid>();
                Array.ForEach(go.Objects(), (ObjRef obj) => ghCurveList1.Add(obj.ObjectId));
                if (ghCurveList1.Count == 0) return null;
                return ghCurveList1;
            }
        }

        public static Guid Guid(ObjectType filter)
        {
            GetObject go;
            while (true)
            {
                go = new GetObject();
                go.AcceptNothing(true);
                go.AcceptEnterWhenDone(true);
                go.SetCommandPrompt("请选择一个物件，之后回车确认");
                go.GeometryFilter = filter;

                if (go.Get() != GetResult.Object) return System.Guid.Empty;

                var obj = go.Object(0);
                return obj.ObjectId;
            }
        }

        public static Point3d Point()
        {
            GetObject go;
            while (true)
            {
                go = new GetObject();
                go.AcceptNothing(true);
                go.AcceptEnterWhenDone(true);
                go.SetCommandPrompt("请选择一条点物件，之后回车或者右键确认");
                go.GeometryFilter = ObjectType.Point;

                if (go.Get() != GetResult.Object) return Point3d.Unset;

                var obj = go.Object(0);
                return obj.Point().Location;
            }
        }

        public static Curve Curve()
        {
            GetObject go;
            while (true)
            {
                go = new GetObject();
                go.AcceptNothing(true);
                go.AcceptEnterWhenDone(true);
                go.SetCommandPrompt("请选择一条曲线，之后回车或者右键确认");
                go.GeometryFilter = ObjectType.Curve | ObjectType.EdgeFilter;

                if (go.Get() != GetResult.Object) return null;

                var obj = go.Object(0);
                var crv = obj.Curve();
                return crv;
            }
        }
    }
}
