using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Napa.Core.FEM;
using Napa.Core.Geometry;
using Napa.Core.Steel;
using Napa.Scripting;

///<summary>
/// This script creates FEM traces around selected openings (or opening segments)
/// included into FEM model as hexagon shape.
///</summary>
public class HexagonOpeningTraces : ScriptBase {
    private const double SPACING = 0.1;
    private const double TOLERANCE = 0.3;

    public override void Run() {
        var segmentNamePrefix = "FEM@S:";
        var segments = Graphics.GetSelectedObjects()
            .Where(d => d.Name.StartsWith(segmentNamePrefix))
            .Select(d => FEM.Manager.CurrentModel.GetTopologicalSegment(Convert.ToInt32(d.Name.Substring(segmentNamePrefix.Length))))
            .Where(s => s.Features.First().StartsWith("O:"))
            .Select(s => Steel.Model.GetSteelObject(s.Features.First()) as IEditableOpening);
        var openings = Graphics.GetSelectedSteelObjects<IEditableOpening>().Concat(segments)
            .Where(o => o != null)
            .ToArray();        
        
        foreach (var opening in openings) {            
            var topSegment = FEM.Manager.CurrentModel.GetTopologicalSegments(opening.ID).FirstOrDefault();
            if (topSegment == null) return;
            
            var topElement = topSegment.GetTopologicalElements().FirstOrDefault();
            if (topElement == null) return;
            
            var opeUpDir = opening.VectorUp;
            var opeRefPoints = opening.GetReferencePoints();
            var opeTopPoints = new[] { opeRefPoints[4], opeRefPoints[6] };
            var opeBottomPoints = new[] { opeRefPoints[2], opeRefPoints[8] };
            
            Point3D nearestPointTop;
            var intersectionsTop = GetIntersections(topElement, opeTopPoints, opeUpDir, out nearestPointTop);
            if (nearestPointTop == null && intersectionsTop.Length == 2) {
                CreateTrace(opening, intersectionsTop);
                nearestPointTop = GetNearestPoint(intersectionsTop, opeRefPoints[5]);
            }
            if (nearestPointTop != null) CreateTrace(opening, new[] { opeRefPoints[5], nearestPointTop });
            
            Point3D nearestPointBottom;
            var intersectionsBottom = GetIntersections(topElement, opeBottomPoints, opeUpDir.Turn(), out nearestPointBottom);
            if (nearestPointBottom == null && intersectionsBottom.Length == 2) {
                CreateTrace(opening, intersectionsBottom);
                nearestPointBottom = GetNearestPoint(intersectionsBottom, opeRefPoints[1]); 
            }
            if (nearestPointBottom != null) CreateTrace(opening, new[] { opeRefPoints[1], nearestPointBottom });
        }
        FEM.FinishModelOperation("FEM: Create traces");
        Graphics.UpdateView();
    }
    
    private Point3D GetNearestPoint(Point3D[] linePoints, Point3D point) {
        var line = new Line3D(linePoints.First(), linePoints.Last());
        return line.NearestPoint(point);
    }
    
    private Point3D[] GetIntersections(ITopologicalElement topElement, Point3D[] curvePoints, Vector3D translationDir, out Point3D nearestPoint) {
        using (var topCurve = Geometry.Manager.CreateCurve(curvePoints)) {
            topCurve.TranslateAndScale(SPACING * translationDir.UnitVector, 1.0);
            topCurve.Extrapolate(10.0);
            var topMidPoint = topCurve.MidPoint;
            
            Point3D closestPoint = null;
            var intersections = topElement.Boundary.Select(b => {
                using (var segCurve = b.Item1.GetCurve()) {
                    if (segCurve.Distance(topMidPoint) < TOLERANCE) closestPoint = segCurve.GetClosestPoint(topMidPoint);
                    return segCurve.Intersection(topCurve, 0.001).FirstOrDefault();
                }
            }).Where(i => i != null).ToArray();
            nearestPoint = closestPoint;
            return intersections;
        }
    }
    
    private void CreateTrace(IOpening opening, Point3D[] points) {
        var traceBuilderFactory = Steel.Model.GetTraceBuilderFactory();
        var traceBuilder = traceBuilderFactory.CreatePolygonPointTraceBuilder();
        traceBuilder.Points = points.Select(i => new ShipPoint(i)).ToList();
        traceBuilder.Projection = opening.NormalVector.MainDirection;
        FEM.Manager.CurrentModel.CreateTrace(opening.MainObject, traceBuilder, true);
    }
}