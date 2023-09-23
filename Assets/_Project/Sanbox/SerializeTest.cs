using Unity.Mathematics;
using UnityEngine;

public class SerializeTest : MonoBehaviour
{   
    public string playerName;
    public int lives;
    public float health;
 
    [SerializeField]
    SerializeData test;

    string s;

    [ContextMenu("Save")]
    public string SaveToString()
    {
        test = new SerializeData();

        s = JsonUtility.ToJson(test);
        print(s);
        return s;
    }

    [ContextMenu("Load")]
    public void Load()
    {
        test = null;
        test = JsonUtility.FromJson<SerializeData>(s);
    }
}

[System.Serializable]
public class SerializeData
{
    [SerializeField]
    TestStruct[] testArray;

    public SerializeData()
    {
        testArray = new TestStruct[2];
        testArray[0] = new TestStruct() { m0 = new float4x4(), lives = 1 };
        testArray[1] = new TestStruct() { m0 = new float4x4(), lives = 4 };
    }
}

[System.Serializable]
public struct TestStruct
{
    public float4x4 m0;
    public int lives;
}