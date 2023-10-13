using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine;
using WebsocketsObserver.Request;

namespace WebsocketsObserver
{
    public class WebsocketsObserver : IWebsocketsObserver, IDisposable
    {
        private delegate void ClientMessageDelegate(SocketMessage message);
        
        private readonly WebSocket _websocket;
        
        private readonly Dictionary<string, HashSet<ClientMessageDelegate>> _messageHandlers = new();
        private readonly Dictionary<string, HashSet<(int, ClientMessageDelegate)>> _handlerTargets = new();
        private readonly ConcurrentQueue<SocketMessage> _messages = new();

        public event Action<bool> OnStateChanged;

        public WebsocketsObserver(WebSocket websocket)
        {
            _websocket = websocket;
            
            _websocket.OnOpen += OnWebsocketOpen;
            _websocket.OnError += Debug.LogError;
            _websocket.OnClose += OnWebsocketClose;
            _websocket.OnMessage += OnMessage;
        }

        public void Tick()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _websocket.DispatchMessageQueue();
#endif

            while (_messages.Count > 0)
            {
                if (!_messages.TryDequeue(out var message))
                    continue;

                string key = message.Type;

                if (!_messageHandlers.TryGetValue(key, out HashSet<ClientMessageDelegate> handlers))
                    return;

                foreach (ClientMessageDelegate handler in handlers)
                {
                    if (handler.Target != null)
                    {
                        handler.Invoke(message);
                    }
                }
            }
        }
        
        public void Dispose()
        {
            if (_websocket == null) 
                return;
            
            _websocket.OnOpen -= OnWebsocketOpen;
            _websocket.OnError -= Debug.LogError;
            _websocket.OnClose -= OnWebsocketClose;
            _websocket.OnMessage -= OnMessage;
        }
        
        private void OnMessage(byte[] bytes)
        {
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            var message = JsonConvert.DeserializeObject<SocketMessage>(json);

            _messages.Enqueue(message);
        }

        private void OnWebsocketOpen()
        {
            Debug.Log("Connection open!");

            OnStateChanged?.Invoke(true);
        }

        private async void OnWebsocketClose(WebSocketCloseCode e)
        {
            Debug.Log($"Connection closed: {e} !");

            if (e == WebSocketCloseCode.Abnormal)
            {
                Debug.Log("Attempt reconnect...");
                
                await _websocket.Close();
                await Task.Delay(TimeSpan.FromSeconds(1));
                await _websocket.Connect();
            }

            OnStateChanged?.Invoke(false);
        }


        public void RegisterHandler<T>(Action<T> handler) where T : IServerMessage
        {
            string key = typeof(T).Name;

            if (!_messageHandlers.TryGetValue(key, out var handlers))
            {
                handlers = new HashSet<ClientMessageDelegate>();
                _messageHandlers.Add(key, handlers);
            }

            ClientMessageDelegate del = CreateMessageDelegate(handler);
            handlers.Add(del);

            int handlerHashCode = handler.GetHashCode();

            if (!_handlerTargets.TryGetValue(key, out var targetHashCodes))
            {
                targetHashCodes = new HashSet<(int, ClientMessageDelegate)>();
                _handlerTargets.Add(key, targetHashCodes);
            }

            targetHashCodes.Add((handlerHashCode, del));
        }

        public void UnregisterHandler<T>(Action<T> handler) where T : IServerMessage
        {
            string key = typeof(T).FullName;

            if (string.IsNullOrEmpty(key))
                throw new NullReferenceException(key);

            if (!_messageHandlers.TryGetValue(key, out HashSet<ClientMessageDelegate> handlers))
                return;

            if (_handlerTargets.TryGetValue(key, out var targetHashCodes))
            {
                int handlerHashCode = handler.GetHashCode();

                ClientMessageDelegate result = null;

                foreach ((int targetHashCode, ClientMessageDelegate del) in targetHashCodes)
                {
                    if (targetHashCode == handlerHashCode)
                    {
                        result = del;
                        targetHashCodes.Remove((targetHashCode, del));
                        break;
                    }
                }

                if (targetHashCodes.Count == 0)
                    _handlerTargets.Remove(key);

                if (result != null)
                    handlers.Remove(result);
            }

            if (handlers.Count == 0)
                _messageHandlers.Remove(key);
        }

        public void SendRequest<T>(T request) where T : IClientRequest
        {
            var message = new SocketMessage
            {
                Type = typeof(T).Name,
                Data = JsonConvert.SerializeObject(request)
            };

            _websocket.SendText(JsonConvert.SerializeObject(message));
        }


        private ClientMessageDelegate CreateMessageDelegate<T>(Action<T> handler)
        {
            void LogicContainer(SocketMessage message)
            {
                T messageData = JsonConvert.DeserializeObject<T>(message.Data, new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                handler?.Invoke(messageData);
            }

            return LogicContainer;
        }
    }
}