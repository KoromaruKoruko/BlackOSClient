using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;

namespace BlackOSClient
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Boot_CmdOperations(args);
            }
            else
            {
                Boot_UserOperations();
            }
            Console.ReadKey();//Debugg
        }
        private static string GetHost()
        {
            Console.Write("Host:");
            string ip = Console.ReadLine();
            IPAddress addr;
            if (IPAddress.TryParse(ip, out addr))
            {
                return ip;
            }
            else
            {
                try
                {
                    IPAddress[] ips = Dns.GetHostAddresses(ip);
                    if (ips.Length > 0)
                        return ip;
                    else
                    {
                        Console.WriteLine("Host doesnt exist");
                        return GetHost();
                    }
                }
                catch
                {
                    Console.WriteLine("Host doesnt exist");
                    return GetHost();
                }
            }
        }
        private static void Boot_UserOperations()
        {
            string Host = "192.168.178.26";
            //Host = GetHost();
            ClientCommands.INIT();
            if (CommandSender.INIT(Host))
            {
                while (true)
                {
                    Console.Write($"BlackOS@{Host}>");
                    string input = Console.ReadLine();
                    if (input.StartsWith("/"))
                        ClientCommands.ExecuteCommand(input.Substring(1));
                    else
                        CommandSender.ExecCommand(input);
                }
            }
            else
            {
                Console.WriteLine("Failed to INIT CommandSender");
            }
            Console.ReadKey();
        }

        struct CommandObjectiv
        {
            public bool isLocalCommand;
            public string LocalCommandName;
            public bool isRemoteCommandByName;
            public string RemoteCommandName;
            public UInt16 RemoteCommandCID;
            public string Args;
        }
        private static void Boot_CmdOperations(string[] args)
        {
            if (args[0] == "-h" || args[0] == "--help")
                DisplayHelpTextForCommandExecution();
            else
            {
                string Host = args[0];
                CommandSender.INIT(Host,false);
                ClientCommands.INIT(false);
                Queue<CommandObjectiv> Objectivs = new Queue<CommandObjectiv>();


                string Args = args[1];
                for (int x = 2; x < args.Length; x++)
                {
                    Args += " " + args[x];
                }
                //hint string is char array
                //you can use a string just like you would a char array
                for (int x = 0; x < Args.Length; x++)
                {
                    if (Args[x] == '-')
                    {
                        x++;
                        if (Args[x] == 'c')
                        {
                            x += 2;
                            if (Args[x] == '(')
                            {
                                x++;
                                int EndOfCommand = Args.Substring(x).IndexOf(")");
                                string[] commandArgs = Args.Substring(x, EndOfCommand).Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                x += EndOfCommand + 1;
                                CommandObjectiv cmd = new CommandObjectiv()
                                {
                                    isLocalCommand = true,
                                    Args = "",
                                    isRemoteCommandByName = false,
                                    RemoteCommandCID = 0,
                                    RemoteCommandName = null,
                                };

                                if (ClientCommands.VerifyCommandExists(commandArgs[0]))
                                {
                                    cmd.LocalCommandName = commandArgs[0];
                                }
                                else
                                {
                                    Console.WriteLine("Command Doesnt Exist:" + commandArgs[0]);
                                    Environment.Exit(1);
                                }
                                if (commandArgs.Length > 1)
                                {
                                    cmd.Args += commandArgs[1];
                                    for (int y = 2; y < commandArgs.Length; y++)
                                        cmd.Args += " " + commandArgs[y];
                                }
                                Objectivs.Enqueue(cmd);

                            }
                            else
                            {
                                Console.WriteLine("Invalid Char at " + x);
                                Environment.Exit(1);
                            }
                        }
                        else if (Args[x] == 's')
                        {
                            x += 2;
                            if (Args[x] == '(')
                            {
                                x++;
                                int EndOfCommand = Args.Substring(x).IndexOf(")");
                                string[] commandArgs = Args.Substring(x, EndOfCommand).Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                x += EndOfCommand + 1;
                                CommandObjectiv cmd = new CommandObjectiv()
                                {
                                    isLocalCommand = false,
                                    LocalCommandName = null,
                                    Args = "",
                                };
                                if (UInt16.TryParse(commandArgs[0], out cmd.RemoteCommandCID))
                                {
                                    cmd.isRemoteCommandByName = false;
                                    cmd.RemoteCommandName = null;
                                }
                                else
                                {
                                    cmd.isRemoteCommandByName = true;
                                    cmd.RemoteCommandName = commandArgs[0];
                                }
                                if (commandArgs.Length > 1)
                                {
                                    cmd.Args += commandArgs[1];
                                    for (int y = 2; y < commandArgs.Length; y++)
                                        cmd.Args += " " + commandArgs[y];
                                }
                                Objectivs.Enqueue(cmd);
                            }
                            else
                            {
                                Console.WriteLine("Invalid Char at " + x);
                                Environment.Exit(1);
                            }
                        }
                        else if (Args[x] == 'h')
                        {
                            DisplayHelpTextForCommandExecution();
                            Environment.Exit(0);
                        }
                        else
                        {
                            Console.WriteLine("Invalid Char at " + x);
                            Environment.Exit(1);
                        }
                    }
                }

                
                while(Objectivs.Count > 0)
                {
                    CommandObjectiv objectiv = Objectivs.Dequeue();

                    if (objectiv.isLocalCommand)
                    {
                        ClientCommands.ExecuteCommand(objectiv.LocalCommandName + " " + objectiv.Args);
                    }
                    else
                    {
                        if (objectiv.isRemoteCommandByName)
                        {
                            CommandSender.SendCommand(objectiv.RemoteCommandName, objectiv.Args);
                        }
                        else
                        {
                            CommandSender.SendCommand(objectiv.RemoteCommandCID, objectiv.Args);
                        }
                    }
                }
            }
        }
        private static void DisplayHelpTextForCommandExecution()
        {
            Console.WriteLine("HelpText::");
            Console.WriteLine("this is a multi functunal programm you can use it in command line or as user operated client");
            Console.WriteLine("to use this as a client just execute it without any args");
            Console.WriteLine("to use this in cmd mode you need to construct all args in a speciffic way");
            Console.WriteLine("as seen bellow");
            Console.WriteLine("");
            Console.WriteLine("<Host> is the Server IP/Domain/Hostname");
            Console.WriteLine("");
            Console.WriteLine("BlackOSClient <Host> -c (Help)");
            Console.WriteLine("this will execute the client side help command");
            Console.WriteLine("");
            Console.WriteLine("BlackOSClient <Host> -s (4)");
            Console.WriteLine("This will execute the serverside command at that location");
            Console.WriteLine("");
            Console.WriteLine("BlackOSClient <Host> -s (4,-a)");
            Console.WriteLine("This will execute the serverside command with args \"-a\", the same can be done with any client side commands");
            Console.WriteLine("");
            Console.WriteLine("BlackOSClient <Host> -s (ReadSensors,-a)");
            Console.WriteLine("This will exec the command ReadSensors with args \"-a\"");
            Console.WriteLine("");
            Console.WriteLine("now beffore you start asking why the commands are constructed in such a way, its because this makes it possable to do multiple consecutiv commands");
            Console.WriteLine("EG:");
            Console.WriteLine("BlackOSClient <Host> -s (3,300) -c (Sleep,1000) -s (7) -s (4,-a)");
            Console.WriteLine("All commands given are executed in order");
            Console.WriteLine("For a list of commands just execute the client side command 'Help' and on server side 'ListCommands'");
        }
    }
}
