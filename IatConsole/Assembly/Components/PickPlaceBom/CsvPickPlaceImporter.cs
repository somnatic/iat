using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using NLog;

namespace IatConsole.Assembly.Components.PickPlaceBom
{
    public static class CsvPickPlaceImporter
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private enum HeaderValueUnits { Mm, Mil, Unspecified}

        private static decimal ConvertValueToMm(string val, HeaderValueUnits headerUnit)
        {
            if (val.EndsWith("mm"))
            {
                return decimal.Parse(val.Substring(0, val.Length - 2), CultureInfo.InvariantCulture);
            }
            if (val.EndsWith("mil"))
            {
                return decimal.Parse(val.Substring(0, val.Length - 3), CultureInfo.InvariantCulture)*25.4m;
            }

            if (headerUnit == HeaderValueUnits.Mm)
            {
                return decimal.Parse(val.Substring(0, val.Length), CultureInfo.InvariantCulture);
            }

            if (headerUnit == HeaderValueUnits.Mil)
            {
                return decimal.Parse(val.Substring(0, val.Length), CultureInfo.InvariantCulture) * 25.4m;
            }

            throw new Exception("Couldn't convert value: " + val);
        }

        private static ComponentLayer ConvertValueToComponentLayer(string val, PickPlaceBomImportSettings ppbis)
        {
            string topLayer = ppbis.PnpLayerTopName;
            string bottomLayer = ppbis.PnpLayerBottomName;

            if (val == topLayer) return ComponentLayer.Top;
            if (val == bottomLayer) return ComponentLayer.Bottom;
            throw new Exception("Couldn't convert value: " + val);
        }

        private static decimal ConvertValueToRotation(string val)
        {
            return decimal.Parse(val, CultureInfo.InvariantCulture);
        }

        public static List<CsvPickPlaceComponent> ReadPickPlaceComponents(IatRunConfiguration config)
        {
            if (!File.Exists(config.AssemblyDataSettings.PickPlaceBomImportSettings.PnpFile))
            {
                Logger.Error("Couldn't read Pick'n'Place file");
                return null;
            }

            CsvReader rd;
            try
            {
                rd = new CsvReader(new StreamReader(config.AssemblyDataSettings.PickPlaceBomImportSettings.PnpFile));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Couldn't read Pick'n'Place file");
                return null;
            }
            try
            {
                List<CsvPickPlaceComponent> ppComponent = new List<CsvPickPlaceComponent>();

                // Load field headers, check to see if the values are in mm or mil
                HeaderValueUnits xPosHeaderUnit, yPosHeaderUnit;
                if (config.AssemblyDataSettings.PickPlaceBomImportSettings.PnpCenterXColumnName.Contains("mm"))
                {
                    xPosHeaderUnit = HeaderValueUnits.Mm;
                }
                else if (config.AssemblyDataSettings.PickPlaceBomImportSettings.PnpCenterXColumnName.Contains("mil"))
                {
                    xPosHeaderUnit = HeaderValueUnits.Mil;
                }
                else
                {
                    xPosHeaderUnit = HeaderValueUnits.Unspecified;
                }

                if (config.AssemblyDataSettings.PickPlaceBomImportSettings.PnpCenterYColumnName.Contains("mm"))
                {
                    yPosHeaderUnit = HeaderValueUnits.Mm;
                }
                else if (config.AssemblyDataSettings.PickPlaceBomImportSettings.PnpCenterYColumnName.Contains("mil"))
                {
                    yPosHeaderUnit = HeaderValueUnits.Mil;
                }
                else
                {
                    yPosHeaderUnit = HeaderValueUnits.Unspecified;
                }
                while (rd.Read())
                {
                    if (rd.IsRecordEmpty()) continue;

                    CsvPickPlaceComponent cppc = new CsvPickPlaceComponent
                    {
                        Designator = rd[config.AssemblyDataSettings.PickPlaceBomImportSettings.PnpDesignatorColumnName],
                        RefX = ConvertValueToMm(rd[config.AssemblyDataSettings.PickPlaceBomImportSettings.PnpCenterXColumnName], xPosHeaderUnit),
                        RefY = ConvertValueToMm(rd[config.AssemblyDataSettings.PickPlaceBomImportSettings.PnpCenterYColumnName], yPosHeaderUnit),
                        ComponentLayer = ConvertValueToComponentLayer(rd[config.AssemblyDataSettings.PickPlaceBomImportSettings.PnpLayerColumnName], config.AssemblyDataSettings.PickPlaceBomImportSettings),
                        Rotation = ConvertValueToRotation(rd[config.AssemblyDataSettings.PickPlaceBomImportSettings.PnpRotationColumnName])
                    };

                    ppComponent.Add(cppc);
                }
                return ppComponent;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while reading Pick'n'Place file");
                return null;
            }
        } 
    }
}
