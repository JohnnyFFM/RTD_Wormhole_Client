using System;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RTD_Wormhole
{
    public partial class Form1 : Form
    {
        private readonly SynchronizationContext synchronizationContext;
        private WSClient LinkClient;

        public Form1()
        {
            InitializeComponent();
            toolStrip.ImageList = imageList;
            synchronizationContext = SynchronizationContext.Current;
            tb_client_ip.Text = Helper.GetLocalIp();
        }     

        // LinkClient
        private void Btn_client_Click(object sender, EventArgs e)
        {
            // allow only one client
            if (LinkClient != null)
            {
                if (LinkClient.connecting) return;
                AppendLog("Stopping Wormhole client...");
                if (LinkClient.Connected())
                {
                    LinkClient.Stop();
                }
                else
                {
                    //LinkClient.Dispose();
                    LinkClient = null;
                }
                ChangeWebSocketStatus("client_link_status", 0);
            } else
            {
                AppendLog("Starting Wormhole client...");
                LinkClient = new WSClient(tb_client_ip.Text, Decimal.ToInt32(ud_client_port.Value), false);
                LinkClient.EConnect += LinkClientConnected;
                LinkClient.EDisconnect += LinkClientDisconnected;
                LinkClient.EData += LinkClientMessageReceived;
                ChangeWebSocketStatus("client_link_status", 1);
                LinkClient.Start();
            }
        }
        void LinkClientConnected(object sender, EventArgs args)
        {
            ChangeWebSocketStatus("client_link_status", 2);
            AppendLog("Client connected to server.");
        }

        void LinkClientDisconnected(object sender, EventArgs args)
        {
            //LinkClient.Dispose();
            LinkClient = null;
            ChangeWebSocketStatus("client_link_status", 0);
            AppendLog("Client not connected.");
        }
        void LinkClientMessageReceived(object sender, DataEventArgs args)
        {
            AppendLog("Client received RTDdata");

            for (int i = 0; i < args.Count; i++)
            {
                int id = (int)args.Data[0, i];

                double value = 0;
                if (args.Data[1, i].GetType() == typeof(double))
                {
                    value = (double)args.Data[1, i];
                }
                SetFX(value);
            }
        }
              
        // UI

        /// <summary>
        /// safely access UI from other threads
        /// </summary>
        /// <param name="action"></param>
        public void PostUI(Action action)
        {
            synchronizationContext.Post(new SendOrPostCallback(o => action()), null);
        }


        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: shutdown server, client and all active connections
            Application.Exit();
        }

        private static string LogStatement(string logText)
        {
            return "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "] " + logText + Environment.NewLine;
        }

        public void ChangeWebSocketStatus(string target, int status, bool debug = false)
        {
            if (debug)
                return;
            if (this.InvokeRequired)
            {
                this.Invoke(
                    new MethodInvoker(
                     delegate () { ChangeWebSocketStatus(target, status, debug); }));
            }
            else
            {
                switch (target)
                {
                    case "client_link_status":
                        client_ws_status.Image = imageList.Images[status];
                        btn_client.Image = imageList.Images[status];
                        break;
                }
            }
        }

        public void AppendLog(string logText, bool debug = false)
        {
            if (debug)
                return;
            if (this.InvokeRequired)
            {
                this.Invoke(
                    new MethodInvoker(
                    delegate () { AppendLog(logText); }));
            }
            else
            {
                DateTime timestamp = DateTime.Now;
                textBox1.AppendText(LogStatement(logText));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SubscribeFX(tb_ccy.Text);
        }

        public void SubscribeFX(string ccy)
        {
            ccy = ccy.ToUpper();
            // return if already subscribed 
            object[] topicFX = new object[4];
            topicFX[0] = "PRICE";
            topicFX[1] = "#" + ccy;
            topicFX[2] = "FRX";
            topicFX[3] = "LAST";
            LinkClient.Subscribe(0, topicFX);
        }

            public void SetFX(double fx)
            {
                PostUI(() =>
                {
                    tb_fx.Text = fx.ToString();
                });
            }
        }
}
