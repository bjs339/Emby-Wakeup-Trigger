﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Win32.TaskScheduler;    // http://taskscheduler.codeplex.com/
using Newtonsoft.Json;


namespace EmbyWakeupTrigger
{

    public class Item
    {
        public string Id;
        public string ChannelId;
        public string Name;
        public string Overview;
        public string StartDate;
        public string EndDate;
        public string Status;
        public int PrePaddingSeconds;
        public int PostPaddingSeconds;
        public bool IsPrePaddingRequired;
        public bool IsPostPaddingRequired;
        public int Priority;
    }

    public partial class EmbyWakeupService : ServiceBase
    {
        RecordingReader recordingReader = new RecordingReader();
        public string timerPath = string.Empty;
        protected FileSystemWatcher watcher;        

        public EmbyWakeupService(string[] args)
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            
            timerPath = string.Concat(recordingReader.GetEmbyDirectory(), "\\data\\livetv\\timers.json");            

            if (!File.Exists(timerPath))
            {
                EventLog.WriteEntry("Emby Wakeup Trigger", string.Format("File {0} does not exist.", timerPath));
                Environment.Exit(1);
            }

            watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(timerPath);
            watcher.Filter = Path.GetFileName(timerPath);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += new FileSystemEventHandler(WatcherChanged);
            watcher.EnableRaisingEvents = true;
        }
        
        public void WatcherChanged(object source, FileSystemEventArgs e)
        {
            recordingReader.ReadRecordings(timerPath);
        }

        protected override void OnStop()
        {
        }
    }

    public class RecordingReader
    {
        public string GetEmbyDirectory()
        {
            string embyPath = string.Empty;

            try
            {
                // First, see if there's a config file with the path to timers.json
                string configPath = string.Concat(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "\\config.txt");
                if (File.Exists(configPath))
                {
                    string[] lines = File.ReadAllLines(configPath);
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    for (int i = 0; i < lines.Length; i++)
                    {
                        dict.Add(lines[i].Split('=')[0].Trim(), lines[i].Split('=')[1].Trim());
                    }
                    dict.TryGetValue("timerPath", out embyPath);
                }
                else
                {
                    ServiceController[] serviceController;
                    serviceController = ServiceController.GetServices();

                    foreach (ServiceController service in serviceController)
                    {
                        if (service.ServiceName == "Emby")
                        {
                            ManagementObject wmiService = new ManagementObject("Win32_Service.Name='" + service.ServiceName + "'");
                            wmiService.Get();
                            string servicePath = wmiService["PathName"].ToString().Replace("\"", "");
                            embyPath = Directory.GetParent(Path.GetDirectoryName(servicePath)).FullName;
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                EventLog.WriteEntry("Emby Wakeup Trigger", ex.Message);
            }

            return embyPath;
        }

        public void ReadRecordings(string timerPath)
        {
            ScheduledTaskManager scheduledTaskManager = new ScheduledTaskManager();

            // Pause to avoid conflict with Emby
            if (!Environment.UserInteractive)
            {
                Thread.Sleep(2000);
            }

            try
            {
                string json = string.Empty;
                // Open the file and read the json obj
                using (StreamReader r = new StreamReader(timerPath))
                {
                    json = r.ReadToEnd();
                }
                List<Item> items = JsonConvert.DeserializeObject<List<Item>>(json);
                List<Item> newItems = items.FindAll(x => x.Status.Equals("New"));
                foreach (Item item in newItems)
                {
                    if (scheduledTaskManager.ScheduledTaskExists(item))
                    {
                        scheduledTaskManager.UpdateScheduledTask(item);
                    }
                    else
                    {
                        scheduledTaskManager.CreateScheduledTask(item);
                    }
                }
            }

            catch (Exception ex)
            {
                EventLog.WriteEntry("Emby Wakeup Trigger", ex.Message);
            }
        }
    }

    public class ScheduledTaskManager
    {

        public bool ScheduledTaskExists(Item item)
        {
            TaskService taskService = new TaskService();
            Task task = taskService.GetTask(item.Id);
            if (task == null)
                return false;
            else
                return true;
        }

        public void CreateScheduledTask(Item item)
        {
            // Get the datetime from the last (most recently added) item
            DateTime startTime;
            if (DateTime.TryParse(item.StartDate, out startTime))
            {
                // Create a new task
                TaskService taskService = new TaskService();
                TaskDefinition taskDefinition = taskService.NewTask();
                taskDefinition.RegistrationInfo.Description =
                    string.Format("Wakeup trigger for {0}: {1}", item.Name, item.Overview);

                // Create the trigger
                TimeTrigger timeTrigger = new TimeTrigger();
                timeTrigger.StartBoundary =
                    startTime.AddSeconds(Convert.ToDouble(item.PrePaddingSeconds * -1));
                timeTrigger.EndBoundary =
                    startTime.AddSeconds(Convert.ToDouble((item.PrePaddingSeconds * -1) + 60));
                taskDefinition.Triggers.Add(timeTrigger);

                // Create an action that does nothing - just opens and closes a command prompt
                taskDefinition.Actions.Add("cmd.exe", "/c exit");

                // Settings
                //taskDefinition.Settings.Compatibility = TaskCompatibility.V2_1;
                taskDefinition.Settings.DeleteExpiredTaskAfter = TimeSpan.FromMinutes(1);
                //taskDefinition.Settings.RunOnlyIfLoggedOn = false;
                taskDefinition.Settings.WakeToRun = true;

                // Register the task in the root folder
                TaskService.Instance.RootFolder.RegisterTaskDefinition(item.Id, taskDefinition);
            }
        }

        public void UpdateScheduledTask(Item item)
        {
            TaskService taskService = new TaskService();
            Task task = taskService.GetTask(item.Id);
            if (task != null)
            {
                DateTime startTime;
                if (DateTime.TryParse(item.StartDate, out startTime))
                {
                    startTime = startTime.AddSeconds(Convert.ToDouble(item.PrePaddingSeconds * -1));

                    if (startTime != task.NextRunTime)
                    {
                        DeleteScheduledTask(item);
                        CreateScheduledTask(item);
                    }
                }
            }
        }

        public void DeleteScheduledTask(Item item)
        {
            TaskService taskService = new TaskService();
            Task task = taskService.GetTask(item.Id);
            if (task != null)
                TaskService.Instance.RootFolder.DeleteTask(item.Id);
        }
    }
}
