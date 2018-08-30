using System.Collections.Generic;
using System.Drawing;

namespace IatConsole.Assembly.Pcb
{
    public interface IPcbLayer
    {
        List<PcbShape> PcbShapes { get; }

        void LoadLayerDataFromFile(string filename);
        RectangleF FindExtents();
        void PerformRelocation(float x, float y);
    }
}
