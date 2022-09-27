using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// Sample percieved luminance - DONE
// Create shader to be able to better select ranges
// Move all get pixels to get from a cached pixel array
// Create shader to mimick final output
// Show original image on the left same size as current
// Designate value region
// Value range
// Contour?
// Shading?
namespace SVGGenerator
{    public struct Line
    {
        public bool newLine;
        public Vector2 p0;
        public Vector2 p1;
    }

    [System.Serializable]
    public class Contour
    {
        public Vector2 startPix;
        public Vector2 endPix;
        public List<Line> lines = new List<Line>();
        public string endCondition;

        public bool closedContour => startPix == endPix;
    }

    [System.Serializable]
    public class TracedRegion
    {
        public Color col;

        public bool generateMinContour = true;
        public bool generateMaxContour = true;
        public bool generateScanLines = true;

        public bool debugDisable = false;

        [Range(0, 1)]
        public float minRange = .4f;
        [Range(0, 1)]
        public float maxRange = .5f;

        [HideInInspector]
        public float prevMinRange = .4f, prevMaxRange = .5f;

        [Range(0, 1)]
        public float scanLineDensity = .3f;

        public bool contourMinVisibility = true;
        public bool contourMaxVisibility = true;
        public bool fillLinesVisibility = true;


        [HideInInspector]
        public List<Contour> minContours = new List<Contour>();
        [HideInInspector]
        public List<Contour> maxContours = new List<Contour>();

        [HideInInspector]
        public List<Line> fillLines = new List<Line>();

        public void Trace(SVGExporter svgExporter)
        {           
            if (debugDisable)
                return;

            if (generateMinContour)
            {
                svgExporter.ContourTrace(this, minContours, minRange);              
            }

            if (generateMaxContour)
            {
                svgExporter.ContourTrace(this, maxContours, maxRange);
            }

            if (generateScanLines)
                fillLines = svgExporter.TextureScanLines(new Vector2(minRange, maxRange), scanLineDensity);            
        }
    }

    public class SVGExporter : MonoBehaviour
    {
        //
        // VARIABLES
        //
        public Texture2D tex;
        public float pixelToWorldScalar = .01f;

        public Transform imageQuad;
        public Material imageQuadMat;
        public Transform imageQuad_Original;
        public Material imageQuadMat_Original;


        [Header("REGIONS")]
        public TracedRegion[] tracedRegions;


        [Header("CONTOURS")]
        public int maxContours = 1;
        public bool startClockwize = true;
        private int maxContourSampleCount = 2;

        [Header("DEBUG")]
        public Vector3 debugPos;
        public float debugPosScale = .01f;
        public bool debugPrint = false;
        public float gizmoLineAlpha = .5f;
        public bool debugDrawContours = true;
        public bool debugDrawAllLines = true;
        public float debugContourOffset = .01f;



        private Vector2 pixelMidpoint => new Vector2(tex.width * .5f, tex.height * .5f);
        private Vector2 pixelMidpointWorldSpace => new Vector2(tex.width * .5f, tex.height * .5f) * pixelToWorldScalar;       


        private Color[] pixelCols;

