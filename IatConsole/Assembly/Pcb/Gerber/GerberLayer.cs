using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IatConsole.Assembly.Pcb.Gerber
{
    public class GerberLayer : IPcbLayer
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, ApertureData> _apertures = new Dictionary<string, ApertureData>();

        public List<PcbShape> PcbShapes { get; } = new List<PcbShape>();

        private double _xmin;
        private double _xmax;
        private double _ymin;
        private double _ymax;

        private static void PerformFlash(GerberLayer gl, PointF currentPos, string currentPenString)
        {
            // Flash at current position
            // This could also be a rectangular shape
            if (gl._apertures[currentPenString].GetApertureType() == ApertureData.ApertureType.Rectangular)
            {
                var ad = (ApertureDataRect)gl._apertures[currentPenString];
                var rect = new PcbRect
                    {
                        Left = currentPos.X - ad.Width/2.0f,
                        Top = currentPos.Y - ad.Height/2.0f,
                        Width = ad.Width,
                        Height = ad.Height
                    };

                gl.PcbShapes.Add(rect);
            }
            else if (gl._apertures[currentPenString].GetApertureType() == ApertureData.ApertureType.Circle)
            {
                var ad = (ApertureDataCircle)gl._apertures[currentPenString];
                var circle = new PcbEllipse
                    {
                        Left = currentPos.X - ad.Diameter/2.0f,
                        Top = currentPos.Y - ad.Diameter/2.0f,
                        Width = ad.Diameter,
                        Height = ad.Diameter
                    };

                gl.PcbShapes.Add(circle);
            }
            else if (gl._apertures[currentPenString].GetApertureType() == ApertureData.ApertureType.Oval)
            {
                var ad = (ApertureDataOval)gl._apertures[currentPenString];
                var oval = new PcbEllipse
                    {
                        Left = currentPos.X - ad.Width/2.0f,
                        Top = currentPos.Y - ad.Height/2.0f,
                        Width = ad.Width,
                        Height = ad.Height
                    };

                gl.PcbShapes.Add(oval);
            }
        }

        private static void AddSimpleLine(GerberLayer gd, float x1, float y1, float x2, float y2, float strokeThickness)
        {
            var line = new PcbLine
                {
                    Left = x1,
                    Top = y1,
                    Width = x2 - x1,
                    Height = y2 - y1
                };

            if (x1 < gd._xmin) gd._xmin = x1;
            if (x1 > gd._xmax) gd._xmax = x1;
            if (x2 < gd._xmin) gd._xmin = x2;
            if (x2 > gd._xmax) gd._xmax = x2;

            if (y1 < gd._ymin) gd._ymin = y1;
            if (y1 > gd._ymax) gd._ymax = y1;
            if (y2 < gd._ymin) gd._ymin = y2;
            if (y2 > gd._ymax) gd._ymax = y2;

            line.StrokeThickness = strokeThickness;
            gd.PcbShapes.Add(line);
        }

        private static float GetCurrentDiameter(GerberLayer gd, string cps)
        {
            if (gd._apertures[cps].GetApertureType() == ApertureData.ApertureType.Circle)
            {
                // Only circles are valid
                return ((ApertureDataCircle)gd._apertures[cps]).Diameter;
            }

            Logger.Warn("Invalid Aperature data, defaulting thickness of line stroke to 0.1");
            return 0.1f;
        }

        public RectangleF FindExtents()
        {
            if (!PcbShapes.Any())
            {
                return new RectangleF(0,0,0,0);
            }

            float minx = PcbShapes.Select(d => d.ExtentRectangle).Min(d => d.Left);
            float maxx = PcbShapes.Select(d => d.ExtentRectangle).Max(d => d.Left + d.Width);
            float miny = PcbShapes.Select(d => d.ExtentRectangle).Min(d => d.Top);
            float maxy = PcbShapes.Select(d => d.ExtentRectangle).Max(d => d.Top + d.Height);

            return new RectangleF(minx, miny, maxx-minx, maxy-miny);
        }

        public void PerformRelocation(float x, float y)
        {
            foreach (PcbShape gs in PcbShapes)
            {
                gs.Left = gs.Left - x;
                gs.Top = gs.Top - y;
            }   
        }

        public void LoadLayerDataFromFile(string filename)
        {
            // Read from file
            string[] fileContent = File.ReadAllLines(filename);

            // Use the . as decimal separator (in german culture this would be ,)
            var nfi = new NumberFormatInfo { NumberDecimalSeparator = "." };

            // This holds the current position
            var currentPos = new PointF();

            // "" means "default, non given"
            string currentPenString = "";

            // Set the default aperture
            _apertures.Add("", new ApertureDataCircle(0.1f));

            #region decoding lines);
            foreach (string line in fileContent)
            {
                if (line.StartsWith("D89"))
                {
                    // Not needed
                    continue;
                }

                Match m = Regex.Match(line, @"M02*");
                if (m.Success)
                {
                    continue;
                }

                m = Regex.Match(line, @"^D03*");
                if (m.Success)
                {
                    PerformFlash(this, currentPos, currentPenString);

                    continue;
                }

                m = Regex.Match(line, @"^%ADD(?<number>[0-9]*)C,(?<valuec>[-+]?[0-9]*\.?[0-9]+)\*");
                if (m.Success)
                {
                    ApertureData ad = new ApertureDataCircle(float.Parse(m.Groups["valuec"].Captures[0].Value, nfi));
                    _apertures.Add(m.Groups["number"].Captures[0].Value, ad);
                    continue;
                }

                m = Regex.Match(line, @"^%ADD(?<number>[0-9]*)R,(?<valuex>[-+]?[0-9]*\.?[0-9]+)X(?<valuey>[-+]?[0-9]*\.?[0-9]+)\*");
                if (m.Success)
                {
                    ApertureData ad = new ApertureDataRect(float.Parse(m.Groups["valuex"].Captures[0].Value, nfi), float.Parse(m.Groups["valuey"].Captures[0].Value, nfi));
                    _apertures.Add(m.Groups["number"].Captures[0].Value, ad);
                    continue;
                }

                m = Regex.Match(line, @"^%ADD(?<number>[0-9]*)O,(?<valuex>[-+]?[0-9]*\.?[0-9]+)X(?<valuey>[-+]?[0-9]*\.?[0-9]+)\*");
                if (m.Success)
                {
                    ApertureData ad = new ApertureDataOval(float.Parse(m.Groups["valuex"].Captures[0].Value, nfi), float.Parse(m.Groups["valuey"].Captures[0].Value, nfi));
                    _apertures.Add(m.Groups["number"].Captures[0].Value, ad);
                    continue;
                }

                // there are two commands which select a drawing tool: G54Dxx* or Dxx*
                bool bSelectNewAperture = false;
                m = Regex.Match(line, @"^D(?<number>[0-9]*)\*");
                if (m.Success)
                {
                    bSelectNewAperture = true;
                }
                else
                {
                    m = Regex.Match(line, @"^G54D(?<number>[0-9]*)\*");
                    if (m.Success) bSelectNewAperture = true;
                }

                if (bSelectNewAperture)
                {
                    string number = m.Groups["number"].Captures[0].Value;
                    if (_apertures.ContainsKey(number))
                    {
                        switch (_apertures[number].GetApertureType())
                        {
                            case ApertureData.ApertureType.Circle:
                                currentPenString = number;
                                break;
                            case ApertureData.ApertureType.Rectangular:
                                currentPenString = number;
                                break;
                            case ApertureData.ApertureType.Oval:
                                currentPenString = number;
                                break;
                            default:
                                currentPenString = "";
                                break;
                        }
                    }
                    else
                    {
                        // Revert to default, this should be rather seldom
                        currentPenString = "";
                    }
                    continue;
                }

                m = Regex.Match(line, @"^D01\*");
                if (m.Success)
                {
                    continue;
                }

                m = Regex.Match(line, @"^X(?<valuex>[-+]?[0-9]*\.?[0-9]+)Y(?<valuey>[-+]?[0-9]*\.?[0-9]+)I(?<valuei>[-+]?[0-9]*\.?[0-9]+)J(?<valuej>[-+]?[0-9]*\.?[0-9]+)D01*");
                if (m.Success)
                {

                    float x = float.Parse(m.Groups["valuex"].Captures[0].Value) / 1000.0f;
                    float y = float.Parse(m.Groups["valuey"].Captures[0].Value) / 1000.0f;

                    double width = currentPos.X - x;
                    double height = currentPos.Y - y;

                    // The following if limits to pure circles (no arcs are currently supported) 
                    if (width < 0.001 && height < 0.001)
                    {
                        // Add a circle
                        var ellipse = new PcbEllipse { Left = currentPos.X - 0.2f, Top = currentPos.Y - 0.2f, Width = 0.4f, Height = 0.4f };
                        PcbShapes.Add(ellipse);
                        
                    }

                    currentPos.X = x;
                    currentPos.Y = y;
                    continue;
                }


                m = Regex.Match(line, @"^X(?<valuex>[-+]?[0-9]*\.?[0-9]+)Y(?<valuey>[-+]?[0-9]*\.?[0-9]+)\*");
                if (m.Success)
                {
                    float x = float.Parse(m.Groups["valuex"].Captures[0].Value) / 1000.0f;
                    float y = float.Parse(m.Groups["valuey"].Captures[0].Value) / 1000.0f;
                    AddSimpleLine(this, currentPos.X, currentPos.Y, x, y, GetCurrentDiameter(this, currentPenString));
                    currentPos.X = x;
                    currentPos.Y = y;
                    continue;
                }

                m = Regex.Match(line, @"^X(?<valuex>[-+]?[0-9]*\.?[0-9]+)Y(?<valuey>[-+]?[0-9]*\.?[0-9]+)D01\*");
                if (m.Success)
                {
                    float x = float.Parse(m.Groups["valuex"].Captures[0].Value) / 1000.0f;
                    float y = float.Parse(m.Groups["valuey"].Captures[0].Value) / 1000.0f;
                    AddSimpleLine(this, currentPos.X, currentPos.Y, x, y, GetCurrentDiameter(this, currentPenString));

                    currentPos.X = x;
                    currentPos.Y = y;
                    continue;
                }

                m = Regex.Match(line, @"^X(?<valuex>[-+]?[0-9]*\.?[0-9]+)Y(?<valuey>[-+]?[0-9]*\.?[0-9]+)D02\*");
                if (m.Success)
                {
                    currentPos.X = float.Parse(m.Groups["valuex"].Captures[0].Value) / 1000.0f;
                    currentPos.Y = float.Parse(m.Groups["valuey"].Captures[0].Value) / 1000.0f;
                    continue;
                }

                m = Regex.Match(line, @"^X(?<valuex>[-+]?[0-9]*\.?[0-9]+)Y(?<valuey>[-+]?[0-9]*\.?[0-9]+)D03\*");
                if (m.Success)
                {
                    float x = float.Parse(m.Groups["valuex"].Captures[0].Value) / 1000.0f;
                    float y = float.Parse(m.Groups["valuey"].Captures[0].Value) / 1000.0f;
                    currentPos.X = x;
                    currentPos.Y = y;
                    PerformFlash(this, currentPos, currentPenString);

                    continue;
                }

                m = Regex.Match(line, @"^X(?<valuex>[-+]?[0-9]*\.?[0-9]+)\*");
                if (m.Success)
                {
                    float x = float.Parse(m.Groups["valuex"].Captures[0].Value) / 1000.0f;
                    AddSimpleLine(this, currentPos.X, currentPos.Y, x, currentPos.Y, GetCurrentDiameter(this, currentPenString));


                    currentPos.X = x;
                    continue;
                }

                m = Regex.Match(line, @"^X(?<valuex>[-+]?[0-9]*\.?[0-9]+)D01\*");
                if (m.Success)
                {
                    float x = float.Parse(m.Groups["valuex"].Captures[0].Value) / 1000.0f;
                    AddSimpleLine(this, currentPos.X, currentPos.Y, x, currentPos.Y, GetCurrentDiameter(this, currentPenString));

                    currentPos.X = x;
                    continue;
                }

                m = Regex.Match(line, @"^X(?<valuex>[-+]?[0-9]*\.?[0-9]+)D02\*");
                if (m.Success)
                {
                    float x = float.Parse(m.Groups["valuex"].Captures[0].Value) / 1000.0f;
                    currentPos.X = x;
                    continue;
                }

                m = Regex.Match(line, @"^X(?<valuex>[-+]?[0-9]*\.?[0-9]+)D03\*");
                if (m.Success)
                {
                    float x = float.Parse(m.Groups["valuex"].Captures[0].Value) / 1000.0f;
                    currentPos.X = x;
                    PerformFlash(this, currentPos, currentPenString);
                    continue;
                }

                m = Regex.Match(line, @"^Y(?<valuey>[-+]?[0-9]*\.?[0-9]+)\*");
                if (m.Success)
                {
                    float y = float.Parse(m.Groups["valuey"].Captures[0].Value) / 1000.0f;
                    AddSimpleLine(this, currentPos.X, currentPos.Y, currentPos.X, y, GetCurrentDiameter(this, currentPenString));
                    currentPos.Y = y;
                    continue;
                }

                m = Regex.Match(line, @"^Y(?<valuey>[-+]?[0-9]*\.?[0-9]+)D01\*");
                if (m.Success)
                {
                    float y = float.Parse(m.Groups["valuey"].Captures[0].Value) / 1000.0f;
                    AddSimpleLine(this, currentPos.X, currentPos.Y, currentPos.X, y, GetCurrentDiameter(this, currentPenString));
                    currentPos.Y = y;
                    continue;
                }

                m = Regex.Match(line, @"^Y(?<valuey>[-+]?[0-9]*\.?[0-9]+)D02\*");
                if (m.Success)
                {
                    float y = float.Parse(m.Groups["valuey"].Captures[0].Value) / 1000.0f;
                    currentPos.Y = y;
                    continue;
                }

                m = Regex.Match(line, @"^Y(?<valuey>[-+]?[0-9]*\.?[0-9]+)D03\*");
                if (m.Success)
                {
                    float y = float.Parse(m.Groups["valuey"].Captures[0].Value) / 1000.0f;
                    currentPos.Y = y;
                    PerformFlash(this, currentPos, currentPenString);
                    continue;
                }

                Logger.Debug("Ignoring line: " + line);
            }
            #endregion
        }

    }
}
