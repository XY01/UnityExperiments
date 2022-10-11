using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SVGGenerator
{
    public enum ContourType
    {
        None,
        Min,
        Max,
        MinMax
    }

    [System.Serializable]
    public class Contour
    {
        public bool closedContour => startPix == endPix;

        public Vector2 startPix;
        public Vector2 endPix;
        public List<Line> lines { private set; get; } = new List<Line>();
        public List<Line> processedLines = new List<Line>();
        public string endCondition;

        public void ProcessContours(int gap, bool debug = false)
        {
            if (debug) Debug.Log($"ProcessContours Before: {lines.Count}  {processedLines.Count}");

            List<Line> newProcessedLines = new List<Line>();
            for (int i = 0; i < lines.Count; i++)
            {
                if (i % gap == 0)
                    newProcessedLines.Add(new Line() { p0 = lines[i].p0, p1 = lines[i].p1, newLine = true });
            }

            processedLines = newProcessedLines;
            if (debug) Debug.Log($"ProcessContours Before/After: {lines.Count}  {processedLines.Count}");
        }

        // REF: https://answers.unity.com/questions/1368390/how-to-calculate-curvature-of-a-path.html
        public void Simplify(int minSampleOffset, int curvatureSampleCount, float curveCutoff)
        {
            processedLines.Clear();

            int currentIndex = 0;
            int count = 0;

            for (int x = minSampleOffset; x <= curvatureSampleCount; x++)
            {
                // Start at point, check curvature of next few points                    
                int middleIndex = currentIndex + x;
                int endIndex = currentIndex + x * 2;

                if (endIndex >= lines.Count)
                {
                    processedLines.Add(new Line() { p0 = lines[currentIndex].p0, p1 = lines[0].p0 });
                    break;
                }

                float curvature = SVGExporter.CurvatureFrom3Points(lines[currentIndex].p0, lines[middleIndex].p0, lines[endIndex].p0);
                if (curvature < curveCutoff || x == curvatureSampleCount || endIndex >= lines.Count)
                {
                    //print("added " + curvature);
                    processedLines.Add(new Line() { p0 = lines[currentIndex].p0, p1 = lines[endIndex].p0 });
                    //curvatureAtPoint.Add(new Vector3(lines[currentIndex].p0.x, lines[currentIndex].p0.y, .5f + curvature));
                    //curvatureAtPoint.Add(new Vector3(lines[endIndex].p0.x, lines[endIndex].p0.y, .5f + curvature));
                    currentIndex = endIndex;
                    x = minSampleOffset;
                    count++;
                    if (count > 1000 || currentIndex >= lines.Count)
                        break;
                }
            }
        }

        public string GenerateSVGString()
        {
            string s = "";
            for (int i = 0; i < processedLines.Count; i++)
            {
                if (i == 0 || processedLines[i].newLine)
                    s += $"   M {processedLines[i].p0.x} {processedLines[i].p0.y}  L {processedLines[i].p1.x} {processedLines[i].p1.y}";
                else
                    s += $"   L {processedLines[i].p0.x} {processedLines[i].p0.y}  L {processedLines[i].p1.x} {processedLines[i].p1.y}";
            }

            return s;
        }

        public void DrawGizmos(float pixelToWorldScalar)
        {
            for (int i = 0; i < processedLines.Count; i++)
            {
                Gizmos.DrawLine(processedLines[i].p0 * pixelToWorldScalar, processedLines[i].p1 * pixelToWorldScalar);
            }
        }
    }
}