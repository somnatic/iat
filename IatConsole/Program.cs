using System;
using System.Diagnostics;
using System.IO;
using IatConsole.Assembly.Components.PickPlaceBom;
using IatConsole.Assembly.Pcb.Gerber;
using NLog;

namespace IatConsole
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            var versionNumber = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Console.WriteLine("Intelligent Assembly Tool " + versionNumber);
            Console.WriteLine("Created by Thomas Linder / mail@thomaslinder.at / http://www.thomaslinder.at/iat");

#if CREATECONFIG
            // Create dummy
            IatRunConfiguration.CreateDummyConfiguration("IatRunConfiguration.iat");
            File.Copy("IatRunConfiguration.iat", @"..\..\IatRunConfiguration.iat", true);
            Logger.Info("Dummy file IatRunConfiguration.iat created");
            return;
#endif

            string configFile = "";

            if (args.Length == 0)
            {
                if (File.Exists("IatRunConfiguration.iat"))
                {
                    configFile = "IatRunConfiguration.iat";
                }
            }
            else if (args.Length == 1)
            {
                if (args[0] == "/c")
                {
                    // Create dummy
                    IatRunConfiguration.CreateDummyConfiguration("IatRunConfiguration.iat");
                    Logger.Info("Dummy configuration file IatRunConfiguration.iat created");
                    return;
                }
                else
                {
                    configFile = args[0];
                }
            }
            else
            {
                Logger.Error("Cannot find configuration file; please specify a configuration filename or create a dummy configuration using the /c switch");
                return;
            }


            if (!File.Exists(configFile))
            {
                Logger.Error("Cannot find configuration file; file does not exist");
                return;
            }

            IatRunConfiguration rc;
            try
            {
                rc = IatRunConfiguration.Load(configFile);
                
                Logger.Info("Configuration loaded successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return;

            }

            Assembly.Assembly assembly = new Assembly.Assembly();

            
            if (!string.IsNullOrWhiteSpace(rc.PcbDataSettings.GerberDataSettings.GerberMechanicalOutlineFile))
            {
                try
                {
                    assembly.MechanicalOutlineLayer = new GerberLayer();
                    assembly.MechanicalOutlineLayer.LoadLayerDataFromFile(rc.PcbDataSettings.GerberDataSettings.GerberMechanicalOutlineFile);
                    Logger.Info("Mechanical outline layer loaded successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Couldn't load mechanical outline layer although it was specified");
                    return;
                }
            }

            bool containsAtLeasteOneLayer = false;
            if (!String.IsNullOrWhiteSpace(rc.PcbDataSettings.GerberDataSettings.GerberTopOverlayFile))
            {
                try
                {
                    containsAtLeasteOneLayer = true;
                    assembly.TopOverlayLayer = new GerberLayer();
                    assembly.TopOverlayLayer.LoadLayerDataFromFile(rc.PcbDataSettings.GerberDataSettings.GerberTopOverlayFile);
                    Logger.Info("Top overlay layer loaded successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Couldn't load top overlay layer although it was specified");
                    return;
                }

                try
                {
                    assembly.TopPasteLayer = new GerberLayer();
                    assembly.TopPasteLayer.LoadLayerDataFromFile(rc.PcbDataSettings.GerberDataSettings.GerberTopPasteFile);
                    Logger.Info("Top paste layer loaded successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Couldn't load top paste layer although it was specified");
                    return;
                }
            }
            if (!string.IsNullOrWhiteSpace(rc.PcbDataSettings.GerberDataSettings.GerberBottomOverlayFile))
            {
                try
                {
                    containsAtLeasteOneLayer = true;
                    assembly.BottomOverlayLayer = new GerberLayer();
                    assembly.BottomOverlayLayer.LoadLayerDataFromFile(rc.PcbDataSettings.GerberDataSettings.GerberBottomOverlayFile);
                    Logger.Info("Bottom overlay layer loaded successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Couldn't load bottom overlay layer although it was specified");
                    return;
                }

                try
                {
                    assembly.BottomPasteLayer = new GerberLayer();
                    assembly.BottomPasteLayer.LoadLayerDataFromFile(rc.PcbDataSettings.GerberDataSettings.GerberBottomPasteFile);
                    Logger.Info("Bottom paste layer loaded successfully");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Couldn't load bottom paste layer although it was specified");
                    return;
                }
            }

            if (!containsAtLeasteOneLayer)
            {
                Logger.Error("Neither top nor bottom layer was specified");
                return;
            }


            // Load component data, currently only PickNPlace+Bom method is available
            var ppai = new PickPlaceBomComponentImporter();
            ppai.LoadAssemblyData(assembly, rc);

            
            //return ad;
            string filename = rc.OutputSettings.OutputFilename;
            PdfCreator.CreateAssemblyDocument(assembly, rc, filename);

            // Save the s_document...
            // ...and start a viewer
            Process.Start(filename);

        }
    }
}
