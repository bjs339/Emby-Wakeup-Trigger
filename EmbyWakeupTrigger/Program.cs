using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace EmbyWakeupTrigger
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (!EventLog.SourceExists("Emby Wakeup Trigger"))
            {
                EventLog.CreateEventSource("Emby Wakeup Trigger", "Application");
            }

            if (Environment.UserInteractive)
            {
                EmbyWakeupTrigger embyWakeupTrigger = new EmbyWakeupTrigger(args);
                //EventLog.WriteEntry("Emby Wakeup Trigger", "Starting up");

                // To debug getting the timers.json location
                //embyWakeupTrigger.GetEmbyDirectory();

                // To debug reading the json and creating the scheduled task
                embyWakeupTrigger.timerPath = "E:\\Workspace\\Emby Wakeup Trigger\\test\\timers.json";
                object x = new object();
                FileSystemEventArgs e = new FileSystemEventArgs(WatcherChangeTypes.Changed, "", "");
                embyWakeupTrigger.WatcherChanged(x, e);
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new EmbyWakeupTrigger(args)
                };
                ServiceBase.Run(ServicesToRun);
            }            
        }
    }
}
