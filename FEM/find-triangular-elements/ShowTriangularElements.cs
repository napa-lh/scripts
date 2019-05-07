using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Napa.Graphics;
using Napa.Scripting;

/// <summary>
/// This script will color triangular FEM elements red so that they are 
/// easy to find and fix the mesh in those locations.
/// </summary>
public class ShowTriangularElements : ScriptBase {

    public override void Run() {
        Graphics.Erase();        
        var style = new StyleAttributes() { FaceColor = Colors.Red };
        foreach (var element in FEM.Manager.CurrentModel.Elements) {
            if (element.Nodes.Count == 3) {
                Graphics.OverrideStyle("FEM@E:" + element.Number, style);
            }
        }
        Graphics.UpdateView();
    }
}