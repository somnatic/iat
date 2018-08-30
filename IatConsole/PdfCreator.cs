using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using IatConsole.Assembly.Components;
using IatConsole.Assembly.Pcb;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;

namespace IatConsole
{
    public static class PdfCreator
    {
        public static void CreateAssemblyDocument(Assembly.Assembly assembly, IatRunConfiguration config, string filename)
        {
            // Create the bounding rectangles for each layer
            // add them up to a single rectangle (bounding rectangle for all layers)
            // to find out how much we have to move each component/gerber shape so it is aligned with 0/0
            var boundingRects = assembly.AllLayers.Select(d => d.FindExtents()).ToList();


            float moveLeft = boundingRects.Min(d => d.Left);
            float moveTop = boundingRects.Min(d => d.Top);

            // For each shape, move it to the according positions
            // Calculate the extents again (the result must be no item to the left/top of 0/0)
            var finalRects = new List<RectangleF>();
            foreach (var lgs in assembly.AllLayers)
            {
                lgs.PerformRelocation(moveLeft, moveTop);
                finalRects.Add(lgs.FindExtents());
            }

            float rFinalMinX = finalRects.Min(d => d.Left);
            float rFinalMinY = finalRects.Min(d => d.Top);

            Debug.Assert(Math.Abs(rFinalMinX) < 0.1);
            Debug.Assert(Math.Abs(rFinalMinY) < 0.1);

            // Since the minimum values are both 0.0 now, calculate the max values
            float maxExtensionX = finalRects.Max(d => d.Left + d.Width);
            float maxExtensionY = finalRects.Max(d => d.Top + d.Height);

            // We also need to move the components with the same factor
            foreach (Component c in assembly.ComponentsTop.Values.SelectMany(d => d))
            {
                c.PositionX -= (decimal)moveLeft;
                c.PositionY -= (decimal)moveTop;
            }
            foreach (Component c in assembly.ComponentsBottom.Values.SelectMany(d => d))
            {
                c.PositionX -= (decimal)moveLeft;
                c.PositionY -= (decimal)moveTop;
            }
            // ReSharper disable once UnusedVariable
            foreach (PointF pf in assembly.DnpPositions)
            {
                // TODO 
                //pf.X -= (decimal) moveLeft;
            }

            // Setup the required variables
            // Get the amount of lines and colors (currently tested for 5 only)
            int nrLines =  config.OutputSettings.ComponentColors.Count;

            List<XColor> colorsFromConfiguration = new List<XColor>();
            for (int i = 0; i < nrLines; i++)
            {
                colorsFromConfiguration.Add(GetXColorFromColor(config.OutputSettings.ComponentColors[i]));
            }

            // Create a temporary PDF file

            PdfDocument pdfDocument = new PdfDocument();
            pdfDocument.Info.Title = "Intelligent Assembly Tool";
            pdfDocument.Info.Author = "";
            pdfDocument.Info.Subject = "Intelligent Assembly Tool";
            pdfDocument.Info.Keywords = "IAT";


            List<Dictionary<string, List<Component>>> componentsGroupTop = SplitAssembly(assembly.ComponentsTop, config);
            List<Dictionary<string, List<Component>>> componentsGroupBottom = SplitAssembly(assembly.ComponentsBottom, config);

            Dictionary<ComponentLayer, List<Dictionary<string, List<Component>>>> allLayerData =
                new Dictionary<ComponentLayer, List<Dictionary<string, List<Component>>>>
                {
                    {ComponentLayer.Top, componentsGroupTop},
                    {ComponentLayer.Bottom, componentsGroupBottom}
                };

            foreach (var componentGroups in allLayerData)
            {
                foreach (var componentGroup in componentGroups.Value)
                {
                    PdfPage page = pdfDocument.AddPage();

                    page.Size = PageSize.A4;
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    // Draw the text string at the very bottom
                    XFont infoFont = new XFont("Consolas", 6, XFontStyle.Regular);
                    XTextFormatter infoTextFormatter = new XTextFormatter(gfx) {Alignment = XParagraphAlignment.Center};
                    XRect infoRect = new XRect(0, page.Height - 20, page.Width, 20);

                    var versionNumber = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    infoTextFormatter.DrawString("Intelligent Assembly Tool " + versionNumber + Environment.NewLine + "Created by Thomas Linder / mail@thomaslinder.at / http://www.thomaslinder.at/iat", infoFont, XBrushes.Black, infoRect);

                    // Draw the component box at the bottom
                    XPen tableDrawPen = new XPen(XColors.Black)
                    {
                        Width = 0.01,
                        LineCap = XLineCap.Round,
                        LineJoin = XLineJoin.Round
                    };

                    XFont tableFont = new XFont("Consolas", 8, XFontStyle.Regular);
                    XTextFormatter tableTextFormatter = new XTextFormatter(gfx);

                    // General page/table layout
                    double lineHeight = 12.0;
                    double topOffsetTotal = page.Height * 0.95 - nrLines * lineHeight;
                    double lineStartX = page.Width * 0.05;
                    double lineEndX = page.Width * 0.95;
                    double rectangleExtension = lineHeight - 4;

                    // Calculate column length for amount, article number & description
                    double remainingLineLength = lineEndX - lineStartX - lineHeight;
                    double lengthAmountField = remainingLineLength * 0.05;
                    double lengthArticleNumberField = remainingLineLength * 0.25;
                    double lengthArticleDescriptionField = remainingLineLength * 0.7;

                    double bottomOffsetTotal = topOffsetTotal + nrLines * lineHeight;

                    // Top Line
                    gfx.DrawLine(tableDrawPen, lineStartX, topOffsetTotal, lineEndX, topOffsetTotal);
                    // Bottom Line
                    gfx.DrawLine(tableDrawPen, lineStartX, topOffsetTotal + (nrLines) * lineHeight, lineEndX, topOffsetTotal + (nrLines) * lineHeight);
                    // Left Vertical
                    gfx.DrawLine(tableDrawPen, lineStartX, topOffsetTotal, lineStartX, bottomOffsetTotal);
                    // Right Vertical
                    gfx.DrawLine(tableDrawPen, lineEndX, topOffsetTotal, lineEndX, bottomOffsetTotal);
                    // First column (color rectangle)
                    gfx.DrawLine(tableDrawPen, lineStartX + lineHeight, topOffsetTotal, lineStartX + lineHeight, bottomOffsetTotal);
                    // Second column (amount)
                    double amountEndX = lineStartX + lineHeight + lengthAmountField;
                    gfx.DrawLine(tableDrawPen, amountEndX, topOffsetTotal, amountEndX, bottomOffsetTotal);
                    // Third column (article number)
                    double articleNumberEndX = lineStartX + lineHeight + lengthAmountField + lengthArticleNumberField;
                    gfx.DrawLine(tableDrawPen, articleNumberEndX, topOffsetTotal, articleNumberEndX, bottomOffsetTotal);



                    for (int lineIndex = 0; lineIndex < Math.Min(nrLines, componentGroup.Count); lineIndex++)
                    {
                        gfx.DrawRectangle(new XSolidBrush(colorsFromConfiguration[lineIndex]), lineStartX + 2, topOffsetTotal + lineIndex * lineHeight + 2, rectangleExtension, rectangleExtension);
                        XRect amountRect = new XRect(lineStartX + lineHeight, topOffsetTotal + lineIndex * lineHeight, lengthAmountField, lineHeight);
                        tableTextFormatter.DrawString(componentGroup.ElementAt(lineIndex).Value.Count.ToString(), tableFont, XBrushes.Black, amountRect, XStringFormats.TopLeft);
                        XRect articleNumberRect = new XRect(lineStartX + lineHeight + lengthAmountField, topOffsetTotal + lineIndex * lineHeight, lengthArticleNumberField, lineHeight);
                        tableTextFormatter.DrawString(componentGroup.ElementAt(lineIndex).Value[0].LibRef, tableFont, XBrushes.Black, articleNumberRect, XStringFormats.TopLeft);
                        XRect articleDescriptionRect = new XRect(lineStartX + lineHeight + lengthAmountField + lengthArticleNumberField, topOffsetTotal + lineIndex * lineHeight, lengthArticleDescriptionField, lineHeight);
                        tableTextFormatter.DrawString(componentGroup.ElementAt(lineIndex).Value[0].Description, tableFont, XBrushes.Black, articleDescriptionRect, XStringFormats.TopLeft);

                        gfx.DrawLine(tableDrawPen, lineStartX, topOffsetTotal + (lineIndex + 1) * lineHeight, lineEndX, topOffsetTotal + (lineIndex + 1) * lineHeight);

                    }


                    // Get the page size to calculate the available space
                    double availableSizeX = page.Width * 0.9; // 90 % of the page width
                    double availableSizeY = page.Height * 0.85 - (bottomOffsetTotal - topOffsetTotal);    // Leave 5% of space on top and bottom and between the table and the drawing

                    double scaleFactorX = availableSizeX / maxExtensionX;
                    double scaleFactorY = availableSizeY / maxExtensionY;

                    double scaleFactor = Math.Min(scaleFactorX, scaleFactorY);

                    if (componentGroups.Key == ComponentLayer.Top)
                    {
                        DrawLayers(gfx, config, ComponentLayer.Top, scaleFactor, assembly.TopOverlayLayer, assembly.TopPasteLayer, assembly.MechanicalOutlineLayer, maxExtensionX, maxExtensionY, componentGroup, colorsFromConfiguration);
                    }
                    else
                    {
                        DrawLayers(gfx, config, ComponentLayer.Bottom, scaleFactor, assembly.BottomOverlayLayer, assembly.BottomPasteLayer, assembly.MechanicalOutlineLayer, maxExtensionX, maxExtensionY, componentGroup, colorsFromConfiguration);
                    }

                }
            }



            pdfDocument.Save(filename);


        }

