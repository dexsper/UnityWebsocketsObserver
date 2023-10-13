# UnityWebsocketsObserver
Based on [NativeWebSocket](https://github.com/endel/NativeWebSocket) <br/>
Analog for the backend: [extented-ws](https://pypi.org/project/extented-ws/)

This is a websocket wrapper for Unity designed to process JSON messages<br/>
The observer-handlers approach is used, like in FastAPI and Aiogram it's allows you to quickly integrate web sockets into the project, and expand the list of requests-responses

## Installation
Requires Unity 2019.1+ with .NET 4.x+ Runtime

### Install manually
1. Download this project
2. Copy the sources from WebsocketsObserver into your Assets directory.

## Usage
### First you need initialize observer instance in scene:

<details>
  <summary>Singleton</summary>

1. Drop class `SingletonWebsocketsObserver` on gameobject in scene
2. Set websokets url
3. Use `SingletonWebsocketsObserver.Instance`
</details>

### For ease of use, you need to use the following **JSON** format both on the client and on the server:

```json
{
  "Type": "CalculateRequest",
  "Data": {
    "Numbers": [2, 2]
  }
}

{
  "Type": "CalculateResponse",
  "Data": {
    "Result": 4
  }
}
```

### Handle and Send message:

```cs
using WebsocketsObserver;
using WebsocketsObserver.Request;

public class CalculateResponse : IServerMessage
{
    public int Result { get; set; }
}

public class CalculateRequest : IClientRequest
{
    public int[] Numbers { get; set; }
}

public class Test : MonoBehaviour
{
    private void Start()
    {
        var observerInstance = SingletonWebsocketsObserver.Instance;

        observerInstance.RegisterHandler<CalculateResponse>(OnCalculated);
        observerInstance.SendRequest(new CalculateRequest
        {
            Numbers = new int[2] { 2, 2 }
        });
    }

    private void OnCalculated(CalculateResponse response)
    {
    }
}
```