        //
        // VECTOR GENERATION FUNCTIONS
        //
        public void ContourTrace(TracedRegion region, List<Contour> contourList, float valueCutoff)
        {
            int[,] contourPixels = new int[tex.width, tex.height];
            int debugContourSampleCount;
            int debugMaxSamplesPerContour = 40000;
            int contourCount = 0;

            Vector2[] neighborsClockwize = new Vector2[8]
            {
                new Vector2(-1,0),
                new Vector2(-1,1),
                new Vector2(0,1),
                new Vector2(1,1),
                new Vector2(1,0),
                new Vector2(1,-1),
                new Vector2(0,-1),
                new Vector2(-1,-1),
            };

            Vector2[] neighborsAntiClockwize = new Vector2[8]
            {
                new Vector2(-1,0),
                new Vector2(-1,-1),
                new Vector2(0,-1),
                new Vector2(1,-1),
                new Vector2(1,0),
                new Vector2(1,1),
                new Vector2(0,1),
                new Vector2(-1,1)
            };


            bool isBorderPixel = GetValueAtPixel(0, 0) < valueCutoff;
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    if (contourCount >= maxContours)
                    {
                        Debug.Log("*************MAX CONTOURS REACHED");
                        return;
                    }

                    debugPos = new Vector2(x, y);

                    bool prevIsBorderPix = isBorderPixel;
                    isBorderPixel = GetValueAtPixel(x, y) < valueCutoff; ;

                    //
                    // FIND PIXEL THAT CROSSES A BORDER
                    //
                    if (isBorderPixel && !prevIsBorderPix && GetValueAtPixel(x, y) < valueCutoff &&
                        contourPixels[x, y] == 0)
                    {
                        debugContourSampleCount = 0;

                        // TRACE CONTOUR
                        //
                        Vector2 startPix = new Vector2(x, y);
                        Vector2 currentContourPix = new Vector2(x, y);

                        Contour newContour = new Contour() { startPix = startPix };

                        bool clockwise = startClockwize;

                        contourPixels[x, y]++;
                        Line newLine = new Line() { p0 = startPix, newLine = true };

                        // DEBUG
                        //                  
                        if (debugPrint) Debug.Log("*************FOUND BORDER PIXEL: " + currentContourPix);

                        // SEARCH ADJASCENT PIXELS CLOCKWISE UNTIL FINDING A BLACK PIXEL THAT COMES AFTER A WHITE PIXEL
                        //                 
                        int neighborIndex = 0;
                        for (int i = 0; i <= neighborsClockwize.Length; i++)
                        {
                            // DEBUG
                            //
                            debugContourSampleCount++;
                            if (debugContourSampleCount > debugMaxSamplesPerContour)
                            {
                                if (debugPrint) Debug.Log(" ENDING CONTOUR: Max samples per contour reached");
                                break;
                            }



                            // GET NEIGHBOR COORDS, EITHER CLOCK OR ANTI CLOCKWISE
                            //
                            neighborIndex %= neighborsClockwize.Length;
                            int searchX, searchY;
                            searchX = clockwise ? (int)currentContourPix.x + (int)neighborsClockwize[neighborIndex].x : (int)currentContourPix.x + (int)neighborsAntiClockwize[neighborIndex].x;
                            searchY = clockwise ? (int)currentContourPix.y + (int)neighborsClockwize[neighborIndex].y : (int)currentContourPix.y + (int)neighborsAntiClockwize[neighborIndex].y;



                            // IF OUTSIDE OF THE IMAGE BOUNDS THEN CONT TO NEXT NEIGHBOR
                            //
                            if (searchX < 0 || searchY < 0 || searchX == tex.width || searchY == tex.height)
                                continue;


                            // CHECK TO SEE IF THIS PIXEL HAS BEEN SEARCHED BEFORE
                            //
                            if (contourPixels[searchX, searchY] >= maxContourSampleCount)
                            {
                                if (debugPrint) Debug.Log("********  ENDING CONTOUR:    Sampling contour pixel more than twice, Breaking");
                                break;
                            }

                            // CHECK TO SEE IF THE SAMPLED PIXEL IS START OR END OF PREV CONTOUR
                            //
                            foreach (Contour c in contourList)
                            {
                                if (c.startPix == new Vector2(searchX, searchY) || c.endPix == new Vector2(searchX, searchY))
                                {
                                    if (debugPrint) Debug.Log("********  ENDING CONTOUR:    Sampled start or end of other contour");
                                    break;
                                }
                            }

                            // SET FOUND PIXEL TO CURRENT POS AND ITERATE
                            //
                            if (GetValueAtPixel(searchX, searchY) < valueCutoff)
                            {
                                if (debugPrint) print($"-------   Found contour: {searchX} {searchY} @   nIndex:  {neighborIndex}");

                                switch (neighborIndex)
                                {
                                    case 0:
                                        neighborIndex = 6;
                                        break;
                                    case 1:
                                        neighborIndex = 6;
                                        break;
                                    case 2:
                                        neighborIndex = 0;
                                        break;
                                    case 3:
                                        neighborIndex = 0;
                                        break;
                                    case 4:
                                        neighborIndex = 2;
                                        break;
                                    case 5:
                                        neighborIndex = 2;
                                        break;
                                    case 6:
                                        neighborIndex = 4;
                                        break;
                                    case 7:
                                        neighborIndex = 4;
                                        break;
                                }


                                i = 0;
                                Vector2 newContourPixel = new Vector2(searchX, searchY);
                                newLine.p1 = newContourPixel;

                                // ADD LINES TO CONTOUR AND FULL LINES LIST
                                //
                                newContour.lines.Add(newLine);

                                contourPixels[searchX, searchY]++;

                                currentContourPix = newContourPixel;
                                newLine = new Line() { p0 = currentContourPix, newLine = false };

                                newContour.endPix = currentContourPix;

                                if (currentContourPix == startPix)
                                {
                                    if (debugPrint) print("ENDING CONTOUR:  BACK TO START END CONTOUR");
                                    break;
                                }
                            }
                            else
                            {
                                neighborIndex++;
                            }

                            if (i == 7 && debugPrint)
                                print("ENDING CONTOUR:  NO NEIGHBORS FOUND");
                        }

                        // IF CONTOUR DIDNT MAKE IT BACK TO START, TRY COUNTER CLOCKWIZE FROM START
                        //
                        //if(clockwise && !newContour.closedContour)
                        //{
                        //    clockwise = false;
                        //    prevContourPix = newContour.startPix;
                        //    currentContourPix = newContour.startPix;
                        //    neighborIndex = 0;
                        //}
                        //else



                        contourList.Add(newContour);

                        // ADD CONTOUR TO LIST
                        //
                        if (debugPrint) Debug.Log("ADDING NEW CONTOUR");
                        contourCount++;
                    }
                }
            }

