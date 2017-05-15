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

#if DEBUG
            EmbyWakeupService embyWakeupService = new EmbyWakeupService(args);
            //EventLog.WriteEntry("Emby Wakeup Trigger", "Starting up");

            // To debug getting the timers.json location
            //embyWakeupTrigger.GetEmbyDirectory();

            // To debug reading the json and creating the scheduled task
            embyWakeupService.timerPath = "E:\\Workspace\\Emby Wakeup Trigger\\test\\timers.json";
            object x = new object();
            FileSystemEventArgs e = new FileSystemEventArgs(WatcherChangeTypes.Changed, "", "");
            embyWakeupService.WatcherChanged(x, e);
#else

            if (args.Length > 0 && args[0] == "--service")
            {
                // Run as a service
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new EmbyWakeupService(args)
                };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                // Don't use service; instead, run one time and then exit
                RecordingReader recordingReader = new RecordingReader();
                string timerPath = string.Concat(recordingReader.GetEmbyDirectory(), "\\data\\livetv\\timers.json");
                recordingReader.ReadRecordings(timerPath);
            }

#endif
        }
    }
}
