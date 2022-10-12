using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SVGGenerator
{
    public enum FillType
    {
        None,
        ScanLine,
        Stipple,
        GradientStipple,
        StippleDash,
    }

    [System.Serializable]
    public class Fill
    {
        public List<Line> fillLines = new List<Line>();

        public void GenerateFill(FillType fillType, SVGExporter svgExporter, TracedRegion region)
        {
            switch (fillType)
            {
                case FillType.ScanLine:
                    AngledScanLineFill(region, Vector2.one.normalized);
                    //ScanLineFill(svgExporter, region);
                    break;
                case FillType.Stipple:
                    StippleFill(region, Vector2.one);
                    break;
                case FillType.StippleDash:
                    StippleFill(region, new Vector2(20, 0));
                    break;
                case FillType.GradientStipple:
                    StippleFillGradient(svgExporter, region, Vector2.one, region.minRange, region.maxRange, 6);
                    break;
                case FillType.None:
                    break;
            }
        }

        #region FILL TYPES
        public void ScanLineFill(SVGExporter svgExporter, TracedRegion region)
        {
            ResetFill();

            int yIncrement = region.pixelRadiusLow;
            int yPixIndex;
            bool drawingLine = false;
            Line newLine = new Line();
            int startY = (int)region.bounds.y;

            for (int y = startY; y < (int)region.bounds.w; y += yIncrement)
            {
                yPixIndex = y;

                for (int x = 0; x < svgExporter.tex.width; x++)
                {
                    // IF DRAWING LINE AND GET TO EDGE OF TEXTURE, END LINE
                    //
                    if (drawingLine && x == svgExporter.tex.width - 1)// || lineDist > maxLineLength)
                    {
                        EndLine(x, yPixIndex);
                    }

                    //
                    // IF DARK ENOUGH, START DRAWING LINE
                    //
                    float valueSample = svgExporter.GetValueAtPixel(region.imageValueSelectionType, x, yPixIndex);

                    //print($"{x}  {yPixIndex}    valueSample {valueSample}     valueRange {valueRange}      drawingLine    {drawingLine}");

                    if (!drawingLine)
                    {
                        if (valueSample > region.minRange && valueSample < region.maxRange)
                        {
                            if (fillLines == null)
                                fillLines = new List<Line>();

                            // START NEW LINE
                            //
                            drawingLine = true;
                            newLine = new Line() { p0 = new Vector2(x, yPixIndex), newLine = true };
                        }
                    }
                    //
                    // IF NOT DARK ENOUGH AND DRAWING A LINE, FINISH LINE AND ADD TOO LINE LIST
                    //
                    else
                    {
                        if (valueSample < region.minRange || valueSample > region.maxRange || x == svgExporter.tex.width - 1)
                        {
                            EndLine(x - 1, yPixIndex);
                        }
                    }
                }
            }

            void EndLine(int x, int y)
            {
                // END LINE
                //
                drawingLine = false;

                if (new Vector2(x, y) != newLine.p0)
                {
                    newLine.p1 = new Vector2(x, y);
                    fillLines.Add(newLine);
                }
            }
        }

        public void AngledScanLineFill(TracedRegion region, Vector2 dir)
        {
            ResetFill();

            bool drawingLine = false;
            Line newLine = new Line();

            int maxSampleCount = 1000;

            float xSample = 0;
            float ySample = 0;
            float xIncrement = dir.x * region.pixelRadiusLow;
            float yIncrement = dir.y * region.pixelRadiusLow;

            Debug.Log((int)region.regionTypeMap.GetLength(0) + "  " + (int)region.regionTypeMap.GetLength(1));

            for (int x = 0; x < (int)region.regionTypeMap.GetLength(0); x += region.pixelRadiusLow)
            {
                for (int s = 0; s < maxSampleCount; s++)
                {
                    xSample = x + xIncrement * s;
                    ySample = yIncrement * s;

                    if (xSample >= (int)region.regionTypeMap.GetLength(0) ||
                        ySample >= (int)region.regionTypeMap.GetLength(1))
                        break;

                    UpdateLineDrawing(xSample, ySample);
                }
            }

            for (int y = 0; y < (int)region.regionTypeMap.GetLength(1); y += region.pixelRadiusLow)
            {
                for (int s = 0; s < maxSampleCount; s++)
                {
                    xSample = xIncrement * s;
                    ySample = y + yIncrement * s;

                    if (xSample > region.bounds.z ||
                        ySample > region.bounds.w)
                        break;

                    UpdateLineDrawing(xSample, ySample);
                }
            }

            void UpdateLineDrawing(float xSample, float ySample)
            {
                // IF DRAWING LINE AND GET TO EDGE OF TEXTURE, END LINE
                //
                if (drawingLine && ySample >= region.regionTypeMap.GetLength(1)-1)// || lineDist > maxLineLength)
                {
                    EndLine((int)xSample, (int)ySample);
                }


                // IF IS IN REGION, START DRAWING LINE
                //
                float valueSample = region.regionTypeMap[(int)xSample, (int)ySample];//  svgExporter.GetValueAtPixel(region.imageValueSelectionType, (int)xSample, (int)ySample);
                if (!drawingLine)
                {
                    if (valueSample == 1)
                    {
                        if (fillLines == null)
                            fillLines = new List<Line>();

                        // LOOK BACKWARDS ALONG DIRECTION TO FIND BORDER PIXEL
                        //
                        for (int i = 0; i < region.pixelRadiusLow * 2; i++)
                        {
                            xSample -= dir.x;
                            ySample -= dir.y;
                            float newValueSample = region.regionTypeMap[(int)xSample, (int)ySample];// svgExporter.GetValueAtPixel(region.imageValueSelectionType, (int)xSample, (int)ySample);
                            if (newValueSample == 0)
                            {
                                xSample += dir.x;
                                ySample += dir.y;
                                break;
                            }
                        }

                        // START NEW LINE
                        //
                        drawingLine = true;
                        newLine = new Line() { p0 = new Vector2((int)xSample,(int)ySample), newLine = true };
                    }
                }
                // IF NOT DARK ENOUGH AND DRAWING A LINE, FINISH LINE AND ADD TOO LINE LIST
                //
                else
                {
                    if (valueSample != 1)
                    {
                        // LOOK BACKWARDS ALONG DIRECTION TO FIND BORDER PIXEL
                        //
                        for (int i = 0; i < region.pixelRadiusLow * 2; i++)
                        {
                            xSample -= dir.x;
                            ySample -= dir.y;

                            if (xSample <=0 || ySample <= 0)
                                break;

                            float newValueSample = region.regionTypeMap[(int)xSample, (int)ySample]; // svgExporter.GetValueAtPixel(region.imageValueSelectionType, (int)xSample, (int)ySample);
                            if (newValueSample == 1)
                            {
                                //xSample += dir.x;
                                //ySample += dir.y;
                                break;
                            }
                        }

                        EndLine((int)xSample,(int)ySample);
                    }
                }
            }

            void EndLine(int x, int y)
            {
                // END LINE
                //
                drawingLine = false;

                if (new Vector2(x, y) != newLine.p0)
                {
                    newLine.p1 = new Vector2(x, y);
                    fillLines.Add(newLine);
                }
            }
        }

        public void StippleFill(TracedRegion region, Vector2 stippleLength)
        {
            ResetFill();

            Vector2 boundsOffset = new Vector2(region.bounds.x, region.bounds.y);

            List<Vector2> poisonDiscSampledPoints = PoissonDiscSampling.GeneratePoints
            (
                radius: region.pixelRadiusLow,
                sampleRegionSize: new Vector2(region.bounds.z - region.bounds.x, region.bounds.w - region.bounds.y)
            );

            Debug.Log("poisonDiscSampledPoints: " + poisonDiscSampledPoints.Count);

            for (int i = 0; i < poisonDiscSampledPoints.Count; i++)
            {
                Vector2 poisonDiscSample = poisonDiscSampledPoints[i];

                if (region.regionMap[(int)(poisonDiscSample.x + boundsOffset.x), (int)(poisonDiscSample.y + boundsOffset.y)] > 0)
                {
                    Line newLine = new Line()
                    {
                        p0 = poisonDiscSample + boundsOffset,
                        p1 = poisonDiscSample + stippleLength + boundsOffset,
                        newLine = true
                    };
                    fillLines.Add(newLine);
                }
            }
        }

        public void StippleFillGradient(SVGExporter svgExporter, TracedRegion region, Vector2 stippleLength, float densityLower, float densityUpper, int gradientLevels = 5)
        {
            ResetFill();

            Vector2 boundsOffset = new Vector2(region.bounds.x, region.bounds.y);
                        
            // CREATE LIST OF SAMPLES FOR EACH LEVEL OF GRADIENT
            //
            List<List<Vector2>> poissonSampleLevels = new List<List<Vector2>>();
            for (int i = 0; i < gradientLevels; i++)
            {
                float norm = i / (float)(gradientLevels - 1);
                List<Vector2> newSample = PoissonDiscSampling.GeneratePoints
                (
                    radius: Mathf.Lerp(region.pixelRadiusLow, region.pixelRadiusHigh, norm),
                    sampleRegionSize: new Vector2(region.bounds.z - region.bounds.x, region.bounds.w - region.bounds.y)
                );
                poissonSampleLevels.Add(newSample);
            }

            float densityRange = densityUpper - densityLower;
            float levelRange = densityRange / (float)gradientLevels;


            //
            // For each sampled level
            //
            for (int j = 0; j < poissonSampleLevels.Count; j++)
            {
                List<Vector2> poisonDiscSampledPoints = poissonSampleLevels[j];

                float valueMin = densityLower + (levelRange * j);
                float valueMax = valueMin + levelRange;

                Debug.Log($"valueMin  {valueMin}   valueMax  {valueMax}");

                //
                // Iterate through each sample and check if the region value falls within its range
                //
                for (int i = 0; i < poisonDiscSampledPoints.Count; i++)
                {
                    // If in the region
                    //
                    Vector2 startPos = poisonDiscSampledPoints[i] + boundsOffset;
                    float startRegionValue = region.regionMap[(int)(startPos.x), (int)(startPos.y)];
                    if (startRegionValue > valueMin && startRegionValue < valueMax)
                    {
                        // Refine stipple length so that it fits within the region
                        //
                        Vector2 refinedStippleLength = Vector2.one;
                        Vector2 endPoint = startPos + refinedStippleLength;
                        for (int x = 0; x < 10; x++)
                        {
                            float norm = (float)x / 9.0f;
                            refinedStippleLength = Vector2.Lerp(Vector2.one, stippleLength, norm);
                            endPoint = startPos + refinedStippleLength;

                            // If outside of the texture sapce return
                            if (endPoint.x >= svgExporter.tex.width - 1 || endPoint.y >= svgExporter.tex.height - 1)
                                break;

                            float endRegionValue = region.regionMap[(int)endPoint.x, (int)endPoint.y];

                            // If not inside value range break
                            if (endRegionValue < valueMin || endRegionValue > valueMax)
                            {
                                break;
                            }
                        }

                        Line newLine = new Line()
                        {
                            p0 = startPos,
                            p1 = endPoint,
                            newLine = true
                        };
                        fillLines.Add(newLine);
                    }
                }
            }
        }
        #endregion

        public string GenerateSVGString()
        {
            string s = "";
            for (int i = 0; i < fillLines.Count; i++)
            {
                if (i == 0 || fillLines[i].newLine)
                    s += $"   M {fillLines[i].p0.x} {fillLines[i].p0.y}  L {fillLines[i].p1.x} {fillLines[i].p1.y}";
                else
                    s += $"   L {fillLines[i].p0.x} {fillLines[i].p0.y}  L {fillLines[i].p1.x} {fillLines[i].p1.y}";
            }

            return s;
        }

        public void ResetFill()
        {
            fillLines.Clear();
        }

        public void DrawGizmos(float pixelToWorldScalar)
        {
            for (int i = 0; i < fillLines.Count; i++)
            {
                Gizmos.DrawLine(fillLines[i].p0 * pixelToWorldScalar, fillLines[i].p1 * pixelToWorldScalar);
            }
        }
    }
}
