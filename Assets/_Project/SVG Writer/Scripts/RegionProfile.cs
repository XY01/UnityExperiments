using UnityEngine;
using SVGGenerator;

[CreateAssetMenu(fileName = "Region Profile", menuName = "SVG Generator/Region Profile", order = 1)]
public class SpawnManagerScriptableObject : ScriptableObject
{
    public TracedRegion[] tracedRegions;
}