using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace BlackOSClient
{
    struct Connection
    {
        public Socket Soc;
        public byte[] buffer;
        public bool Successfull;
        public ManualResetEvent allDone;
        public ManualResetEvent FullyDone;
        public UInt16 CID;
    }
    public static class CommandSender
    {

        private const int LastReadTimeoutSeconds = 1;
        private const int BufferSize = 1024;
        private static Dictionary<string, UInt16> Commands;
        private static string HostIP;
        public static Dictionary<string, UInt16> CMDs => Commands;

        public static bool INIT(string HostIP, bool Display = true)
        {
            Commands = new Dictionary<string, ushort>();
            Connection conn = new Connection();
            conn.buffer = new byte[2];
            conn.allDone = new ManualResetEvent(false);
            conn.FullyDone = new ManualResetEvent(false);

            CommandSender.HostIP = HostIP;
            try
            {
                conn.Soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                conn.Soc.BeginConnect(HostIP, 2460, new AsyncCallback(ConnectCallBack), conn);
                conn.allDone.WaitOne();
            }
            catch
            {
                Console.WriteLine("Failed to connect to host " + HostIP);
                return false;
            }
            try
            {
                conn.Soc.BeginSend(BitConverter.GetBytes((UInt16)0), 0, 2, SocketFlags.None, new AsyncCallback(SendCallBack), conn);
                conn.allDone.WaitOne();
                Thread.Sleep(100);
                conn.buffer = new byte[2];
                conn.Soc.Receive(conn.buffer);
                Int16 Resp = BitConverter.ToInt16(conn.buffer, 0);
                switch (Resp)
                {
                    case (1):
                        conn.buffer = new byte[BufferSize];
                        RipCommands(conn, Display);
                        conn.FullyDone.WaitOne();
                        return true;

                    case (2):
                        Console.WriteLine("Failed to Get CommandList Return:Invalid Args");
                        return false;

                    case (3):
                        Console.WriteLine("Failed to Get CommandList Return:Invalid Command Code");
                        return false;

                    default:
                        Console.WriteLine("Invalid Response Code");
                        Console.WriteLine("Response code:" + Resp);
                        Console.WriteLine($"DataBuffer: [0]:{conn.buffer[0]},[1]:{conn.buffer[1]}");
                        Console.WriteLine($"DataBufferSize:{conn.buffer.Length}");
                        return false;
                }
            }
            catch
            {
                Console.WriteLine("Failed to Send RemoteCommand");
                return false;
            }
        }
        public static void ExecCommand(string parse)
        {
            if (parse.Contains(" "))
            {
                if (Commands.ContainsKey(parse.Substring(0, parse.IndexOf(" "))))
                {
                    UInt16 CID = Commands[parse.Substring(0, parse.IndexOf(" "))];
                    string Args = parse.Substring(parse.IndexOf(" ") + 1);
                    if(Args.Contains(" && "))
                    {
                        List<ushort> cid = new List<ushort>();
                        List<string> args = new List<string>();
                        bool fail = false;
                        while (true)
                        {
                            cid.Add(CID);
                            if (Args.Contains(" && "))
                                args.Add(Args.Substring(0, Args.IndexOf(" && ")));
                            else
                            {
                                args.Add(Args);
                                break;
                            }
                            Args = Args.Substring(Args.IndexOf(" && ") + 4);
                            if (Commands.ContainsKey(Args.Substring(0, Args.IndexOf(" "))))
                            {
                                CID = Commands[Args.Substring(0, Args.IndexOf(" "))];
                                Args = Args.Substring(Args.IndexOf(" ") + 1);
                            }
                            else
                            {
                                fail = true;
                                Console.WriteLine("Unknown command : "+ Args.Substring(0, Args.IndexOf(" ")));
                            }
                        }
                        if(!fail)
                        {
                            for(int x = 0; x < cid.Count; x++)
                            {
                                SendCommand(cid[x], args[x]);
                            }
                        }
                    }
                    else SendCommand(CID, Args);
                }
                else
                {
                    Console.WriteLine("Unknown command... there is no known key for it");
                }
            }
            else
            {
                if (Commands.ContainsKey(parse))
                {
                    SendCommand(Commands[parse], "");
                }
                else
                {
                    Console.WriteLine("Unknown command... there is no known key for it");
                }
            }
        }
        public static void SendCommand(string Name, string Args)
        {
            if (Commands.ContainsKey(Name))
            {
                SendCommand(Commands[Name], Args);
            }
            else
            {
                Console.WriteLine("ServerSide Command Not Found :" + Name);
            }
        }
        public static void SendCommand(UInt16 CID, string Args)
        {
            byte[] ProcPacket = new byte[10];
            byte[] FollowUpPacket = new byte[0];
            BitConverter.GetBytes(CID).CopyTo(ProcPacket, 0);
            if (!string.IsNullOrWhiteSpace(Args))
            {
                FollowUpPacket = Encoding.ASCII.GetBytes(Args);
                BitConverter.GetBytes(FollowUpPacket.LongLength).CopyTo(ProcPacket, 2);
            }
            Connection conn = new Connection()
            {
                Soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                allDone = new ManualResetEvent(false),
                FullyDone = new ManualResetEvent(false),
                CID = CID
            };



            conn.Soc.BeginConnect(HostIP, 2460, new AsyncCallback(ConnectCallBack), conn);
            conn.allDone.WaitOne();
            conn.Soc.BeginSend(ProcPacket, 0, 10, SocketFlags.None, new AsyncCallback(SendCallBack), conn);
            conn.allDone.WaitOne();

            if (FollowUpPacket.Length > 0)
            {
                conn.Soc.BeginSend(FollowUpPacket, 0, FollowUpPacket.Length, SocketFlags.None, new AsyncCallback(SendCallBack), conn);
            }

            conn.buffer = new byte[2];
            conn.Soc.BeginReceive(conn.buffer, 0, 2, SocketFlags.None, new AsyncCallback(ProcessReturn), conn);
            conn.FullyDone.WaitOne();
        }
        public static bool VerifyKey(UInt16 CID)
        {
            return Commands.ContainsValue(CID);
        }

        private static void RipCommands(Connection conn, bool Display = true)
        {
            /*GEN INFO
             * Formatting goes as such
             * the first two lines are irelavant
             * the last line is irelavant
             * and everything imbetween is relevant
             * the relevant stuff is as such
             * at the start of the line is the command name
             * and after the '|' is the command id
             * not the '|' is allways on the 38th char
             * and everything imbettween the name and that char
             * is just whitespace
             * 
             * Encoding is ASCII
             * 
             * now due to the way the ReturnStream works youll have a tag 
             * beffore the stuff the process sent so what well have to do
             * is skip to tag this is done by skipping till the ']:' part
             */

            Commands = new Dictionary<string, ushort>();
            using (StreamReader RS = new StreamReader(new NetworkStream(conn.Soc)))
            {
                Thread.Sleep(1000);//wait till a large amount of data is sent
                if (Display)
                {
                    Console.WriteLine(RS.ReadLine());
                    Console.WriteLine(RS.ReadLine());//skip 2 lines
                }
                else
                {
                    RS.ReadLine();
                    RS.ReadLine();
                }
                DateTime TMR = DateTime.Now;
                while (true)
                {
                    if (RS.Peek() > 0 || conn.Soc.Available > 0)
                    {
                        string line = RS.ReadLine();
                        if (Display)
                            Console.WriteLine(line);
                        if (!line.Contains('|')) { break; }
                        line = line.Substring(line.IndexOf("]:") + 2);//skip tag
                        string CommandName = line.Substring(0, line.IndexOf(' '));//read commandname
                        if (!Commands.ContainsKey(CommandName))
                        {
                            line = line.Substring(line.IndexOf("|") + 1);//skip till CID
                            UInt16 CID = UInt16.Parse(line);
                            Commands.Add(CommandName, CID);
                        }
                    }
                    if (DateTime.Now - TMR > TimeSpan.FromSeconds(LastReadTimeoutSeconds))
                    {
                        try
                        {
                            if (!conn.Soc.Connected) break;
                            else
                            {
                                conn.Soc.Send(new byte[1] { 255 });//Verify that the Server is still keeping the Socket open thus the command is still active
                                TMR = DateTime.Now;
                            }
                        }
                        catch
                        {
                            break;
                        }
                    }
                    else Thread.Sleep(20);
                }
            }
            conn.FullyDone.Set();
        }
        private static void ReturnManager(Connection conn)
        {
            string ASCII_Return = "";
            bool END = false;
            try
            {
                while (!END && conn.Soc.Connected)
                {
                    byte[] TransitionBuffer = new byte[BufferSize];
                    conn.buffer = new byte[BufferSize];
                    conn.Soc.Receive(conn.buffer);
                    int x = 0;
                    int TX = 0;
                    for (; x < conn.buffer.Length; x++)
                    {
                        if (conn.buffer[x] == 4)
                        {
                            END = true;
                            break;
                        }
                        else if (conn.buffer[x] != 0)
                        {
                            TransitionBuffer[TX] = conn.buffer[x];
                            TX++;
                        }
                        else break;
                    }
                    conn.buffer = new byte[TX];
                    Array.Copy(TransitionBuffer, 0, conn.buffer, 0, TX);
                    TransitionBuffer = null;

                    if (END)
                    {
                        ASCII_Return += Encoding.ASCII.GetString(conn.buffer, 0, x);
                    }
                    else if (TX > 0)
                    {//[Procname:ThreadID]:{message}\n
                        string cur = Encoding.ASCII.GetString(conn.buffer);
                        if (cur.StartsWith("["))
                        {
                            ASCII_Return += cur;
                        }
                    }
                    string[] lns = ASCII_Return.Split(new char[1] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lns.Length > 1)
                    {
                        for (x = 0; x < lns.Length - 1; x++)
                        {
                            if (lns[x].StartsWith("["))
                            {
                                Console.WriteLine(lns[x]);
                                ASCII_Return = ASCII_Return.Substring(lns[x].Length + 1);
                            }
                        }
                    }
                    else if (ASCII_Return.Contains('\n') && lns.Length == 1)
                    {

                        Console.WriteLine(lns[0]);
                        ASCII_Return = "";
                    }

                }
                if (ASCII_Return.Length > 0)
                {
                    Console.WriteLine(ASCII_Return);
                }
            }
            catch
            {
                Console.WriteLine("ERROR While reading data");
            }
            conn.Soc.Close();
            conn.Soc.Dispose();
            conn.FullyDone.Set();
        }
        private static void READ(IAsyncResult R)
        {
            Connection conn = (Connection)R.AsyncState;
            if (conn.Soc.EndReceive(R) > 0) conn.Successfull = true; else conn.Successfull = false;
            conn.allDone.Set();
        }
        private static void ConnectCallBack(IAsyncResult R)
        {
            Connection conn = (Connection)R.AsyncState;
            conn.Soc.EndConnect(R);
            conn.allDone.Set();
        }
        private static void SendCallBack(IAsyncResult R)
        {
            Connection conn = (Connection)R.AsyncState;
            while (!R.IsCompleted) ;
            conn.Soc.EndSend(R);
            conn.allDone.Set();
        }
        private static void ProcessReturn(IAsyncResult R)
        {
            Connection conn = (Connection)R.AsyncState;
            conn.Soc.EndReceive(R);

            switch (BitConverter.ToInt16(conn.buffer, 0))
            {
                case (1):
                    if (conn.CID == 0)
                    {
                        RipCommands(conn);
                        conn.Soc.Close();
                        conn.Soc.Dispose();
                    }
                    else
                    {
                        ReturnManager(conn);
                    }
                    return;
                case (2):
                    Console.WriteLine("InvalidArgs TO Command");
                    return;
                case (3):
                    Console.WriteLine("InvalidCommandCode . . . Key not found on server");
                    Console.WriteLine("Removing Key from my database");
                    foreach (string x in Commands.Keys)
                    {
                        if (Commands[x] == conn.CID)
                        {
                            Console.WriteLine($"Command:{x} Has been Removed from database");
                            Commands.Remove(x);
                        }
                    }
                    return;
                default:
                    Console.WriteLine("Server Sent an Unknown ReturnCode");
                    return;
            }
        }
    }
}
