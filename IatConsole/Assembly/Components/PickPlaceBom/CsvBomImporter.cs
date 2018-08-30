using System.Collections.Generic;
using System.IO;
using CsvHelper;

namespace IatConsole.Assembly.Components.PickPlaceBom
{
    public static class CsvBomImporter
    {
        public static List<CsvBomComponent> ReadBomComponents(IatRunConfiguration config)
        {
            string filename = config.AssemblyDataSettings.PickPlaceBomImportSettings.BomFile;
            var rd = new CsvReader(new StreamReader(filename));

            List<CsvBomComponent> bomComponents = new List<CsvBomComponent>();

            while (rd.Read())
            {
                if (rd.IsRecordEmpty()) continue;

                CsvBomComponent cbc = new CsvBomComponent
                {
                    LibRef = rd[config.AssemblyDataSettings.PickPlaceBomImportSettings.BomPartReferenceColumnName],
                    Description = rd[config.AssemblyDataSettings.PickPlaceBomImportSettings.BomDescriptionColumnName],
                    Designator = rd[config.AssemblyDataSettings.PickPlaceBomImportSettings.BomDesignatorColumnName]
                };
                bomComponents.Add(cbc);
            }

            return bomComponents;
        }
    }
}
