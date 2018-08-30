using System;
using System.Drawing;

namespace IatConsole.Assembly.Pcb
{
    public abstract class PcbShape
    {
        public float Left { get; set; }
        public float Top { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        /// <summary>
        /// Returns a rectangle where width and height are always > 0
        /// </summary>
        public RectangleF ExtentRectangle
        {
            get
            {
                var xa = Left;
                var xb = Left + Width;
                var ya = Top;
                var yb = Top + Height;

                var minx = Math.Min(xa, xb);
                var maxx = Math.Max(xa, xb);
                var miny = Math.Min(ya, yb);
                var maxy = Math.Max(ya, yb);

                return new RectangleF(minx, miny, maxx-minx, maxy-miny);
            }
        }

        

    }
}