        private static void DrawLayers(XGraphics gfx, IatRunConfiguration config, ComponentLayer componentLayer, double scaleFactor, IPcbLayer overlayLayer, IPcbLayer pasteLayer, IPcbLayer mechanicalLayer, double maxExtensionX, double maxExtensionY, Dictionary<string, List<Component>> componentGroup, List<XColor> colorsFromConfiguration)
        {
            var state = gfx.Save();

            if (componentLayer == ComponentLayer.Top)
            {
                gfx.ScaleTransform(1, -1);
                gfx.TranslateTransform(1, -maxExtensionY);
                gfx.TranslateTransform(gfx.PdfPage.Width*0.05, -gfx.PdfPage.Height*0.05);
                gfx.TranslateTransform(1, -maxExtensionY*scaleFactor + maxExtensionY);
                gfx.ScaleTransform(scaleFactor);
            }
            else
            {
                gfx.ScaleTransform(-1, -1);
                gfx.TranslateTransform(-maxExtensionX, -maxExtensionY);
                gfx.TranslateTransform(-gfx.PdfPage.Width * 0.05, -gfx.PdfPage.Height * 0.05);
                gfx.TranslateTransform(-maxExtensionX * scaleFactor + maxExtensionX, -maxExtensionY * scaleFactor + maxExtensionY);
                gfx.ScaleTransform(scaleFactor);

            }

            XPen xpenPasteLayer = new XPen(XColors.Gray)
            {
                Width = 0.1,
                LineCap = XLineCap.Round,
                LineJoin = XLineJoin.Round
            };
            foreach (PcbShape s in pasteLayer.PcbShapes)
            {
                DrawElement(gfx, s, xpenPasteLayer);
            }

            XPen xpenOverlayLayer = new XPen(XColors.Black)
            {
                Width = 0.1,
                LineCap = XLineCap.Round,
                LineJoin = XLineJoin.Round
            };
            foreach (PcbShape s in overlayLayer.PcbShapes)
            {
                DrawElement(gfx, s, xpenOverlayLayer);
            }

            XPen xpenMechanicalOutlineLayer = new XPen(XColors.DarkRed)
            {
                Width = 0.1,
                LineCap = XLineCap.Round,
                LineJoin = XLineJoin.Round
            };
            foreach (PcbShape s in mechanicalLayer.PcbShapes)
            {
                DrawElement(gfx, s, xpenMechanicalOutlineLayer);
            }

            int nrLines = config.OutputSettings.ComponentColors.Count;

            for (int lineIndex = 0; lineIndex < Math.Min(nrLines, componentGroup.Count); lineIndex++)
            {
                XPen xpenComponent = new XPen(colorsFromConfiguration[lineIndex])
                {
                    Width = 0.1,
                    LineCap = XLineCap.Round,
                    LineJoin = XLineJoin.Round
                };
                XBrush xb = new XSolidBrush(colorsFromConfiguration[lineIndex]);

                double width = 0.5;
                double dotScaleFactor = (double)config.OutputSettings.DotScaleFactor;
                foreach (Component c in componentGroup.Values.ElementAt(lineIndex))
                {
                    double posX = (double) c.PositionX - width * dotScaleFactor / 2;
                    double posY = (double) c.PositionY - width * dotScaleFactor / 2;
                    double widthXY = width * dotScaleFactor; 
                    gfx.DrawRectangle(xpenComponent, xb, posX, posY , widthXY, widthXY);
                }
            }

            gfx.Restore(state);

        }

