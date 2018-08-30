using System.Collections.Generic;
using System.Drawing;
using IatConsole.Assembly.Pcb;

namespace IatConsole.Assembly
{
    public class Assembly
    {
        public IPcbLayer TopOverlayLayer { get; set; }
        public IPcbLayer TopPasteLayer { get; set; }
        public IPcbLayer BottomOverlayLayer { get; set; }
        public IPcbLayer BottomPasteLayer { get; set; }

        public IPcbLayer MechanicalOutlineLayer { get; set; }

        public List<IPcbLayer> AllLayers => new List<IPcbLayer>
        {
            TopOverlayLayer, TopPasteLayer, BottomOverlayLayer, BottomPasteLayer, MechanicalOutlineLayer
        };


        public Dictionary<string, List<Components.Component>> ComponentsTop { get; }
        public Dictionary<string, List<Components.Component>> ComponentsBottom { get; }
        public List<PointF> DnpPositions { get; } 

        public Assembly()
        {
            ComponentsTop = new Dictionary<string, List<Components.Component>>();
            ComponentsBottom = new Dictionary<string, List<Components.Component>>();
            DnpPositions = new List<PointF>();

            TopOverlayLayer = null;
            TopPasteLayer = null;
            BottomOverlayLayer = null;
            BottomPasteLayer = null;
            MechanicalOutlineLayer = null;
        } 
    }
}
