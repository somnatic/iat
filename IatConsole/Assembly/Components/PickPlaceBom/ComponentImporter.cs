using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IatConsole.Assembly.Components.PickPlaceBom
{
    public class PickPlaceBomComponentImporter : IComponentImport
    {
        public void LoadAssemblyData(Assembly ass, IatRunConfiguration config)
        {
            // Get the required files from the configuration data
            List<CsvPickPlaceComponent> ppComponents = CsvPickPlaceImporter.ReadPickPlaceComponents(config);
            List<CsvBomComponent> bomComponents = CsvBomImporter.ReadBomComponents(config);

            foreach (var ppc in ppComponents)
            {
                string designator = ppc.Designator;
                var bomComponent = bomComponents.SingleOrDefault(d => d.Designator == designator);
                if (bomComponent != null)
                {
                    Component c = new Component
                    {
                        LibRef = bomComponent.LibRef,
                        Description = bomComponent.Description,
                        Designator = ppc.Designator,
                        PositionX = ppc.RefX,
                        PositionY = ppc.RefY,
                        Rotation = ppc.Rotation
                    };


                    if (ppc.ComponentLayer == ComponentLayer.Top)
                    {
                        if (!ass.ComponentsTop.ContainsKey(c.LibRef))
                        {
                            ass.ComponentsTop.Add(c.LibRef, new List<Component>());
                        }
                        ass.ComponentsTop[c.LibRef].Add(c);
                    }

                    if (ppc.ComponentLayer == ComponentLayer.Bottom)
                    {
                        if (!ass.ComponentsBottom.ContainsKey(c.LibRef))
                        {
                            ass.ComponentsBottom.Add(c.LibRef, new List<Component>());
                        }
                        ass.ComponentsBottom[c.LibRef].Add(c);
                    }
                }
                else
                {
                    // Probably not populated
                    ass.DnpPositions.Add(new PointF
                    {
                        X = (float)ppc.RefX,
                        Y = (float)ppc.RefY
                    });
                }
            }
        }
    }
}
