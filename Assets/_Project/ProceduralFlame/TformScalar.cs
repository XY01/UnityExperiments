using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TformScalar : MonoBehaviour
{
    public float speed = 1;
    public Vector3 maxScale;
    public Vector3 minScale;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float sin = (Mathf.Sin(speed * Time.time) + 1) * .5f;
        transform.localScale = Vector3.Lerp(minScale, maxScale, sin);
    }
}
