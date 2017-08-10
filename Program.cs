using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace HeStart
{
    class Program
    {
        static string path;
        static string startArgs;
        static Process proc;
        static Thread LoopThread;
        static bool ServerStarted = false;
        static bool BreakLoop = false;

        static void Main(string[] args)
        {
            // load config

            // start server
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                path = @"/usr/bin/screen";
                startArgs = @"-S ttt /home/steam/gmod1/ttt.sh";
            }
            else
            {
                path = @"C:\gmod\ttt.bat";
                startArgs = "";
            }

            StartServer();

            while (true)
            {
                Thread.Sleep(1000);
            }
		}

        static void StartServer()
        {
            ServerStarted = false;
            SQuery.DropCount = 0;

            proc = Process.Start(new ProcessStartInfo()
            {
                FileName = path,
                Arguments = startArgs
            });

            LoopThread = new Thread(() => MainLoop());
            LoopThread.Start();
        }

        static void StopServer()
        {
            if (!proc.HasExited)
            {
                proc.Kill();
            }
            Thread.Sleep(1000);
            StartServer();
        }

        static async void MainLoop()
        {
            while (!BreakLoop)
            {
                List<string> IPs = new List<string>();
                IPs.Add("31.220.44.229:27015");

                var data = await SQuery.A2SQuery(IPs);

                foreach (A2SInfoData server in data)
                {
                    SQuery.DroppedLast = false;
                    Console.WriteLine("Response from {0}: {1}/{2} players", server.IP, server.Players, server.Maxplayers, SQuery.DropCount);

                    if (!ServerStarted)
                    {
                        SQuery.DropCount = 0;
                        ServerStarted = true;
                    }
                }

                if (SQuery.DropCount > 5 && ServerStarted)
                {
                    Console.WriteLine("Looks like the server froze, restarting...");
                    StopServer();
                    break;
                }

                if (!SQuery.DroppedLast && ServerStarted)
                {
                    SQuery.DropCount = 0;
                }

                if (ServerStarted)
                {
                    Console.WriteLine("Drop Count: {0}", SQuery.DropCount);
                }
                else
                {
                    Console.WriteLine("Server hasn't started yet, waiting before counting any drops");
                }

                Thread.Sleep(2500);
            }
        }
    }
}
