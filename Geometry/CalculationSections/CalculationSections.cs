//css_ref NapaCore.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Napa.Core.Geometry;
using Napa.Graphics;
using Napa.Legacy.NativeAccess;
using Napa.Scripting;

///<summary>
/// This script draws calculations sections of selected compartments
/// to the graphics.
///</summary>
public class CalculationSections : ScriptBase {
    public override void Run() {
        Graphics.Erase();
        var compartments = Graphics.GetSelectedGeometricObjects<IRoom>()
            .Select(c => c.Name)
            .ToArray();
            
        foreach (var compartmentName in compartments) {
            var curveName = "_C.CSE_TEMP_";
            GM.Calcsect(compartmentName, curveName);
            var curve = Geometry.Manager.GetCurve(curveName);
            foreach (var branch in curve.Branches) {
                Graphics.DrawLine(branch, new StyleAttributes() { LineColor = Colors.Green, LineWeight = 3 } );
                branch.Dispose();
            }
        }
        Graphics.UpdateView();
    }
}

public static class GM {
    private const int ID = Napa.Legacy.NativeAccess.SubsystemID.GM;

    public static void Calcsect(string name, string resultName) {
        ServiceFunctionUtil.InvokePlain(ID, "CALCSECT", name, resultName);
    }
}