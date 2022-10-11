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


// Test distance from line calc  - DONE
// Try sampling filtered pixels and using UV spaced stepping to get less pixelated lines 
// Set HSV select from region in shader in on validate
// Stippling, dash line styles
// Try draw a path along a line where contour lines are near parallell
// Simplify lines, straighten lines by taking angle to next lines end point and if not over x angle then move end point



// Create shader to mimick final output
// Show histogram of image to allow for selection of range visually
// constraint stipple dash to region
// mask contour tracing by stipple


namespace SVGGenerator
{
    #region ENUMS  
    public struct Line
    {
        public bool newLine;
        public Vector2 p0;
        public Vector2 p1;
    }



    public enum ImageValueSelectionType
    {
        Hue,
        Brightness,
        Saturation
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
        public int gizmoGridSize = 100;
        public Color debugGridCol = Color.white * .2f;

        // HELPER PROPS
        private Vector2 pixelMidpointWorldSpace => new Vector2(tex.width * .5f, tex.height * .5f) * pixelToWorldScalar;
        #endregion


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

        [ContextMenu("Generate")]
        void GenerateTracedRegions()
        {
            for (int i = 0; i < tracedRegions.Length; i++)
            {
                tracedRegions[i].Trace(this);
            }
        }

        [ContextMenu("Process contours")]
        void ProcessRegionContours()
        {
            foreach(TracedRegion region in tracedRegions)
            {
                region.ProcessContours();
            }
        }

        [ContextMenu("Simplify")]
        void SimplifyRegions()
        {
            foreach(TracedRegion region in tracedRegions)
            {
                region.SimplifyContours();
            }
        }

        [ContextMenu("Output")]
        public void WriteString()
        {
            // CACHE PIXEL COLS
            //
            string path = "Assets/_Project/SVG Writer/Resources/testSVG " + System.DateTime.Now.ToString("yyyy-MM-dd") + ".svg";
            
            string testSVGStringHeader = $@"<svg width=""{tex.width}"" height=""{tex.height}""    xmlns:xlink=""http://www.w3.org/1999/xlink"" style=""stroke:black; stroke-opacity:1; stroke-width:1;""  xmlns=""http://www.w3.org/2000/svg""> <defs id = ""genericDefs""/>";
            //<g> <path style = ""fill:none;"" d = """;

            foreach (TracedRegion region in tracedRegions)
            {
                testSVGStringHeader += @"""<g> <path style = ""fill:none;"" d = """;

                foreach (Contour c in region.minContours)
                {
                    for (int i = 0; i < c.processedLines.Count; i++)
                    {
                        if (i == 0 || c.processedLines[i].newLine)
                            testSVGStringHeader += $"   M {c.processedLines[i].p0.x} {c.processedLines[i].p0.y}  L {c.processedLines[i].p1.x} {c.processedLines[i].p1.y}";
                        else
                            testSVGStringHeader += $"   L {c.processedLines[i].p0.x} {c.processedLines[i].p0.y}  L {c.processedLines[i].p1.x} {c.processedLines[i].p1.y}";
                    }
                }

                foreach (Contour c in region.maxContours)
                {
                    for (int i = 0; i < c.processedLines.Count; i++)
                    {
                        if (i == 0 || c.processedLines[i].newLine)
                            testSVGStringHeader += $"   M {c.processedLines[i].p0.x} {c.processedLines[i].p0.y}  L {c.processedLines[i].p1.x} {c.processedLines[i].p1.y}";
                        else
                            testSVGStringHeader += $"   L {c.processedLines[i].p0.x} {c.processedLines[i].p0.y}  L {c.processedLines[i].p1.x} {c.processedLines[i].p1.y}";
                    }
                }


                // FILL LINES
                //
                for (int i = 0; i < region.fill.fillLines.Count; i++)
                {
                    if (i == 0 || region.fill.fillLines[i].newLine)
                        testSVGStringHeader += $"   M {region.fill.fillLines[i].p0.x} {region.fill.fillLines[i].p0.y}  L {region.fill.fillLines[i].p1.x} {region.fill.fillLines[i].p1.y}";
                    else
                        testSVGStringHeader += $"   L {region.fill.fillLines[i].p0.x} {region.fill.fillLines[i].p0.y}  L {region.fill.fillLines[i].p1.x} {region.fill.fillLines[i].p1.y}";
                }

                testSVGStringHeader += @"""/> </g>";
            }

            testSVGStringHeader += @"</svg>";


            // WRITE TO TEXT FILE
            //
            StreamWriter writer = new StreamWriter(path, false);
            writer.WriteLine(testSVGStringHeader);
            writer.Close();
        }

        public float GetValueAtPixel(ImageValueSelectionType selectionType, int x, int y)
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
                case ImageValueSelectionType.Hue:
                    return h;
                case ImageValueSelectionType.Saturation:
                    return s;
                case ImageValueSelectionType.Brightness:
                    return v;
                default:
                    return v;
            }
        }

        // GIZMOS
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
                        c.DrawGizmos(pixelToWorldScalar);                       
                    }
                }

                if (region.contourMaxVisibility)
                {
                    foreach (Contour c in region.maxContours)
                    {
                        c.DrawGizmos(pixelToWorldScalar);
                    }
                }

                if (region.fillLinesVisibility)
                {
                    region.fill.DrawGizmos(pixelToWorldScalar);                   
                }
            }

            // DRAW PIXEL GUIDES
            //
            Gizmos.color = debugGridCol;
            int xCount = Mathf.CeilToInt(tex.width / (float)gizmoGridSize);
            int yCount = Mathf.CeilToInt(tex.height / (float)gizmoGridSize);
            for (int x = 0; x < xCount; x++)
            {
                float xNorm = x / (xCount-1.0f);
                float xPos = xNorm * tex.width;
                Gizmos.DrawLine(new Vector3(xPos, 0, 0) * pixelToWorldScalar, new Vector3(xPos, tex.height, 0) * pixelToWorldScalar);
            }

            for (int y = 0; y < yCount; y++)
            {
                float yNorm = y / (yCount - 1.0f);
                float yPos = yNorm * tex.height;
                Gizmos.DrawLine(new Vector3(0, yPos, 0) * pixelToWorldScalar, new Vector3(tex.width, yPos, 0) * pixelToWorldScalar);
            }            
        }


        #region HELPER METHODS
        public static float CurvatureFrom3Points(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            var cc = CircleCenterFrom3Points(p1, p2, p3);
            var radius = (cc - p1).magnitude;
            radius = Mathf.Max(radius, 0.0001f);
            return 1f / radius;
        }

        private static Vector2 CircleCenterFrom3Points(Vector2 p1, Vector2 p2, Vector2 p3)
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

        float DistanceOfPointFromLine(Vector2 origin, Vector2 end, Vector2 point)
        {
            float a = Vector2.Distance(origin, end);
            float b = Vector2.Distance(origin, point);
            float c = Vector2.Distance(end, point);
            float s = (a + b + c) / 2;
            float distance = 2 * Mathf.Sqrt(s * (s - a) * (s - b) * (s - c)) / a;

            return distance;
        }
        #endregion
    }
}