using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SVGGenerator
{
    [System.Serializable]
    public class TracedRegion
    {
        public bool debugDisable = false;
     
       

        [Header("SELECTION"),ColorUsage(false)]
        public Color col;
        public ImageValueSelectionType imageValueSelectionType = ImageValueSelectionType.Brightness;
        [Range(0, 1)]
        public float minRange = .4f;
        [Range(0, 1)]
        public float maxRange = .5f;
        [HideInInspector]
        public float prevMinRange = .4f, prevMaxRange = .5f;

        [Header("CONTOUR")]
        public ContourType contourType = ContourType.Min;
        public int contourGap = 0;
        public int samplingPixelStep = 1;


        [Header("FILL")]
        public FillType fillType = FillType.ScanLine;
        [Range(0, 100)]
        public int pixelRadiusLow = 4;
        [Range(0, 100)]
        public int pixelRadiusHigh = 20;


        


        [Header("SIMPLIFY")]
        public int minSampleOffset = 4;
        public int curvatureSampleCount = 10;
        [Tooltip("Larger > Smaller == More > Less detail")]
        public float curveCutoff = .5f;      
       

        [Header("GIZMO VISIBILITY")]
        public bool contourMinVisibility = true;
        public bool contourMaxVisibility = true;
        public bool fillLinesVisibility = true;

        [HideInInspector]
        public List<Contour> minContours = new List<Contour>();
        [HideInInspector]
        public List<Contour> maxContours = new List<Contour>();
        [HideInInspector]
        public Fill fill = new Fill();

        [HideInInspector]
        public float[,] regionValueMap;
        public PixelType[,] regionTypeMap;
        public Texture2D regionMapTex;
        public float areaInPixels;
        public Vector4 bounds;

        [HideInInspector]
        public Vector2[] neighborsClockwize = new Vector2[8]
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

        public void GenerateRegionMap(SVGExporter svgExporter)
        {
            regionValueMap = new float[svgExporter.tex.width, svgExporter.tex.height];

            int xMin = 9999;
            int xMax = -9999;
            int yMin = 9999;
            int yMax = -9999;
            int count = 0;

            // FIND VALUES IN RANGE AND STORE IN REGION MAP
            //
            for (int x = 1; x < svgExporter.tex.width-1; x++)
            {
                for (int y = 1; y < svgExporter.tex.height-1; y++)
                {
                    float val = svgExporter.GetValueAtPixel(imageValueSelectionType, x, y);
                    if (minRange < val && val < maxRange)
                    {
                        regionValueMap[x, y] = val;

                        xMin = Mathf.Min(x, xMin);
                        xMax = Mathf.Max(x, xMax);
                        yMin = Mathf.Min(y, yMin);
                        yMax = Mathf.Max(y, yMax);
                        count++;
                    }
                    else
                    {
                        regionValueMap[x, y] = 0;
                    }
                }
            }


            // BOUNDS, AREA, FLOOD FILL
            bounds = new Vector4(xMin, yMin, xMax, yMax);
            areaInPixels = count;
            //floodFillCount = 0;
            //floodFillUtil(regionMap, 10, 10, 0, 1);


            // GENERATE REGION TYPE MAP AND REGION TEX
            //
            regionTypeMap = new PixelType[regionValueMap.GetLength(0),regionValueMap.GetLength(1)];
            for (int x = 0; x < regionValueMap.GetLength(0); x++)
            {
                for (int y = 0; y < regionValueMap.GetLength(1); y++)
                {
                    if (regionValueMap[x, y] == 0)
                    {
                        regionTypeMap[x, y] = PixelType.NotInRegion;
                    }
                    // IF BORDER
                    else if (x > 0 && x < regionValueMap.GetLength(0) && y > 0 && y < regionValueMap.GetLength(1))
                    {
                        bool isBorder = false;
                        // IF ANY NEIGHBORS ARE OUT OF RANGE THEN IT'S A BORDER
                        foreach (Vector2 neighborOffset in neighborsClockwize)
                        {
                            if (regionValueMap[x + (int)neighborOffset.x, y + (int)neighborOffset.y] == 0)
                            {
                                isBorder = true;
                                regionTypeMap[x, y] = PixelType.Border;
                                break;
                            }
                        }

                        if(!isBorder)
                        {
                            regionTypeMap[x, y] = PixelType.InRegion;
                        }                       
                    }
                    else
                    {
                        regionTypeMap[x, y] = PixelType.InRegion;
                    }
                }
            }


            // DISCARD BORDERS NOT WITH NO ADJASCENT IN RANGE TYPE VALUES
            //
            for (int x = 1; x < regionTypeMap.GetLength(0) - 1; x++)
            {
                for (int y = 1; y < regionTypeMap.GetLength(1) - 1; y++)
                {
                    if (regionTypeMap[x, y] == PixelType.Border)
                    {
                        bool keep = false;
                        foreach(Vector2 neighborOffset in neighborsClockwize)
                        {
                            if(regionTypeMap[x + (int)neighborOffset.x, y + (int)neighborOffset.y] == PixelType.InRegion)
                            {
                                keep = true;                               
                                break;
                            }
                        }

                        if(!keep)
                        {
                            regionTypeMap[x, y] = PixelType.NotInRegion;
                            regionValueMap[x, y] = 0;
                        }
                    }
                }
            }


            // GENRATE TEXTURE FROM REGION MAP
            //
            regionMapTex = new Texture2D
            (
                regionValueMap.GetLength(0),
                regionValueMap.GetLength(1),
                TextureFormat.ARGB32,
                true
            );

            for (int x = 0; x < regionValueMap.GetLength(0); x++)
            {
                for (int y = 0; y < regionValueMap.GetLength(1); y++)
                {
                    switch(regionTypeMap[x, y])
                    {
                        case PixelType.NotInRegion:
                            regionMapTex.SetPixel(x, y, Color.black);
                            break;
                        case PixelType.InRegion:
                            regionMapTex.SetPixel(x, y, Color.yellow);
                            break;
                        case PixelType.Border:
                            regionMapTex.SetPixel(x, y, Color.blue);
                            break;
                    }
                }
            }

            regionMapTex.Apply();
        }

        //int maxFloodFill = 10000;
        //int floodFillCount = 0;
        //void floodFillUtil(float[,] screen,
        //                    int x, int y,
        //                    float prevVal, float newVal)
        //{
        //    // Base cases
        //    if (x < 0 || x >= screen.GetLength(0) ||
        //        y < 0 || y >= screen.GetLength(0))
        //        return;
        //    if (screen[x, y] != prevVal)
        //        return;

        //    // Replace the color at (x, y)
        //    screen[x, y] = newVal;
        //    floodFillCount++;

        //    if (floodFillCount < maxFloodFill)
        //    {
        //        // Recur for north, east, south and west
        //        floodFillUtil(screen, x + 1, y, prevVal, newVal);
        //        floodFillUtil(screen, x - 1, y, prevVal, newVal);
        //        floodFillUtil(screen, x, y + 1, prevVal, newVal);
        //        floodFillUtil(screen, x, y - 1, prevVal, newVal);
        //    }
        //}

        public void Trace(SVGExporter svgExporter)
        {
            if (debugDisable)
                return;

            GenerateRegionMap(svgExporter);

            minContours.Clear();
            maxContours.Clear();
            fill.ResetFill();

            switch (contourType)
            {
                case ContourType.None:
                    break;
                case ContourType.Min:                    
                    TraceContours(svgExporter, minContours, minRange);
                    break;
                case ContourType.Max:
                    TraceContours(svgExporter, maxContours, maxRange);
                    break;
                case ContourType.MinMax:
                    TraceContours(svgExporter, minContours, minRange);
                    TraceContours(svgExporter, maxContours, maxRange);
                    break;
            }

            fill.GenerateFill(fillType, svgExporter, this);


            Debug.Log($"Region trace complete - Contours: {minContours.Count + maxContours.Count}  Fill lines: {fill.fillLines.Count}");
        }

        private void TraceContours(SVGExporter svgExporter, List<Contour> contourList, float valueCutoff)
        {
            int[,] contourSampleCounts = new int[svgExporter.tex.width, svgExporter.tex.height];
            int debugContourSampleCount;
            int debugMaxSamplesPerContour = 40000;
            int contourCount = 0;
            int maxContourSampleCount = 2;

            bool inInRegion = false;
            int xSamples = svgExporter.tex.width;// Mathf.FloorToInt(svgExporter.tex.width / samplingPixelStep);
            int ySamples = svgExporter.tex.height;// Mathf.FloorToInt(svgExporter.tex.height / samplingPixelStep);

            for (int x = 0; x < xSamples; x++)
            {
                for (int y = 0; y < ySamples; y++)
                {
                    if (contourCount >= svgExporter.maxContours)
                    {
                        Debug.Log("*************MAX CONTOURS REACHED");
                        return;
                    }

                    int xSample = x;// * samplingPixelStep;
                    int ySample = y;// * samplingPixelStep;
                    svgExporter.debugPos = new Vector2(x, y);

                    bool prevIsBorderPix = inInRegion;                 
                    inInRegion = regionValueMap[x, y] != 0;

                    // FIND PIXEL THAT CROSSES A BORDER
                    //
                    if (inInRegion && 
                        !prevIsBorderPix &&
                        contourSampleCounts[xSample, ySample] == 0)
                    {
                        debugContourSampleCount = 0;

                        // TRACE CONTOUR
                        //
                        Vector2 startPix = new Vector2(xSample, ySample);
                        Vector2 currentContourPix = new Vector2(xSample, ySample);
                        Contour newContour = new Contour() { startPix = startPix };
                        contourSampleCounts[xSample, ySample]++;
                        Line newLine = new Line() { p0 = startPix, newLine = true };


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
                                if (svgExporter.debugPrint) Debug.Log(" ENDING CONTOUR: Max samples per contour reached");
                                break;
                            }


                            // GET NEIGHBOR COORDS, EITHER CLOCK OR ANTI CLOCKWISE
                            //
                            neighborIndex %= neighborsClockwize.Length;
                            int searchX, searchY;
                            searchX = (int)currentContourPix.x + (int)neighborsClockwize[neighborIndex].x;// * samplingPixelStep;
                            searchY = (int)currentContourPix.y + (int)neighborsClockwize[neighborIndex].y;// * samplingPixelStep;


                            // IF OUTSIDE OF THE IMAGE BOUNDS THEN CONT TO NEXT NEIGHBOR
                            //
                            if (searchX < 0 || searchY < 0 || searchX == svgExporter.tex.width || searchY == svgExporter.tex.height)
                                continue;


                            // CHECK TO SEE IF THIS PIXEL HAS BEEN SEARCHED BEFORE
                            //
                            if (contourSampleCounts[searchX, searchY] >= maxContourSampleCount)
                            {
                                if (svgExporter.debugPrint) Debug.Log("********  ENDING CONTOUR:    Sampling contour pixel more than twice, Breaking");
                                break;
                            }

                            // CHECK TO SEE IF THE SAMPLED PIXEL IS START OR END OF PREV CONTOUR
                            //
                            foreach (Contour c in contourList)
                            {
                                if (c.startPix == new Vector2(searchX, searchY) || c.endPix == new Vector2(searchX, searchY))
                                {
                                    if (svgExporter.debugPrint) Debug.Log("********  ENDING CONTOUR:    Sampled start or end of other contour");
                                    break;
                                }
                            }

                            // SET FOUND PIXEL TO CURRENT POS AND ITERATE
                            //
                            if (regionValueMap[searchX, searchY] == 0)
                            {
                                if (svgExporter.debugPrint) Debug.Log($"-------   Found contour: {searchX} {searchY} @   nIndex:  {neighborIndex}");

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

                                contourSampleCounts[searchX, searchY]++;

                                currentContourPix = newContourPixel;
                                newLine = new Line() { p0 = currentContourPix, newLine = false };

                                newContour.endPix = currentContourPix;

                                if (currentContourPix == startPix)
                                {
                                    if (svgExporter.debugPrint) Debug.Log("ENDING CONTOUR:  BACK TO START END CONTOUR");
                                    break;
                                }
                            }
                            else
                            {
                                neighborIndex++;
                            }

                            if (i == 7 && svgExporter.debugPrint)
                                Debug.Log("ENDING CONTOUR:  NO NEIGHBORS FOUND");
                        }

                        // ADD CONTOUR TO LIST
                        //                       
                        if (newContour.lines.Count > 0)
                        {
                            newContour.ResetProcessedLines();                            
                            contourList.Add(newContour);
                            contourCount++;
                        }

                        if (svgExporter.debugPrint)
                            Debug.Log($"Added new contour. Line count: {newContour.lines.Count}");
                    }
                }
            }

            if (svgExporter.debugPrint) Debug.Log("*************END OF TEXTURE SEARCH REACHED");
        }

        public void ProcessContours()
        {
            if (contourGap <= 0)
                return;

            foreach(Contour c in minContours)
            {
                c.ProcessContours(contourGap, true);
            }

            foreach (Contour c in maxContours)
            {
                c.ProcessContours(contourGap, true);
            }
        }

        public void SimplifyContours()
        {           
            foreach (Contour c in minContours)
            {
                c.Simplify(minSampleOffset, curvatureSampleCount, curveCutoff);
            }

            foreach (Contour c in maxContours)
            {
                c.Simplify(minSampleOffset, curvatureSampleCount, curveCutoff);
            }            
        }
    }
}