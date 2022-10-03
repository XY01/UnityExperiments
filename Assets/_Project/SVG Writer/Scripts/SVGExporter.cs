using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// Sample percieved luminance - DONE
// Create shader to be able to better select ranges - DONE
// Move all get pixels to get from a cached pixel array - DONE
// Show original image on the left same size as current - DONE
// Designate value region - DONE
// Add reagion by hue, value, saturation - DONE


// Test distance from line calc
// Try sampling filtered pixels and using UV spaced stepping to get less pixelated lines
// Set HSV select from region in shader in on validate
// Stippling, dash line styles
// Try draw a path along a line where contour lines are near parallell
// Simplify lines, straighten lines by taking angle to next lines end point and if not over x angle then move end point



// Create shader to mimick final output
// Show histogram of image to allow for selection of range visually


namespace SVGGenerator
{
    #region MINOR CLASSES
    /// <summary>
    /// Holds start and end position for a straight line section as well as an instruction to start the line at the first position
    /// </summary>
    public struct Line
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


    public enum RegionSelectionType
    {
        Hue,
        Value,
        Saturation
    }

    [System.Serializable]
    public class TracedRegion
    {
        public RegionSelectionType regionSelectionType = RegionSelectionType.Value;

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


        //[HideInInspector]
        public List<Contour> minContours = new List<Contour>();
        //[HideInInspector]
        public List<Contour> maxContours = new List<Contour>();

        //[HideInInspector]
        public List<Line> fillLines = new List<Line>();

        public void Trace(SVGExporter svgExporter)
        {
            if (debugDisable)
                return;

            minContours.Clear();
            maxContours.Clear();
            fillLines.Clear();

            if (generateMinContour)
            {
                svgExporter.TraceContour(this, minContours, minRange);
            }

            if (generateMaxContour)
            {
                svgExporter.TraceContour(this, maxContours, maxRange);
            }

            if (generateScanLines)
                fillLines = svgExporter.TraceScaneLine(this);


            Debug.Log($"Region trace complete - Contours: {minContours.Count + maxContours.Count}  Fill lines: {fillLines.Count}");
        }
    }
    #endregion


    public class SVGExporter : MonoBehaviour
    {
        #region VARIABLES       
        public Texture2D tex;
        private const float pixelToWorldScalar = .01f;

        public Transform imageQuad;
        public Material imageQuadMat;
        public Transform imageQuad_Original;
        public Material imageQuadMat_Original;


        [Header("CONTOURS")]
        public int maxContours = 500;
        public bool startClockwize = true;
        private int maxContourSampleCount = 2;


        [Header("REGIONS")]
        public TracedRegion[] tracedRegions;
              

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

        #endregion


       
        //
        // VECTOR GENERATION FUNCTIONS
        //
        public void TraceContour(TracedRegion region, List<Contour> contourList, float valueCutoff)
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


            bool isBorderPixel = GetValueAtPixel(region.regionSelectionType, 0, 0) < valueCutoff;
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
                    isBorderPixel = GetValueAtPixel(region.regionSelectionType, x, y) < valueCutoff; ;

