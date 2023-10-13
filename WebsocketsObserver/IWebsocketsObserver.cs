using System;
using WebsocketsObserver.Request;

namespace WebsocketsObserver
{
    public interface IWebsocketsObserver
    {
        event Action<bool> OnStateChanged;
        
        public void RegisterHandler<T>(Action<T> handler) where T : IServerMessage;
        public void UnregisterHandler<T>(Action<T> handler) where T : IServerMessage;
        public void SendRequest<T>(T request) where T : IClientRequest;
    }
}