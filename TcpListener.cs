using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    class TcpListener {
        public static Control control;
        public static void Run() {
            System.Net.Sockets.TcpListener server = null;
            try {
                Int32 port = 13076;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                server = new System.Net.Sockets.TcpListener(localAddr, port);

                // Start listening for client requests.
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;

                // Enter the listening loop.
                while (true) {
                    using TcpClient client = server.AcceptTcpClient();

                    data = null;

                    // Get a stream object for reading and writing
                    NetworkStream stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0) {
                        data = System.Text.Encoding.UTF8.GetString(bytes, 0, i);
                        HandleString(data);
                    }
                }
            }
            catch {
            }
            finally {
                server!.Stop();
            }
        }
        protected static void HandleString(string fromJava) {
            Console.WriteLine(fromJava);
            control.Invoke(() => {
                var capture = fromJava;
                try {
                    PumpernickelMessage.HandleMessages(capture);
                }
                catch (Exception e) {
                    PumpernickelAdviceWindow.instance.DisplayException(e);
                }
            });
        }
    }
}
