using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncTcpClient
{
    //用于回调参数
    public class ReadObject
    {
        public NetworkStream netStream;
        public byte[] bytes;
        public ReadObject(NetworkStream netStream, int bufferSize)
        {
            this.netStream = netStream;
            bytes = new byte[bufferSize];
        }
    }
}