                    //
                    // FIND PIXEL THAT CROSSES A BORDER
                    //
                    if (isBorderPixel && !prevIsBorderPix && GetValueAtPixel(region.regionSelectionType, x, y) < valueCutoff &&
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
                            if (GetValueAtPixel(region.regionSelectionType, searchX, searchY) < valueCutoff)
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

        public List<Line> TraceScaneLine(TracedRegion region)
        {
            List<Line> newLines = new List<Line>();

            int yIncrement = (int)Mathf.Lerp(tex.height * .1f, 1, Mathf.Clamp01(region.scanLineDensity));
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
                    float valueSample = GetValueAtPixel(region.regionSelectionType, x, yPixIndex);

                    //print($"{x}  {yPixIndex}    valueSample {valueSample}     valueRange {valueRange}      drawingLine    {drawingLine}");

                    if (!drawingLine)
                    {
                        if (valueSample > region.minRange && valueSample < region.maxRange)
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
                        if (valueSample < region.minRange || valueSample > region.maxRange || x == tex.width - 1)
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
        }



        //
        // HELPER FUNCTIONS
        //
        private float GetValueAtPixel(RegionSelectionType selectionType, int x, int y)
        {
            float h, s, v;


            // DISCRETE PIXEL SAMPLING
            //
            //Color.RGBToHSV(pixelCols[x + y * tex.width], out h, out s, out v);


            // BILNEAR SAMPLING
            //
            Color col = tex.GetPixelBilinear((float)(x / (float)tex.width), (float)(y / (float)tex.height));
            Color.RGBToHSV(col, out h, out s, out v);

            switch (selectionType)
            {
                case RegionSelectionType.Hue:
                    return h;
                case RegionSelectionType.Saturation:
                    return s;
                case RegionSelectionType.Value:
                    return v;
                default:
                    return v;
            }
        }

        public Vector2 FindNearestPointOnLine(Vector2 origin, Vector2 direction, Vector2 point)
        {
            direction.Normalize();
            Vector2 lhs = point - origin;

            float dotP = Vector2.Dot(lhs, direction);
            return origin + direction * dotP;
        }

        public Vector2 FindNearestPointOnDiscreteLine(Vector2 origin, Vector2 end, Vector2 point)
        {
            //Get heading
            Vector2 heading = (end - origin);
            float magnitudeMax = heading.magnitude;
            heading.Normalize();

            //Do projection from the point but clamp it
            Vector2 lhs = point - origin;
            float dotP = Vector2.Dot(lhs, heading);
            dotP = Mathf.Clamp(dotP, 0f, magnitudeMax);
            return origin + heading * dotP;
        }

        public Transform start;
        public Transform end;
        public Transform point;

        float DistanceOfPointFromLine(Vector2 origin, Vector2 end, Vector2 point)
        {
            float a = Vector2.Distance(origin, end);
            float b = Vector2.Distance(origin, point);
            float c = Vector2.Distance(end, point);
            float s = (a + b + c) / 2;
            float distance = 2 * Mathf.Sqrt(s * (s - a) * (s - b) * (s - c)) / a;

            return distance;
        }


        private void OnValidate()
        {
            foreach (TracedRegion region in tracedRegions)
            {
                if (region.minRange != region.prevMinRange)
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

        [ContextMenu("Simplify")]
        void SimplifyRegions()
        {
            foreach(TracedRegion region in tracedRegions)
            {
                foreach(Contour c in region.minContours)
                {
                    if (c.lines.Count > 0)
                    {
                        //c.lines = SimplifyContour(c);
                        c.lines = AssessCurvature(c);
                    }
                }

                foreach (Contour c in region.maxContours)
                {
                    if (c.lines.Count > 0)
                    {
                        //c.lines = SimplifyContour(c);
                        c.lines = AssessCurvature(c);
                    }
                }
            }
        }

        [Header("Simplify")]
        public int minSampleOffset = 3;
        public int curvatureSampleCount = 10;
        [Tooltip("Larger > Smaller == More > Less detail")]
        public float curveCutoff = .5f;
        List<Vector3> curvatureAtPoint = new List<Vector3>();
        List<Vector3> curvatureDerivedPoints = new List<Vector3>();
        //https://answers.unity.com/questions/1368390/how-to-calculate-curvature-of-a-path.html

      
        List<Line> AssessCurvature(Contour c)
        {
            List<Line> simplifiedContour = new List<Line>();
            curvatureAtPoint = new List<Vector3>();           


            int currentIndex = 0;
            int count = 0;
            //while (currentIndex < c.lines.Count)
            {
                for (int x = minSampleOffset; x <= curvatureSampleCount; x++)
                {
                    // Start at point, check curvature of next few points                    
                    int middleIndex = currentIndex + x;
                    int endIndex = currentIndex + x * 2;
                  
                    if(endIndex >= c.lines.Count)
                    {
                        simplifiedContour.Add(new Line() { p0 = c.lines[currentIndex].p0, p1 = c.lines[0].p0 });
                        break;
                    }

                    float curvature = CurvatureFrom3Points(c.lines[currentIndex].p0, c.lines[middleIndex].p0, c.lines[endIndex].p0);
                    if (curvature < curveCutoff || x == curvatureSampleCount || endIndex >= c.lines.Count)
                    {
                        //print("added " + curvature);
                        simplifiedContour.Add(new Line() { p0 = c.lines[currentIndex].p0, p1 = c.lines[endIndex].p0 });
                        curvatureAtPoint.Add(new Vector3(c.lines[currentIndex].p0.x, c.lines[currentIndex].p0.y, .5f + curvature));
                        curvatureAtPoint.Add(new Vector3(c.lines[endIndex].p0.x, c.lines[endIndex].p0.y, .5f + curvature));
                        currentIndex = endIndex;
                        x = minSampleOffset;
                        count++;
                        if (count > 1000 || currentIndex >= c.lines.Count)
                            break;                    
                    }                  
                }
            }

            return simplifiedContour;
        }



        /**
         * Finds the center of the circle passing through the points p1, p2 and p3.
         * (NaN, NaN) will be returned if the points are colinear.
         */

        float CurvatureFrom3Points(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            var cc = CircleCenterFrom3Points(p1, p2, p3);
            var radius = (cc - p1).magnitude;
            radius = Mathf.Max(radius, 0.0001f);
            return 1f/radius;
        }

        static Vector2 CircleCenterFrom3Points(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float temp = p2.sqrMagnitude;
            float bc = (p1.sqrMagnitude - temp) / 2.0f;
            float cd = (temp - p3.sqrMagnitude) / 2.0f;
            float det = (p1.x - p2.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p2.y);
            if (Mathf.Abs(det) < 1.0e-6)
            {
                return new Vector2(float.NaN, float.NaN);
            }
            det = 1 / det;
            return new Vector2((bc * (p2.y - p3.y) - cd * (p1.y - p2.y)) * det, ((p1.x - p2.x) * cd - (p2.x - p3.x) * bc) * det);
        }

        float PointDistFromLine(Vector2 startp, Vector2 endp, Vector2 p)
        {
            float a = (startp - endp).magnitude;
            float b = (startp - p).magnitude;
            float c = (endp - p).magnitude;
            float s = (a + b + c) / 2f;
            float distance = 2 * Mathf.Sqrt(s * (s - a) * (s - b) * (s - c)) / a;
            return distance;
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

            foreach(Vector3 curveaturePoint in curvatureAtPoint)
            {
                //Gizmos.color = Color.yellow * curveaturePoint.z;
                Gizmos.DrawSphere(new Vector3(curveaturePoint.x, curveaturePoint.y, 0) * pixelToWorldScalar, (.1f + curveaturePoint.z) * debugPosScale);
            }

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