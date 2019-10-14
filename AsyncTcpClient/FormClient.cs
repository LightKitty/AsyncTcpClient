using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AsyncTcpClient
{
    public partial class FormClient : Form
    {
        private bool isExit = false;
        //用于线程间的互操作
        private delegate void SetListBoxCallback(string str);
        private SetListBoxCallback setListBoxCallback;
        private delegate void SetRichTextBoxReceiveCallback(string str);
        private SetRichTextBoxReceiveCallback setRichTextBoxReceiveCallback;
        private TcpClient client;
        private NetworkStream networkStream;
        //用于线程同步，初始状态设为非终止状态，使用手动重置方式
        private EventWaitHandle allDone = new EventWaitHandle(false, EventResetMode.ManualReset);

        public FormClient()
        {
            InitializeComponent();
            listBoxStatus.HorizontalScrollbar = true;
            setListBoxCallback = new SetListBoxCallback(SetListBox);
            setRichTextBoxReceiveCallback = new SetRichTextBoxReceiveCallback(SetRichTextBoxReceive);
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            //使用IPv4
            //client = new TcpClient(AddressFamily.InterNetwork);
            //实际使用时要将Dns.GetHostName()变为服务器名或IP地址
            IPAddress[] serverIPs = Dns.GetHostAddresses(Dns.GetHostName());
            //创建一个委托，让其引用在异步操作完成时调用的回调方法
            AsyncCallback requestCallback = new AsyncCallback(RequestCallback);
            //将事件的状态设为非终止状态
            allDone.Reset();
            //开始一个对远程主机的异步请求
            IPAddress serverIp = serverIPs[0];
            client = new TcpClient(serverIp.AddressFamily);
            client.BeginConnect(serverIp, 51888, requestCallback, client);
            listBoxStatus.Invoke(setListBoxCallback, string.Format("本机 EndPoint：{0}", client.Client.LocalEndPoint));
            listBoxStatus.Invoke(setListBoxCallback, "开始与服务器建立连接");
            //阻塞当前进程，即场态界面不再相应任何用户操作，等待BeginConnect完成
            //这样做的目的时为了与服务器连接有结果（成功或失败）时，才能继续
            //当beginConnect完成时，会自动调用RequestCallback
            //通过在RequestCallback中调用Set方法解除阻塞
            allDone.WaitOne();
        }
        //ar是IAsyncResult类型的几口，表示异步操作的状态
        //是由listenner.BeginAcceptTcpClient(callback,listener)传递过来的
        private void RequestCallback(IAsyncResult ar)
        {
            //异步操作能执行到此处，说明调用BeginConnect已经完成
            //并且得到了IAsyncResult类型的状态参数ar，但BeginConnect尚未结束
            //此时需要解除阻塞，以便能调用EndConnect
            allDone.Set();
            //调用Set后，事件的状态变为终止状态，当前线程继续
            //buttonConnect_Click执行结束，同时窗台界面可以相应用户操作
            try
            {
                //获取连接成功后得到的状态参数
                client = (TcpClient)ar.AsyncState;
                //异步接收传入的连接尝试，使BeginConnect正常结束
                client.EndConnect(ar);
                listBoxStatus.Invoke(setListBoxCallback, string.Format("与服务器{0}连接成功", client.Client.RemoteEndPoint));
                //获取接收和发送数据的网络流
                networkStream = client.GetStream();
                //获取接收和发送的数据，BeginRead完成后，会自动调用ReadCallback
                ReadObject readObject = new ReadObject(networkStream, client.ReceiveBufferSize);
                networkStream.BeginRead(readObject.bytes, 0, readObject.bytes.Length, ReadCallback, readObject);
                //allDone.WaitOne();
            }
            catch(Exception err)
            {
                listBoxStatus.Invoke(setListBoxCallback, err.Message);
                return;
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            //异步操作能执行到此处，说明调用BeginRead已经完成
            try
            {
                ReadObject readObject = (ReadObject)ar.AsyncState;
                int count = readObject.netStream.EndRead(ar);
                richTextBoxReceive.Invoke(setRichTextBoxReceiveCallback, Encoding.UTF8.GetString(readObject.bytes, 0, count));
                if(isExit==false)
                {
                    //重新调用BeginRead进行异步读取
                    readObject = new ReadObject(networkStream, client.ReceiveBufferSize);
                    networkStream.BeginRead(readObject.bytes, 0, readObject.bytes.Length, ReadCallback, readObject);
                }
            }
            catch (Exception err)
            {
                listBoxStatus.Invoke(setListBoxCallback, err.Message);
            }
        }

        private void SendString(string str)
        {
            try
            {
                byte[] bytesData = Encoding.UTF8.GetBytes(str + "\r\n");
                networkStream.BeginWrite(bytesData, 0, bytesData.Length, new AsyncCallback(SendCallback), networkStream);
            }
            catch(Exception err)
            {
                listBoxStatus.Invoke(setListBoxCallback, err.Message);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                networkStream.EndWrite(ar);
            }
            catch(Exception err)
            {
                listBoxStatus.Invoke(setListBoxCallback, err.Message);
            }
        }

        private void SetListBox(string str)
        {
            listBoxStatus.Items.Add(str);
            listBoxStatus.SelectedIndex = listBoxStatus.Items.Count - 1;
            listBoxStatus.ClearSelected();
        }

        private void SetRichTextBoxReceive(string str)
        {
            richTextBoxReceive.AppendText(str);
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            SendString(richTextBoxSend.Text);
            richTextBoxSend.Clear();
        }

        private void FormClient_Click(object sender, EventArgs e)
        {
            isExit = true;
            allDone.Set();
        }
    }
}
