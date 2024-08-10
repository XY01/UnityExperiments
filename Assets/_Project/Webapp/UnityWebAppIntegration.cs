using UnityEngine;
using System.Collections;
using NativeWebSocket;
using System.Text;

public class UnityWebAppIntegration : MonoBehaviour
{
    private WebSocket websocket;
    private const string WebSocketURL = "ws://localhost:3000";

    [System.Serializable]
    public class UIElement
    {
        public string id;
        public string type;
        public string label;
        public int min;
        public int max;
        public int value;
    }

    [System.Serializable]
    public class UIElements
    {
        public UIElement[] elements;
    }

    [System.Serializable]
    public class WebSocketMessage
    {
        public string id;
        public string value;
    }

    void Start()
    {
        StartCoroutine(SetupWebSocket());
    }

    IEnumerator SetupWebSocket()
    {
        Debug.Log("Attempting to connect to WebSocket...");
        websocket = new WebSocket(WebSocketURL);

        websocket.OnOpen += () =>
        {
            Debug.Log("WebSocket connection established");
        };

        websocket.OnError += (e) =>
        {
            Debug.LogError($"WebSocket error: {e}");
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("WebSocket connection closed");
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log($"Received WebSocket message: {message}");
            HandleIncomingMessage(message);
        };

        yield return websocket.Connect();
    }

    void HandleIncomingMessage(string jsonMessage)
    {
        WebSocketMessage message = JsonUtility.FromJson<WebSocketMessage>(jsonMessage);
        UpdateUnityObject(message);
    }

    void UpdateUnityObject(WebSocketMessage message)
    {
        // Implement your logic to update Unity GameObjects based on the received data
        switch (message.id)
        {
            case "button1":
                Debug.Log("button1 pressed");
                break;
            case "slider1":
                Debug.Log($"slider1 value: {message.value}");
                // Update a Unity object based on the slider value
                break;
            // Handle other UI elements
        }
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null)
        {
            websocket.DispatchMessageQueue();
        }
#endif
    }

    private void OnApplicationQuit()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            websocket.Close();
        }
    }
}