            if (debugPrint) Debug.Log("*************END OF TEXTURE SEARCH REACHED");
        }

        public List<Line> TextureScanLines(Vector2 valueRange, float denistyNorm)
        {
            List<Line> newLines = new List<Line>();

            int yIncrement = (int)Mathf.Lerp(tex.height * .1f, 1, Mathf.Clamp01(denistyNorm));
            int yPixIndex;
            bool drawingLine = false;
            Line newLine = new Line();
            int startY = Random.Range(0, yIncrement);

            for (int y = startY; y < tex.height; y += yIncrement)
            {
                yPixIndex = y;

                for (int x = 0; x < tex.width; x++)
                {
                    // IF DRAWING LINE AND GET TO EDGE OF TEXTURE, END LINE
                    //
                    if (drawingLine && x == tex.width - 1)// || lineDist > maxLineLength)
                    {
                        EndLine(x, yPixIndex);
                    }

                    //
                    // IF DARK ENOUGH, START DRAWING LINE
                    //
                    float valueSample = GetValueAtPixel(x, yPixIndex);

                    //print($"{x}  {yPixIndex}    valueSample {valueSample}     valueRange {valueRange}      drawingLine    {drawingLine}");

                    if (!drawingLine)
                    {
                        if (valueSample > valueRange.x && valueSample < valueRange.y)
                        {
                            if (newLines == null)
                                newLines = new List<Line>();

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
                        if (valueSample < valueRange.x || valueSample > valueRange.y || x == tex.width - 1)
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
                    newLines.Add(newLine);
                }
            }

            return newLines;
        }

        float GetValueAtPixel(int x, int y)
        {
            float h, s, v;
            Color.RGBToHSV(pixelCols[x + y * tex.width], out h, out s, out v);
            return v;
            //return pixelCols[x + y * tex.width].r;
        }

        private float ColorToPercievedLuminance(Color col)
        {
            //return sRGBtoLin(col.r) * 0.2426f + sRGBtoLin(col.g) * 0.7152f + sRGBtoLin(col.b) * 0.0722f;
            return col.r * 0.2426f + col.g * 0.7152f + col.b * 0.0722f;
        }

        float sRGBtoLin(float colorChannel)
        {
            // Send this function a decimal sRGB gamma encoded color value
            // between 0.0 and 1.0, and it returns a linearized value.

            if (colorChannel <= 0.04045f)
            {
                return colorChannel / 12.92f;
            }
            else
            {
                return Mathf.Pow((colorChannel + 0.055f) / 1.055f, 2.4f);
            }
        }

        private void OnValidate()
        {
            foreach(TracedRegion region in tracedRegions)
            {
                if(region.minRange != region.prevMinRange)
                {
                    region.prevMinRange = region.minRange;
                    imageQuadMat.SetColor("_RegionCol", region.col);
                    imageQuadMat.SetFloat("_MinRange", region.minRange);
                    imageQuadMat.SetFloat("_MaxRange", region.maxRange);
                }

                if (region.maxRange != region.prevMaxRange)
                {
                    region.prevMaxRange = region.maxRange;
                    imageQuadMat.SetColor("_RegionCol", region.col);
                    imageQuadMat.SetFloat("_MinRange", region.minRange);
                    imageQuadMat.SetFloat("_MaxRange", region.maxRange);
                }
            }
        }

        //void GenerateRandomLines()
        //{
        //    lineList = new List<Line>();
        //    float radius = 400;
        //    for (int i = 0; i < 100; i++)
        //    {
        //        Line newLine = new Line()
        //        {
        //            p0 = pixelMidpoint + Random.insideUnitCircle * radius,
        //            p1 = pixelMidpoint + Random.insideUnitCircle * radius
        //        };

        //        lineList.Add(newLine);
        //    }
        //}



        //
        // CREATE & WRITE VECTOR TO FILE
        //

        [ContextMenu("Test")]
        public void WriteString()
        {
            // CACHE PIXEL COLS
            //
            pixelCols = tex.GetPixels();

            string path = "Assets/_Project/SVG Writer/Resources/testSVG " + System.DateTime.Now.ToString("yyyy-MM-dd") + ".svg";
           

            List<Line> lineList = new List<Line>();
            List<Contour> allContours = new List<Contour>();


            string testSVGStringHeader = @"<svg width=""800"" height=""800""    xmlns:xlink=""http://www.w3.org/1999/xlink"" style=""stroke:black; stroke-opacity:1; stroke-width:1;""  xmlns=""http://www.w3.org/2000/svg""> <defs id = ""genericDefs""/>";
                                        //<g> <path style = ""fill:none;"" d = """;

            foreach (TracedRegion region in tracedRegions)
            {
                testSVGStringHeader += @"""<g> <path style = ""fill:none;"" d = """;

                region.Trace(this);

                foreach(Contour c in region.minContours)
                {
                    for (int i = 0; i < c.lines.Count; i++)
                    {
                        if (i == 0 || c.lines[i].newLine)
                            testSVGStringHeader += $"   M {c.lines[i].p0.x} {c.lines[i].p0.y}  L {c.lines[i].p1.x} {c.lines[i].p1.y}";
                        else
                            testSVGStringHeader += $"   L {c.lines[i].p0.x} {c.lines[i].p0.y}  L {c.lines[i].p1.x} {c.lines[i].p1.y}";
                    }
                }

                foreach (Contour c in region.maxContours)
                {
                    for (int i = 0; i < c.lines.Count; i++)
                    {
                        if (i == 0 || c.lines[i].newLine)
                            testSVGStringHeader += $"   M {c.lines[i].p0.x} {c.lines[i].p0.y}  L {c.lines[i].p1.x} {c.lines[i].p1.y}";
                        else
                            testSVGStringHeader += $"   L {c.lines[i].p0.x} {c.lines[i].p0.y}  L {c.lines[i].p1.x} {c.lines[i].p1.y}";
                    }
                }


                // FILL LINES
                //
                for (int i = 0; i < region.fillLines.Count; i++)
                {
                    if (i == 0 || region.fillLines[i].newLine)
                        testSVGStringHeader += $"   M {region.fillLines[i].p0.x} {region.fillLines[i].p0.y}  L {region.fillLines[i].p1.x} {region.fillLines[i].p1.y}";
                    else
                        testSVGStringHeader += $"   L {region.fillLines[i].p0.x} {region.fillLines[i].p0.y}  L {region.fillLines[i].p1.x} {region.fillLines[i].p1.y}";
                }

                testSVGStringHeader += @"""/> </g>";
            }

          

            //for (int i = 0; i < lineList.Count; i++)
            //{
            //    if (i == 0 || lineList[i].newLine)
            //        testSVGStringHeader += $"   M {lineList[i].p0.x} {lineList[i].p0.y}  L {lineList[i].p1.x} {lineList[i].p1.y}";
            //    else
            //        testSVGStringHeader += $"   L {lineList[i].p0.x} {lineList[i].p0.y}  L {lineList[i].p1.x} {lineList[i].p1.y}";
            //}

            testSVGStringHeader += @"</svg>";


            // WRITE TO TEXT FILE
            //
            StreamWriter writer = new StreamWriter(path, false);
            writer.WriteLine(testSVGStringHeader);
            writer.Close();

            print("Lines generated: " + lineList.Count);
        }




        //
        // DEBUG
        //
        private void OnDrawGizmos()
        {
            if (imageQuadMat.GetTexture("_BaseMap") != tex)
                imageQuadMat.SetTexture("_BaseMap", tex);
            if (imageQuadMat_Original.GetTexture("_BaseMap") != tex)
                imageQuadMat_Original.SetTexture("_BaseMap", tex);



            // POSITION IMAGE QUAD
            //
            imageQuad.transform.position = pixelMidpointWorldSpace;
            imageQuad.transform.localScale = new Vector3(tex.width * pixelToWorldScalar, tex.height * pixelToWorldScalar);
            imageQuad_Original.transform.position = pixelMidpointWorldSpace + Vector2.left * tex.width * pixelToWorldScalar;
            imageQuad_Original.transform.localScale = imageQuad.transform.localScale;


            Gizmos.DrawSphere(debugPos * pixelToWorldScalar, debugPosScale);

            // DRAW CONTOUR START AND ENDS
            //
           
            foreach (TracedRegion region in tracedRegions)
            {
                Gizmos.color = region.col;

                if (region.contourMinVisibility)
                {
                    foreach (Contour c in region.minContours)
                    {
                        for (int i = 0; i < c.lines.Count; i++)
                        {
                            Gizmos.DrawLine(c.lines[i].p0 * pixelToWorldScalar, c.lines[i].p1 * pixelToWorldScalar);
                        }
                    }
                }

                if (region.contourMaxVisibility)
                {
                    foreach (Contour c in region.maxContours)
                    {
                        for (int i = 0; i < c.lines.Count; i++)
                        {
                            Gizmos.DrawLine(c.lines[i].p0 * pixelToWorldScalar, c.lines[i].p1 * pixelToWorldScalar);
                        }
                    }
                }

                if (region.fillLinesVisibility)
                {
                    for (int i = 0; i < region.fillLines.Count; i++)
                    {
                        Gizmos.DrawLine(region.fillLines[i].p0 * pixelToWorldScalar, region.fillLines[i].p1 * pixelToWorldScalar);
                    }
                }
            }



            //    float hue = 0;
            //    int contourCount = 0;
            //    foreach (Contour c in allContours)
            //    {
            //        Gizmos.color = Color.HSVToRGB(hue, 1, 1);
            //        Gizmos.DrawSphere(c.startPix * pixelToWorldScalar, debugPosScale);
            //        Gizmos.DrawSphere(c.endPix * pixelToWorldScalar, debugPosScale);

            //        for (int i = 0; i < c.lines.Count; i++)
            //        {
            //            Gizmos.DrawLine(c.lines[i].p0 * pixelToWorldScalar + (Vector2.right * contourCount * debugContourOffset), c.lines[i].p1 * pixelToWorldScalar + (Vector2.right * contourCount * debugContourOffset));
            //        }

            //        hue += 1f / (float)allContours.Count;
            //        contourCount++;
            //    }
            


            //if (debugDrawAllLines && lineList != null)
            //{
            //    Gizmos.color = Color.white * gizmoLineAlpha;
            //    for (int i = 0; i < lineList.Count; i++)
            //    {
            //        Gizmos.DrawLine(lineList[i].p0 * pixelToWorldScalar, lineList[i].p1 * pixelToWorldScalar);
            //    }
            //}
        }
    }
}