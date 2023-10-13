using System;

namespace WebsocketsObserver
{
    [Serializable]
    public class SocketMessage
    {
        public string Type { get; set; }
        public string Data { get; set; }
    }
}