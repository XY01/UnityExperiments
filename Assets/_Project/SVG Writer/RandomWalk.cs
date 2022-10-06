using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.UIElements;

public class RandomWalk : MonoBehaviour
{
    Vector2 currentPos;
    List<Vector2> walkPoints = new List<Vector2>();

    public Vector2 dir = Vector2.one;
    public float speed = .1f;
    //public float drag = 0.5f;

    public float rotNoiseSpeed = .1f;
    float rotationNoise;
    public float speedNoiseSpeed = .2f;
    float speedNoise;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Update()
    {
        ManualUpdate(Time.deltaTime);
    }

    // Update is called once per frame
    void ManualUpdate(float delta)
    {
        //// DRAG        
        //vel = -vel * drag * delta;

        rotationNoise = Mathf.PerlinNoise(currentPos.x + (rotNoiseSpeed * Time.timeSinceLevelLoad), currentPos.y + (rotNoiseSpeed * Time.timeSinceLevelLoad));
        speedNoise = Mathf.PerlinNoise(currentPos.x + (speedNoiseSpeed * Time.timeSinceLevelLoad), currentPos.y + (speedNoiseSpeed * Time.timeSinceLevelLoad));

        FaceRotation(dir, rotationNoise * 360);

        // UPDATE POS
        currentPos += dir * speed * delta;
    }



    public Vector2 Rotate(Vector2 v, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        float tx = v.x;
        float ty = v.y;
        v.x = (cos * tx) - (sin * ty);
        v.y = (sin * tx) + (cos * ty);
        return v;
    }

    Vector2 FaceRotation(float rotation)
    {
        float _x = dir.x;
        float _y = dir.y;

        float _angle = rotation * Mathf.Deg2Rad;
        float _cos = Mathf.Cos(_angle);
        float _sin = Mathf.Sin(_angle);

        float _x2 = _x * _cos - _y * _sin;
        float _y2 = _x * _sin + _y * _cos;

        return new Vector2(_x2, _y2);
    }
}
