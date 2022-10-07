using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SVGGenerator
{
    public enum FillType
    {
        None,
        ScanLine,
        Stipple,
        GradientStipple,
        StippleDash,
    }

    [System.Serializable]
    public class Fill
    {
        [Header("FILL")]
        public FillType fillType = FillType.ScanLine;
        [Range(0, 1)]
        public float fillDensityLow = .3f;
        [Range(0, 1)]
        public float fillDensityHigh = .6f;

        public List<Line> fillLines = new List<Line>(); 
    }
}
