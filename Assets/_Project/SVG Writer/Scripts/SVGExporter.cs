using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SVGExporter : MonoBehaviour
{
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

    //
    // VARIABLES
    //
    public Texture2D tex;
    public float pixelToWorldScalar = .01f;

    public Transform imageQuad;
    public Material imageQuadMat;

    public Vector2[] valueRanges = new Vector2[2] {new Vector2(0,.5f), new Vector2(.5f, 1) };

    [Header("SCAN LINE")]
    public bool generateScanLines = false;
    public float[] densityLevels = new float[2] { .9f, .1f };

    [Header("CONTOURS")]
    public bool generateContours = true;
    public int maxContours = 1;
    public float contourCutoff = .5f;
    public List<Contour> contours = new List<Contour>();
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
    private List<Line> lineList = new List<Line>();


    //
    // VECTOR GENERATION FUNCTIONS
    //
    public void ContourTrace(float valueCutoff)
    {       
        int[,] contourPixels = new int[tex.width, tex.height];
        int debugContourSampleCount;
        int debugMaxSamplesPerContour = 40000;

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

        Color[] pixelCols = tex.GetPixels();     
        bool isBorderPixel = pixelCols[0].r < valueCutoff;
        for (int x = 0; x < tex.width; x++)
        {
            for (int y = 0; y < tex.height; y++)
            {
                if (contours.Count >= maxContours)
                {
                    if (debugPrint) Debug.Log("*************MAX CONTOURS REACHED");                  
                    return;
                }

                debugPos = new Vector2(x, y);

                bool prevIsBorderPix = isBorderPixel;
                isBorderPixel = pixelCols[x + y * tex.width].r < valueCutoff; ;

                //
                // FIND PIXEL THAT CROSSES A BORDER
                //
                if (isBorderPixel && !prevIsBorderPix && pixelCols[x + y * tex.width].r < valueCutoff &&
                    contourPixels[x, y] == 0)
                {
                    debugContourSampleCount = 0;
                    int maxPrevPixelSamples = 4;
                    int prevPixelSampleCounter = 0;

                    // TRACE CONTOUR
                    //
                    Vector2 startPix = new Vector2(x, y);
                    Vector2 prevContourPix = new Vector2(x, y);
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
                        foreach(Contour c in contours)
                        {
                            if(c.startPix == new Vector2(searchX, searchY) || c.endPix == new Vector2(searchX, searchY))
                            {
                                if (debugPrint) Debug.Log("********  ENDING CONTOUR:    Sampled start or end of other contour");
                                break;
                            }
                        }

                        // SET FOUND PIXEL TO CURRENT POS AND ITERATE
                        //
                        if (tex.GetPixel(searchX, searchY).r < valueCutoff)
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
                            lineList.Add(newLine);
                            newContour.lines.Add(newLine);

                            contourPixels[searchX, searchY]++;

                          

                            prevContourPix = currentContourPix;
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


                 

                    
                    // ADD CONTOUR TO LIST
                    //
                    if (debugPrint) Debug.Log("ADDING NEW CONTOUR");
                    contours.Add(newContour);                    
                }
            }            
        }

        if (debugPrint) Debug.Log("*************END OF TEXTURE SEARCH REACHED");
    }
     
    void TextureScanLines(Vector2 valueRange, float denistyNorm)
    {
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
                if(drawingLine && x == tex.width-1)// || lineDist > maxLineLength)
                {
                    EndLine(x, yPixIndex);
                }

                //
                // IF DARK ENOUGH, START DRAWING LINE
                //
                float valueSample = tex.GetPixel(x, yPixIndex).r;// tex.GetPixelBilinear(uPos, vPos).r;

                //print($"{x}  {yPixIndex}    valueSample {valueSample}     valueRange {valueRange}      drawingLine    {drawingLine}");

                if (!drawingLine)
                {
                    if (valueSample > valueRange.x && valueSample < valueRange.y)
                    {                       
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
                    if (valueSample < valueRange.x || valueSample > valueRange.y || x == tex.width-1)
                    {
                        EndLine(x-1, yPixIndex);                       
                    }
                }
            }
        }

        void EndLine(int x, int y)
        {
            // END LINE
            //
            drawingLine = false;

            if(new Vector2(x, y) != newLine.p0)
            {
                newLine.p1 = new Vector2(x, y);
                lineList.Add(newLine);
            }          
        }
    }

    void GenerateRandomLines()
    {
        lineList = new List<Line>();
        float radius = 400;
        for (int i = 0; i < 100; i++)
        {
            Line newLine = new Line()
            {
                p0 = pixelMidpoint + Random.insideUnitCircle * radius,
                p1 = pixelMidpoint + Random.insideUnitCircle * radius
            };

            lineList.Add(newLine);
        }
    }



    //
    // CREATE & WRITE VECTOR TO FILE
    //

    [ContextMenu("Test")]
    public void WriteString()
    {
        string path = "Assets/_Project/SVG Writer/Resources/testSVG " + System.DateTime.Now.ToString("yyyy-MM-dd") + ".svg";

        lineList = new List<Line>();
        contours = new List<Contour>();

       
        if (generateContours)
        {
            for (int i = 0; i < valueRanges.Length; i++)
            {
                ContourTrace(valueRanges[i].x);
            }
        }

        if (generateScanLines)
        {
            for (int i = 0; i < valueRanges.Length; i++)
            {
                TextureScanLines(valueRanges[i], densityLevels[i]);
            }
        }


        string testSVGStringHeader = @"<svg width=""800"" height=""800""    xmlns:xlink=""http://www.w3.org/1999/xlink"" style=""stroke:black; stroke-opacity:1; stroke-width:1;""  xmlns=""http://www.w3.org/2000/svg""> <defs id = ""genericDefs""/>
                                        <g> <path style = ""fill:none;"" d = """;

        for (int i = 0; i < lineList.Count; i++)
        {
            if(i == 0 || lineList[i].newLine)
                testSVGStringHeader += $"   M {lineList[i].p0.x} {lineList[i].p0.y}  L {lineList[i].p1.x} {lineList[i].p1.y}";
            else
                testSVGStringHeader += $"   L {lineList[i].p0.x} {lineList[i].p0.y}  L {lineList[i].p1.x} {lineList[i].p1.y}";
        }

        testSVGStringHeader += @"""/> </g> </svg>";
        
        
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

        // DRAW FRAME
        //
        Vector3 topLeft = Vector3.up * tex.height * pixelToWorldScalar;        
        Vector3 btmRight = Vector3.right * tex.height * pixelToWorldScalar;
        Vector3 topRight = topLeft + btmRight;
        Gizmos.DrawLine(Vector3.zero, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, btmRight);
        Gizmos.DrawLine(btmRight, Vector3.zero);

        // POSITION IMAGE QUAD
        //
        imageQuad.transform.position = pixelMidpointWorldSpace;
        imageQuad.transform.localScale = new Vector3(tex.width * pixelToWorldScalar, tex.height * pixelToWorldScalar);


        Gizmos.DrawSphere(debugPos * pixelToWorldScalar, debugPosScale);

        // DRAW CONTOUR START AND ENDS
        //
        if (debugDrawContours)
        {
            float hue = 0;
            int contourCount = 0;
            foreach (Contour c in contours)
            {
                Gizmos.color = Color.HSVToRGB(hue, 1, 1);
                Gizmos.DrawSphere(c.startPix * pixelToWorldScalar, debugPosScale);
                Gizmos.DrawSphere(c.endPix * pixelToWorldScalar, debugPosScale);

                for (int i = 0; i < c.lines.Count; i++)
                {
                    Gizmos.DrawLine(c.lines[i].p0 * pixelToWorldScalar + (Vector2.right * contourCount * debugContourOffset), c.lines[i].p1 * pixelToWorldScalar + (Vector2.right * contourCount * debugContourOffset));
                }

                hue += 1f / (float)contours.Count;
                contourCount++;
            }
        }


        if (debugDrawAllLines && lineList != null)
        {
            Gizmos.color = Color.white * gizmoLineAlpha;
            for (int i = 0; i < lineList.Count; i++)
            {
                Gizmos.DrawLine(lineList[i].p0 * pixelToWorldScalar, lineList[i].p1 * pixelToWorldScalar);
            }
        }
    }
}
