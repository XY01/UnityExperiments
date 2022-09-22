using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SVGExporter : MonoBehaviour
{
    struct Line
    {
        public bool newLine;
        public Vector2 p0;
        public Vector2 p1;
    }

    List<Line> lineList = new List<Line>();

    int width = 800;
    int height = 800;

    Vector2 midPoint => new Vector2(width * .5f, height * .5f);

    public Texture2D tex;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public Vector3 debugPos;
    public void ContourTrace()
    {
        bool[,] checkedPixels = new bool[tex.width, tex.height];
        bool[,] hasNode = new bool[tex.width, tex.height];
        float contourCutoff = .5f;

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

        Color[] pixelCols = tex.GetPixels();
        int contourCount = 0;

        for (int x = 1; x < tex.width; x++)
        {
            for (int y = 1; y < tex.height; y++)
            {
                if (contourCount > 5)
                {
                    print("here 2");
                    return;
                }

                // FIND PIXEL THAT CROSSES A BORDER
                //
                if (pixelCols[x + y * tex.width].r < contourCutoff)
                {
                    // TRACE CONTOUR
                    //
                    Vector2 startPix = new Vector2(x, y);
                    Vector2 currentContourPix = new Vector2(x, y);
                    bool endContourSearchConditionMet = false;

                    hasNode[x, y] = true;
                    Line newLine = new Line() { p0 = startPix, newLine = true };


                    // DEBUG
                    //
                    Debug.Log("*************Found border pixel: " + currentContourPix);
                    debugPos = startPix;

                    print("here 0");
                    while (!endContourSearchConditionMet)
                    {
                              
                        print("here 1");
                        for (int j = 0; j < 90; j++)
                        {

                            // SEARCH ADJASCENT PIXELS CLOCKWISE UNTIL FINDING A BLACK PIXEL THAT COMES AFTER A WHITE PIXEL
                            //                 
                            bool foundContourPixelInNeighbors = false;
                            int neighborIndex = 0;
                            for (int i = 0; i < neighborsClockwize.Length; i++)
                            {
                                neighborIndex %= neighborsClockwize.Length;
                                int searchX = (int)currentContourPix.x + (int)neighborsClockwize[neighborIndex].x;
                                int searchY = (int)currentContourPix.y + (int)neighborsClockwize[neighborIndex].y;

                                if (searchX < 0 || searchY < 0 || searchX == tex.width || searchY == tex.height)
                                    continue;

                                print($"Searching: {searchX} {searchY} @   nIndex:  {neighborIndex}");

                                // SET FOUND PIXEL TO CURRENT POS AND ITERATE
                                //
                                if (tex.GetPixel(searchX, searchY).r < contourCutoff)
                                {
                                    print($"FOUND: {searchX} {searchY} @   nIndex:  {neighborIndex}");

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
                                   
                                    foundContourPixelInNeighbors = true;
                                    Vector2 newContourPixel = new Vector2(searchX, searchY);
                                    newLine.p1 = newContourPixel;
                                    lineList.Add(newLine);

                                    

                                    currentContourPix = newContourPixel;
                                    newLine = new Line() { p0 = currentContourPix, newLine = false };

                                    if (currentContourPix == startPix)
                                    {
                                        endContourSearchConditionMet = true;
                                        contourCount++;
                                        print("BACK TO START END CONTOUR");
                                    }
                                }
                                else
                                {
                                    neighborIndex++;
                                }
                            }
                            contourCount++;

                            if (!foundContourPixelInNeighbors)
                            {
                                print("No neighbor pixel found");
                                endContourSearchConditionMet = true;
                            }
                        }
                        endContourSearchConditionMet = true;
                    }  
                    
                }
            }            
        }
    }

    public int uSampleCount = 1000;
    public int vSampleCount = 100;
    public float[] cutoffLevels = new float[2] { .9f, .5f };
    public float[] maxLineLengths = new float[2] { 1, 1 };
    void TextureScanLines(int uCount, int vCount, float[] cutoffLevels)
    {
        int uSampleSteps = uCount;
        float uincrement = 1f / (float)(uSampleSteps - 1);

        float vScanLineCount = vCount;
        float vIncrement = 1f/(float)(vScanLineCount-1);

        float vPos;
        float uPos;
        bool drawingLine = false;
        Line newLine = new Line();

        float scalar = 800;
        float selectedCutoff;
        float maxLineLength;
        float lineDist = 0;

        for (int i = 0; i < vScanLineCount; i++)
        {
            vPos = i * vIncrement;
            selectedCutoff = cutoffLevels[i% cutoffLevels.Length];
            maxLineLength = maxLineLengths[i % cutoffLevels.Length];

            for (int j = 0; j < uSampleSteps; j++)
            {
                uPos = j * uincrement;              

                if(drawingLine && uPos == 1)// || lineDist > maxLineLength)
                {
                    EndLine();
                }

                //
                // IF DARK ENOUGH, START DRAWING LINE
                //
                float valueSample = tex.GetPixelBilinear(uPos, vPos).r;
               
                if (!drawingLine)
                {
                    if (valueSample < selectedCutoff)
                    {                       
                        // START NEW LINE
                        //
                        drawingLine = true;
                        lineDist = 0;
                        newLine = new Line() { p0 = new Vector2(uPos, vPos) * scalar, newLine = true };
                    }
                }
                //
                // IF NOT DARK ENOUGH AND DRAWING A LINE, FINISH LINE AND ADD TOO LINE LIST
                //
                else
                {
                    if (valueSample > selectedCutoff)
                    {
                        EndLine();                       
                    }
                }
            }

            lineDist += uincrement;
        }

        void EndLine()
        {
            // END LINE
            //
            drawingLine = false;
            newLine.p1 = new Vector2(uPos, vPos) * scalar;
            lineList.Add(newLine);
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
                p0 = midPoint + Random.insideUnitCircle * radius,
                p1 = midPoint + Random.insideUnitCircle * radius
            };

            lineList.Add(newLine);
        }
    }

    [ContextMenu("Test")]
    public void WriteString()
    {
        string path = "Assets/_Project/SVG Writer/Resources/testSVG " + System.DateTime.Now.ToString("yyyy-MM-dd") + ".svg";

        lineList = new List<Line>();
        //GenerateRandomLines();
        //TextureScanLines(uSampleCount, vSampleCount, cutoffLevels);
        //TextureScanLines(uSampleCount, (int)(vSampleCount * .5f), .5f);
        //TextureScanLines(uSampleCount, (int)(vSampleCount * .25f), .9f);

        ContourTrace();

        string testSVGStringHeader = @"<svg width=""800"" height=""800""    xmlns:xlink=""http://www.w3.org/1999/xlink"" style=""stroke:black; stroke-opacity:1; stroke-width:1;""  xmlns=""http://www.w3.org/2000/svg""> <defs id = ""genericDefs""/>
                                        <g> <path style = ""fill:none;"" d = """;

        for (int i = 0; i < lineList.Count; i++)
        {
            if(i == 0 || lineList[i].newLine)
                testSVGStringHeader += $"M {lineList[i].p0.x} {lineList[i].p0.y}  L {lineList[i].p1.x} {lineList[i].p1.y}";
            else
                testSVGStringHeader += $"L {lineList[i].p0.x} {lineList[i].p0.y}  L {lineList[i].p1.x} {lineList[i].p1.y}";
        }

        testSVGStringHeader += @"""/> </g> </svg>";




        string testSVGString = @"<svg width=""800"" height=""800""    xmlns:xlink=""http://www.w3.org/1999/xlink"" style=""stroke:black; stroke-opacity:1; stroke-width:1;""  xmlns=""http://www.w3.org/2000/svg""> <defs id = ""genericDefs""/>
                                <g> 
                                <path style = ""fill:none;"" d = ""M600 350 L293.7663 684.0021 L173.2283 107.2198 L671.9431 500.0001
                                 M600 500 L295.8442 680.4031 L215.4855 180.4115 L647.9621 500
                                 M600 500 L297.9221 676.8041 L257.7428 253.6032 L623.9811 500
                                 M100 100 L100 700 L700 700 L700 100 L100 100
                                 M100 100 L100 700 L700 700 L700 100 Q 400 1200 100 100""/>  
                                </g>
                                </svg>";


        //Write some text to the test.txt file
        StreamWriter writer = new StreamWriter(path, false);
        writer.WriteLine(testSVGStringHeader);
        writer.Close();

        ////Re-import the file to update the reference in the editor
        //AssetDatabase.ImportAsset(path);
        //TextAsset asset = Resources.Load("test");
        ////Print the text from the file
        //Debug.Log(asset.text);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(debugPos * .01f, .1f);

        if(lineList != null)
        {
            for (int i = 0; i < lineList.Count; i++)
            {
                Gizmos.DrawLine(lineList[i].p0 * .01f, lineList[i].p1 * .01f);
            }
        }
    }
}
