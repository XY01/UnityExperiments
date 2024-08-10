using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
        public List<UIElement> elements;
    }

    [System.Serializable]
    public class WebSocketMessage
    {
        public string type;
        public string data;
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
            SendUIElements();
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
            HandleIncomingMessage(message);
        };

        yield return websocket.Connect();
    }

    void SendUIElements()
    {
        UIElements uiElements = new UIElements
        {
            elements = new List<UIElement>
            {
                new() { id = "button1", type = "button", label = "Play" },
                new() { id = "button2", type = "button", label = "Pause" },
                new() { id = "button3", type = "button", label = "Stop" },
                new() { id = "slider1", type = "slider", label = "Gate", min = 0, max = 100, value = 0 },
                new() { id = "slider2", type = "slider", label = "Pattern", min = 0, max = 100, value = 1 },
                new() { id = "slider3", type = "slider", label = "FlowRate", min = 0, max = 100, value = 1 }
            }
        };

        WebSocketMessage message = new WebSocketMessage
        {
            type = "ui_elements",
            data = JsonUtility.ToJson(uiElements)
        };

        string jsonMessage = JsonUtility.ToJson(message);
        websocket.SendText(jsonMessage);
    }

    void HandleIncomingMessage(string jsonMessage)
    {
        Debug.Log($"Received message: {jsonMessage}");
        WebSocketMessage message = JsonUtility.FromJson<WebSocketMessage>(jsonMessage);
        UpdateUnityObject(message);
    }

    void UpdateUnityObject(WebSocketMessage message)
    {
        // Implement your logic to update Unity GameObjects based on the received data
        switch (message.type)
        {
            case "button_click":
                Debug.Log($"Button {message.data} clicked");
                break;
            case "slider_change":
                UIElement sliderData = JsonUtility.FromJson<UIElement>(message.data);
                if (sliderData is null)
                {
                    Debug.LogError("Failed to parse slider data");
                    break;
                }

                Debug.Log($"Slider {sliderData.label} value: {sliderData.value}");
                // Update Unity object based on the slider value
                break;
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