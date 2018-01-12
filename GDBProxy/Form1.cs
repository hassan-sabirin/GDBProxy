using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Collections;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Channels.Tcp;
using HassanS;

namespace GDBProxy
{
    public partial class Form1 : myForm
    {
        private TcpChannel tcpChannel;
        private IpcChannel ipcChannel;

        public Form1()
        {
            InitializeComponent();

            this.logtextbox = this.textBox1;
            this.statustext = this.toolStripStatusLabel1;


            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;

            IDictionary prop = new Hashtable();
            //prop["secure"] = true;
            prop["portname"] = "localhost:43333";
            prop["port"] = 43333;
            prop["connectionTimeout"] = 24*60*60*1000;
            prop["tokenImpersonationLevel"] = "Impersonation";


            ipcChannel = new IpcChannel(prop, null, provider);

            tcpChannel = new TcpChannel(prop, null, provider);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // put object service online
            string server = "localhost:43333";

            ChannelServices.RegisterChannel(tcpChannel, false);
            
            ChannelServices.RegisterChannel(ipcChannel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(GDBProxy), "GDBProxy", WellKnownObjectMode.Singleton);
            this.LogUpdate("ArcObject Server created");
            //GDBProxy proxy = new GD   BProxy();
            //proxy.Initialize("test");

        }
    }
}
