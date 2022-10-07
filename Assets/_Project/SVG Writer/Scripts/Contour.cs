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
        public Vector2 startPix;
        public Vector2 endPix;
        public List<Line> lines = new List<Line>();
        public List<Line> processedLines = new List<Line>();
        public string endCondition;
        public int contourGap = 0;

        public bool closedContour => startPix == endPix;
        
    }
}