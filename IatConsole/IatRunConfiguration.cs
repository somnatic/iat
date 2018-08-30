using System.Collections.Generic;
using System.IO;
using Color = System.Drawing.Color;
using Newtonsoft.Json;


namespace IatConsole
{
    public class IatRunConfiguration
    {
        public PcbDataSettings PcbDataSettings { get; }
        public AssemblyDataSettings AssemblyDataSettings { get; }
        public OutputSettings OutputSettings { get; }

        private IatRunConfiguration()
        {
            PcbDataSettings = new PcbDataSettings();
            AssemblyDataSettings = new AssemblyDataSettings();
            OutputSettings = new OutputSettings();
        }

        public static IatRunConfiguration Load(string filename)
        {
            string json = File.ReadAllText(filename);
            IatRunConfiguration config = JsonConvert.DeserializeObject<IatRunConfiguration>(json);
            return config;
        }

        public static void CreateDummyConfiguration(string filename)
        {
            var iatRunConfiguration = new IatRunConfiguration();
            iatRunConfiguration.PcbDataSettings.GerberDataSettings.GerberTopOverlayFile = @"..\..\..\demo-project\Out\Gerber\PCB1.GM11";
            iatRunConfiguration.PcbDataSettings.GerberDataSettings.GerberTopPasteFile = @"..\..\..\demo-project\Out\Gerber\PCB1.GTP";
            iatRunConfiguration.PcbDataSettings.GerberDataSettings.GerberBottomOverlayFile = @"..\..\..\demo-project\Out\Gerber\PCB1.GM12";
            iatRunConfiguration.PcbDataSettings.GerberDataSettings.GerberBottomPasteFile = @"..\..\..\demo-project\Out\Gerber\PCB1.GBP";
            iatRunConfiguration.PcbDataSettings.GerberDataSettings.GerberMechanicalOutlineFile = @"..\..\..\demo-project\Out\Gerber\PCB1.GM1";
            iatRunConfiguration.AssemblyDataSettings.PickPlaceBomImportSettings.BomFile = @"..\..\..\demo-project\Out\BOM\Bill of Materials-demo-project(Test1).csv";
            iatRunConfiguration.AssemblyDataSettings.PickPlaceBomImportSettings.BomPartReferenceColumnName = @"LibRef";
            iatRunConfiguration.AssemblyDataSettings.PickPlaceBomImportSettings.BomDesignatorColumnName = @"Designator";
            iatRunConfiguration.AssemblyDataSettings.PickPlaceBomImportSettings.BomDescriptionColumnName = @"Description";
            iatRunConfiguration.AssemblyDataSettings.PickPlaceBomImportSettings.PnpFile = @"..\..\..\demo-project\Out\Pick Place\Pick Place for PCB1.csv";
            iatRunConfiguration.AssemblyDataSettings.PickPlaceBomImportSettings.PnpDesignatorColumnName = @"Designator";
            iatRunConfiguration.AssemblyDataSettings.PickPlaceBomImportSettings.PnpCenterXColumnName = @"Mid X";
            iatRunConfiguration.AssemblyDataSettings.PickPlaceBomImportSettings.PnpCenterYColumnName = @"Mid Y";
            iatRunConfiguration.AssemblyDataSettings.PickPlaceBomImportSettings.PnpLayerColumnName = @"Layer";
            iatRunConfiguration.AssemblyDataSettings.PickPlaceBomImportSettings.PnpRotationColumnName = @"Rotation";
            iatRunConfiguration.AssemblyDataSettings.PickPlaceBomImportSettings.PnpLayerTopName = @"T";
            iatRunConfiguration.AssemblyDataSettings.PickPlaceBomImportSettings.PnpLayerBottomName = @"B";
            iatRunConfiguration.OutputSettings.ComponentColors = new List<Color>
            {
                Color.Red, Color.Green, Color.DodgerBlue, Color.Orange, Color.DarkViolet
            };
            iatRunConfiguration.OutputSettings.DotScaleFactor = 1.0m;

            string json = JsonConvert.SerializeObject(iatRunConfiguration, Formatting.Indented);
            File.WriteAllText(filename, json);

        }
    }

    public class OutputSettings
    {
        public List<Color> ComponentColors { get; set; }

        public decimal DotScaleFactor;

        public OutputSettings()
        {
            ComponentColors = new List<Color>();
        }
    }

    public class PcbDataSettings
    {
        public GerberDataSettings GerberDataSettings { get; }

        public PcbDataSettings()
        {
            GerberDataSettings = new GerberDataSettings();
        }
    }


    public class GerberDataSettings
    {
        public string GerberTopOverlayFile { get; set; }
        public string GerberBottomOverlayFile { get; set; }
        public string GerberTopPasteFile { get; set; }
        public string GerberBottomPasteFile { get; set; }
        public string GerberMechanicalOutlineFile { get; set; }

        public GerberDataSettings()
        {
            GerberTopOverlayFile = "";
            GerberBottomOverlayFile = "";
            GerberTopPasteFile = "";
            GerberBottomPasteFile = "";
            GerberMechanicalOutlineFile = "";
        }
    }

    public class AssemblyDataSettings
    {
        public PickPlaceBomImportSettings PickPlaceBomImportSettings { get; }

        public AssemblyDataSettings()
        {
            PickPlaceBomImportSettings = new PickPlaceBomImportSettings();
        }
    }

    public class PickPlaceBomImportSettings
    {
        public string BomFile { get; set; }
        public string BomPartReferenceColumnName { get; set; }
        public string BomDesignatorColumnName { get; set; }
        public string BomDescriptionColumnName { get; set; }

        public string PnpFile { get; set; }
        public string PnpDesignatorColumnName { get; set; }
        public string PnpCenterXColumnName { get; set; }
        public string PnpCenterYColumnName { get; set; }
        public string PnpLayerColumnName { get; set; }
        public string PnpRotationColumnName { get; set; }
        public string PnpLayerTopName { get; set; }
        public string PnpLayerBottomName { get; set; }

        public PickPlaceBomImportSettings()
        {
            BomFile = "";
            BomPartReferenceColumnName = "";
            BomDesignatorColumnName = "";
            BomDescriptionColumnName = "";

            PnpFile = "";
            PnpDesignatorColumnName = "";
            PnpCenterXColumnName = "";
            PnpCenterYColumnName = "";
            PnpLayerColumnName = "";
            PnpRotationColumnName = "";
            PnpLayerTopName = "";
            PnpLayerBottomName = "";
        }
    }
   
}
