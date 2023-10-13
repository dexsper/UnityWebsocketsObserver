using NativeWebSocket;
using UnityEngine;
using Zenject;

namespace WebsocketsObserver
{
    public class SingletonWebsocketsObserver : MonoBehaviour
    {
        [SerializeField] private string _wsUrl;

        public static WebsocketsObserver Instance { get; private set; }

        private void Awake()
        {
            Instance = new WebsocketsObserver(new WebSocket(_wsUrl));
        }

        private void Update()
        {
            Instance.Tick();
        }
    }

    public class ZenjectWebsocketsObserver : WebsocketsObserver, ITickable
    {
        [Inject]
        public ZenjectWebsocketsObserver(WebSocket websocket) : base(websocket)
        {
        }
    }
}