using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossProductHatching : MonoBehaviour
{
    RaycastHit hit;
    public List<CrossInfo> crossInfoList = new List<CrossInfo>();
    CrossInfo activeCrossInfo;

    public float stepSize = .01f;
    public int maxSteps = 200;
    bool tracing = false;

    [System.Serializable]
    public struct CrossInfo
    {
        public Vector3 hitPos;
        public Vector3 hitCross;
        public Vector3 hitNormal;
    }

    // Start is called before the first frame update
    void Start()
    {
        hit = new RaycastHit();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = 30;
            
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.ScreenToWorldPoint(mousePosition) - Camera.main.transform.position);
            if (Physics.Raycast(ray, out hit))
            {
                activeCrossInfo = new CrossInfo() { hitPos = hit.point, hitCross = Vector3.Cross(hit.normal, Vector3.up), hitNormal = hit.normal };
                crossInfoList.Add(activeCrossInfo);

                tracing = true;
            }
        }

        if(tracing)
        {
            int stepCount = 0;
            while (stepCount < maxSteps)
            {
                if (!RaycastAtPoint(activeCrossInfo))
                {
                    print("No point found along path");
                    tracing = false;
                    break;
                }
                else
                {
                    //print("stepCount  " + stepCount);
                    stepCount++;
                }
            }
            tracing = false;
        }
    }
    float currentAngle = 0;
    bool RaycastAtPoint(CrossInfo crossInfo)
    {
        hit = new RaycastHit();
        Vector3 rayAim = (crossInfo.hitPos + (crossInfo.hitCross * stepSize));
        Ray ray = new Ray(Camera.main.transform.position, rayAim - Camera.main.transform.position);
        if (Physics.Raycast(ray, out hit))
        {

            Vector3 upVec = Vector3.ProjectOnPlane(Vector3.up, hit.normal);


            Vector3 prevPos = activeCrossInfo.hitPos;
            float prevAngle = currentAngle;

            currentAngle = Vector3.SignedAngle(ray.direction, hit.normal, upVec);
            float angleDiff =  currentAngle - prevAngle;
            Vector3 cross = Vector3.Cross(hit.normal, upVec);

            //Quaternion rotQuat = Quaternion.Euler(0, 0, -angleDiff);
            //cross = rotQuat * cross;
            //if (angle < 20)
            //{
                
               
            //}
            //else if(angle > 20)
            //{
            //    Quaternion rotQuat = Quaternion.Euler(0, 0, -20);
            //    cross = rotQuat * cross;
            //}


            activeCrossInfo = new CrossInfo() { hitPos = hit.point, hitCross = cross, hitNormal = hit.normal };
            crossInfoList.Add(activeCrossInfo);
            print("added point. Dist to last: " + Vector3.Distance(prevPos, activeCrossInfo.hitPos));
            return true;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        //print(crossInfoList.Count);
        for (int i = 0; i < crossInfoList.Count; i++)
        {
            Gizmos.DrawSphere(crossInfoList[i].hitPos, stepSize * .1f);
            //Gizmos.DrawLine(crossInfoList[i].hitPos, crossInfoList[i].hitPos + crossInfoList[i].hitNormal * .1f);
            Gizmos.DrawLine(crossInfoList[i].hitPos, crossInfoList[i].hitPos + crossInfoList[i].hitCross * stepSize);
        }     
    }
}