        private static List<Dictionary<string, List<Component>>> SplitAssembly(Dictionary<string, List<Component>> componentsLayer, IatRunConfiguration config)
        {
            List<Dictionary<string, List<Component>>> componentsGrouped = new List<Dictionary<string, List<Component>>>();

            Dictionary<string, List<Component>> dict = new Dictionary<string, List<Component>>();
            for (int i = 0; i < componentsLayer.Count; i++)
            {
                string key = componentsLayer.Keys.ElementAt(i);
                dict.Add(key, componentsLayer[key]);

                if (i % config.OutputSettings.ComponentColors.Count == config.OutputSettings.ComponentColors.Count - 1)
                {
                    // Last element for this dictionary
                    componentsGrouped.Add(dict);
                    dict = new Dictionary<string, List<Component>>();
                }
                else if (i == componentsLayer.Count - 1)
                {
                    // Finish
                    componentsGrouped.Add(dict);
                }
            }

            return componentsGrouped;
        }

        private static void DrawElement(XGraphics gfx, PcbShape gs, XPen pen)
        {
            if (gs is PcbLine)
            {
                gfx.DrawLine(pen, new XPoint(gs.Left, gs.Top), new XPoint(gs.Left + gs.Width, gs.Top + gs.Height));
            }
            if (gs is PcbRect)
            {
                gfx.DrawRectangle(pen, new XSolidBrush(pen.Color), gs.Left, gs.Top, gs.Width, gs.Height);
            }
            if (gs is PcbEllipse)
            {
                gfx.DrawEllipse(pen, new XSolidBrush(pen.Color), gs.Left, gs.Top, gs.Width, gs.Height);
            }
        }

        private static XColor GetXColorFromColor(Color color)
        {
            return XColor.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
