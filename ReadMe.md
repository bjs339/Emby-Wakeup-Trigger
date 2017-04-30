### Emby Wakeup Trigger

The Emby Wakeup Trigger wakes a computer running Emby Server to record scheduled TV shows.

It is a Windows Service that watches for changes in the timers.json file that Emby uses to store scheduling information. When a new recording is added, it creates a Windows Scheduled Task to wake the computer, allowing Emby to record the show.

#### Requirements:
- Windows - tested on Windows 7 and 8.1; probably works on 7, 8, and 10 at least
- .NET Framework 4.6.2 (also required by Emby Server)

#### Installation:
- Copy to target computer
- Open elevated command prompt
- Execute install.bat to install and start service.
- (Execute uninstall.bat to uninstall)

#### Limitations:
- Does not remove the scheduled task if recording is cancelled.