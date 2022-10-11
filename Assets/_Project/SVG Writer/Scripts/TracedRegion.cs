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


        [Header("FILL")]
        public FillType fillType = FillType.ScanLine;
        [Range(0, 1)]
        public float fillDensityLow = .3f;
        [Range(0, 1)]
        public float fillDensityHigh = .6f;


        


        [Header("SIMPLIFY")]
        public int minSampleOffset = 3;
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
        public float[,] regionMap;
        public float areaInPixels;
        public Vector4 bounds;

        public void GenerateRegionMap(SVGExporter svgExporter)
        {
            regionMap = new float[svgExporter.tex.width, svgExporter.tex.height];

            int xMin = 9999;
            int xMax = -9999;
            int yMin = 9999;
            int yMax = -9999;
            int count = 0;

            for (int x = 0; x < svgExporter.tex.width; x++)
            {
                for (int y = 0; y < svgExporter.tex.height; y++)
                {
                    float val = svgExporter.GetValueAtPixel(imageValueSelectionType, x, y);
                    if (minRange < val && val < maxRange)
                    {
                        regionMap[x, y] = val;

                        xMin = Mathf.Min(x, xMin);
                        xMax = Mathf.Max(x, xMax);
                        yMin = Mathf.Min(y, yMin);
                        yMax = Mathf.Max(y, yMax);
                        count++;
                    }
                    else
                    {
                        regionMap[x, y] = 0;
                    }
                }
            }

            bounds = new Vector4(xMin, yMin, xMax, yMax);
            areaInPixels = count;
        }

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
            int[,] contourPixels = new int[svgExporter.tex.width, svgExporter.tex.height];
            int debugContourSampleCount;
            int debugMaxSamplesPerContour = 40000;
            int contourCount = 0;
            int maxContourSampleCount = 2;

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



            bool isBorderPixel = svgExporter.GetValueAtPixel(imageValueSelectionType, 0, 0) < valueCutoff;
            for (int x = 0; x < svgExporter.tex.width; x++)
            {
                for (int y = 0; y < svgExporter.tex.height; y++)
                {
                    if (contourCount >= svgExporter.maxContours)
                    {
                        Debug.Log("*************MAX CONTOURS REACHED");
                        return;
                    }

                    svgExporter.debugPos = new Vector2(x, y);

                    bool prevIsBorderPix = isBorderPixel;
                    isBorderPixel = svgExporter.GetValueAtPixel(imageValueSelectionType, x, y) < valueCutoff; ;

                    //
                    // FIND PIXEL THAT CROSSES A BORDER
                    //
                    if (isBorderPixel && !prevIsBorderPix && svgExporter.GetValueAtPixel(imageValueSelectionType, x, y) < valueCutoff &&
                        contourPixels[x, y] == 0)
                    {
                        debugContourSampleCount = 0;

                        // TRACE CONTOUR
                        //
                        Vector2 startPix = new Vector2(x, y);
                        Vector2 currentContourPix = new Vector2(x, y);

                        Contour newContour = new Contour() { startPix = startPix };

                        bool clockwise = svgExporter.startClockwize;

                        contourPixels[x, y]++;
                        Line newLine = new Line() { p0 = startPix, newLine = true };

                        // DEBUG
                        //                  
                        if (svgExporter.debugPrint) Debug.Log("*************FOUND BORDER PIXEL: " + currentContourPix);

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
                            searchX = clockwise ? (int)currentContourPix.x + (int)neighborsClockwize[neighborIndex].x : (int)currentContourPix.x + (int)neighborsAntiClockwize[neighborIndex].x;
                            searchY = clockwise ? (int)currentContourPix.y + (int)neighborsClockwize[neighborIndex].y : (int)currentContourPix.y + (int)neighborsAntiClockwize[neighborIndex].y;



                            // IF OUTSIDE OF THE IMAGE BOUNDS THEN CONT TO NEXT NEIGHBOR
                            //
                            if (searchX < 0 || searchY < 0 || searchX == svgExporter.tex.width || searchY == svgExporter.tex.height)
                                continue;


                            // CHECK TO SEE IF THIS PIXEL HAS BEEN SEARCHED BEFORE
                            //
                            if (contourPixels[searchX, searchY] >= maxContourSampleCount)
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
                            if (svgExporter.GetValueAtPixel(imageValueSelectionType, searchX, searchY) < valueCutoff)
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

                                contourPixels[searchX, searchY]++;

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
                        newContour.processedLines = newContour.lines;
                        contourList.Add(newContour);
                        if (svgExporter.debugPrint)
                            Debug.Log($"Added new contour. Line count {newContour.processedLines.Count}  {newContour.lines.Count}");

                        contourCount++;
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