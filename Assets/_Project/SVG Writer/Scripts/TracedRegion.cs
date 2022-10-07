using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SVGGenerator
{
    [System.Serializable]
    public class TracedRegion
    {
        public bool debugDisable = false;
        public Color col;
        public RegionSelectionType regionSelectionType = RegionSelectionType.Value;

        public ContourType contourType = ContourType.Min;

        [Header("FILL")]
        public FillType fillType = FillType.ScanLine;
        [Range(0, 1)]
        public float fillDensityLow = .3f;
        [Range(0, 1)]
        public float fillDensityHigh = .6f;

        [Header("RANGE")]
        [Range(0, 1)]
        public float minRange = .4f;
        [Range(0, 1)]
        public float maxRange = .5f;
        [HideInInspector]
        public float prevMinRange = .4f, prevMaxRange = .5f;

        [Header("GIZMO VISIBILITY")]
        public bool contourMinVisibility = true;
        public bool contourMaxVisibility = true;
        public bool fillLinesVisibility = true;

        [HideInInspector]
        public List<Contour> minContours = new List<Contour>();
        [HideInInspector]
        public List<Contour> maxContours = new List<Contour>();     
        [HideInInspector]
        public List<Fill> fills = new List<Fill>();

        [HideInInspector]
        public float[,] regionMap;
        public float areaInPixels;
        public Vector4 bounds;

        public void Trace(SVGExporter svgExporter)
        {
            if (debugDisable)
                return;

            svgExporter.GenerateRegionMap(this);

            minContours.Clear();
            maxContours.Clear();
            fills.Clear();

            switch (contourType)
            {
                case ContourType.None:
                    break;
                case ContourType.Min:
                    svgExporter.TraceContour(this, minContours, minRange);
                    break;
                case ContourType.Max:
                    svgExporter.TraceContour(this, maxContours, maxRange);
                    break;
                case ContourType.MinMax:
                    svgExporter.TraceContour(this, minContours, minRange);
                    svgExporter.TraceContour(this, maxContours, maxRange);
                    break;
            }


            processedMinContours = svgExporter.ProcessContours(minContours, contourGap);
            processedMaxContours = svgExporter.ProcessContours(maxContours, contourGap);


            switch (fillType)
            {
                case FillType.ScanLine:
                    fills = svgExporter.ScanLineFill(this);
                    break;
                case FillType.Stipple:
                    svgExporter.StippleFill(this, Vector2.one);
                    break;
                case FillType.StippleDash:
                    svgExporter.StippleFill(this, new Vector2(20, 0));
                    break;
                case FillType.GradientStipple:
                    svgExporter.StippleFillGradient(this, Vector2.one, minRange, maxRange, 3);
                    break;
                case FillType.None:
                    break;
            }


            Debug.Log($"Region trace complete - Contours: {minContours.Count + maxContours.Count}  Fill lines: {fills.Count}");
        }
    }
}