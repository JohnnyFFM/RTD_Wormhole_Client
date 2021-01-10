using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using WatsonWebsocket; // wsclient

namespace RTD_Wormhole
{

    class WSClient : IDisposable
    {
        // Todo create log / error event to pass on data for logging
        private WatsonWsClient LinkClient;
        public bool connecting = false;
        private bool _disposed = false;

        public event EventHandler EConnect;
        public event EventHandler<StatusEventArgs> EStatus;
        public event EventHandler EDisconnect;
        public event EventHandler EHeartBeatLost;
        public event EventHandler<DataEventArgs> EData;

        public WSClient(string serverIP, int serverPort, bool ssl)
        {
            LinkClient = new WatsonWsClient(serverIP, serverPort, ssl);
            LinkClient.ServerConnected += LinkClientConnected;
            LinkClient.ServerDisconnected += LinkClientDisconnected;
            LinkClient.MessageReceived += LinkClientMessageReceived;
        }

        public void Dispose()
        {
            Dispose(true);

            // Use SupressFinalize in case a subclass 
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clear all property values that maybe have been set
                    // when the class was instantiated
                    if (LinkClient.Connected) LinkClient.Stop();
                    LinkClient.Dispose();
                }

                // Indicate that the instance has been disposed.
                _disposed = true;
            }
        }

        // Events

        protected virtual void OnConnect()
        {
            EConnect?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnStatus(StatusEventArgs error)
        {
            EStatus?.Invoke(this, error);
        }

        protected virtual void OnHeartBeatLost()
        {
            EHeartBeatLost?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDisconnect()
        {
            EDisconnect?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnData(DataEventArgs data)
        {
            EData?.Invoke(this, data);
        }

        // LinkClient
        public void Start()
        {
            connecting = true;
            LinkClient.Start();
        }
        public void Stop()
        {
            connecting = true;
            LinkClient.Stop();
        }
        void LinkClientConnected(object sender, EventArgs args)
        {
            connecting = false;
            // pass through event
            OnConnect();
        }

        void LinkClientDisconnected(object sender, EventArgs args)
        {
            LinkClient.Dispose();
            LinkClient = null;
            connecting = false;
            // pass through event
            OnDisconnect();
        }

        public bool Connected()
        {
            return LinkClient.Connected;
        }
        void LinkClientMessageReceived(object sender, MessageReceivedEventArgs args)
        {
            switch (args.MessageType)
            {
                case WebSocketMessageType.Text:
                    OnStatus(new StatusEventArgs(Encoding.UTF8.GetString(args.Data)));
                    break;
                case WebSocketMessageType.Binary:

                    object incoming = Helper.ByteArrayToObject(args.Data);
                    switch (incoming)
                    {
                        case RTDdata data:
                            OnData(new DataEventArgs(data.count, data.data));
                            break;
                        default:
                            break;
                    }

                    break;
                default:
                    break;
            }
        }

        public void Subscribe(int topicID, object[] data)
        {
            SubscriptionRequest sr = new SubscriptionRequest(topicID, data);
            byte[] srb = Helper.ObjectToByteArray(sr);
            LinkClient.SendAsync(srb);
        }
        public void Unsubscribe(int topicID)
        {
            CancelRequest cr = new CancelRequest(topicID);
            byte[] crb = Helper.ObjectToByteArray(cr);
            LinkClient.SendAsync(crb);
        }
    }

    [Serializable]
    struct SubscriptionRequest
    {
        readonly int topicID;
        readonly object[] topicParams;

        public SubscriptionRequest(int x, object[] y)
        {
            this.topicID = x;
            this.topicParams = y;
        }
    }

    [Serializable]
    struct RTDdata
    {
        public readonly int count;
        public readonly object[,] data;

        public RTDdata(int x, object[,] y)
        {
            this.count = x;
            this.data = y;
        }
    }

    [Serializable]
    struct CancelRequest
    {
        public readonly int topicID;

        public CancelRequest(int x)
        {
            this.topicID = x;
        }
    }

    public class DataEventArgs : EventArgs
    {
        public object[,] Data { get; set; }
        public int Count { get; set; }

        public DataEventArgs(int count, object[,] data)
        {
            this.Data = data;
            this.Count = count;
        }
    }

    public class StatusEventArgs : EventArgs
    {
        public string Errormsg { get; set; }

        public StatusEventArgs(string errormsg)
        {
            this.Errormsg = errormsg;
        }
    }
}

