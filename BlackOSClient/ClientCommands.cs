using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BlackOSClient
{
    struct command
    {
        public Action<string[]> Func;
        public string HelpText;
    }
    public static class ClientCommands
    {
        private static Dictionary<string, command> Commands;
        public static void INIT(bool Display=true)
        {
            Commands = new Dictionary<string, command>();
            CreateCommand("Help", new Action<string[]>(Help), "Displays all commands with help message");
            CreateCommand("Quit", new Action<string[]>(Quit), "Exits the Client");
            CreateCommand("Clear", new Action<string[]>(Clear), "Clears Screen");
            CreateCommand("QSEND", new Action<string[]>(QSEND), "Sends a Command Based on CID");
            CreateCommand("Sleep", new Action<string[]>(Sleep), "Sleep for x, used in cmd operations");
            CreateCommand("ListCmds", new Action<string[]>(ListCmds), "Lists all remtoe commands known");

            if (Display)
                Console.WriteLine("/Help for Client Commands");
        }
        public static void CreateCommand(string CommandName, Action<string[]> Func, string HelpText)
        {
            Commands.Add(CommandName, new command() { Func = Func, HelpText = HelpText });
        }
        public static void ExecuteCommand(string Input)
        {
            string[] Args = Input.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string Command = Args[0];

            {
                string[] TMP = Args;
                Args = new string[Args.Length - 1];
                Array.Copy(TMP, 1, Args, 0, Args.Length);
            }//Crop Args to remove first Entry

            if (Commands.ContainsKey(Command))
            {
                Commands[Command].Func.Invoke(Args);
            }
            else
            {
                Console.WriteLine("Command Desnt Exist");
                Console.WriteLine("/Help for a list of commands");
            }
            Console.WriteLine();
        }
        public static bool VerifyCommandExists(string command)
        {
            return Commands.ContainsKey(command);
        }

        private static void Help(string[] Args)
        {
            foreach (string com in Commands.Keys)
            {
                Console.WriteLine($"{com} :{Commands[com].HelpText}");
            }
        }
        private static void Quit(string[] Args)
        {
            Environment.Exit(0);
        }
        private static void Clear(string[] Args)
        {
            Console.Clear();
        }
        private static void QSEND(string[] Args)
        {
            if (Args.Length > 0)
            {
                UInt16 CID;
                if (UInt16.TryParse(Args[0], out CID))
                {
                    if (CommandSender.VerifyKey(CID))
                    {
                        string args = "";
                        for (int x = 1; x < Args.Length; x++)
                        {
                            args += " " + Args[x];
                        }
                        CommandSender.SendCommand(CID, args);
                    }
                    else
                    {
                        Console.WriteLine("Invalid CID");
                    }
                }
                else
                {
                    Console.WriteLine("A CID is Required");
                }
            }
            else
            {
                Console.WriteLine("A CID is Required");
            }
        }
        private static void Sleep(string[] Args)
        {
            if (Args.Length > 0)
            {
                int STime = 0;
                if (int.TryParse(Args[0], out STime))
                {
                    Console.WriteLine("Sleeping for {0}ms", STime);
                    Thread.Sleep(STime);
                }
                else
                {
                    Console.WriteLine("Incorrect Format");
                }
            }
            else
            {
                Console.WriteLine("missing Args");
                Console.WriteLine("HelpText:");
                Console.WriteLine("Sleep [Time]");
                Console.WriteLine("Makes the client sleep for X amount of time");
            }
        }
        private static void ListCmds(string[] Args)
        {
            Console.WriteLine("Name-----------------------------------ID---");
            Console.WriteLine("############################################");
            foreach (string key in CommandSender.CMDs.Keys)
            {
                string name = key;
                for (int y = name.Length; y < 37; y++) { name += " "; }
                name += "|";
                Console.WriteLine(name + CommandSender.CMDs[key]);
            }
            Console.WriteLine("############################################");
        }
    }
}
