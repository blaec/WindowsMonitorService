using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;
using System.Xml;

namespace monitorservice
{
    public partial class monitorservice : ServiceBase
    {
        private const string SERVICE_NAME = "MonitorService";
        private const string APP_NAME = "Application";

        private string homeDir = (new System.IO.DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory)).FullName.Trim();
        private string sourcePath = "";
        private string destinationPath = "";
        private int weekDay = 0;
        private string time = "";
        private bool isReady = false;
        private backupfiles BackupEngine = new backupfiles();

        public Timer ServiceTimer { get; set; } = null;

        public monitorservice()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Bounding the service to the Windows Event Log and write the log
            if (!EventLog.SourceExists(SERVICE_NAME))
            {
                EventLog.CreateEventSource(SERVICE_NAME, APP_NAME);
            }
            LogEvent($"{SERVICE_NAME} starts on {DateTime.Now:yyyy-MM-dd hh:mm:ss tt}", EventLogEntryType.Information);

            CheckParameters(); //Need to load service behavior parameters

            /// In this case a Timer object is instantiated and started.
            /// Every 300 milliseconds, the Elapsed event of the timer will be fired and the timer_Elapsed method will be executed.
            /// The OnStop event will release this resource.
            ServiceTimer = new Timer(300)
            {
                AutoReset = true
            };
            ServiceTimer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            ServiceTimer.Start();
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (isReady)
            {
                if (weekDay != 0) //Need to know if current weekday matches parameter's weekday
                {
                    if ((int)DateTime.Now.DayOfWeek + 1 != weekDay)
                    {
                        return;
                    }
                }
                if (DateTime.Now.TimeOfDay < TimeSpan.Parse(time)) //If current daytime is earlier than defined in parameters, the process is stoped
                {
                    return;
                }
                if (BackupEngine.isBusy) //If backup process was previously started we do nothing
                {
                    return;
                }
                LogEvent($"{SERVICE_NAME} start backup", EventLogEntryType.Information);
                try
                {
                    BackupEngine.sourcePath = sourcePath;
                    BackupEngine.destinationPath = destinationPath;
                    BackupEngine.DoBackup();
                }
                catch (Exception ex)
                {
                    LogEvent($"{SERVICE_NAME} failed to backup {ex.Message}", EventLogEntryType.Error);
                }
                LogEvent($"{SERVICE_NAME} backup complete", EventLogEntryType.Information);
            }
        }

        /// <summary>
        /// This code stops the Timer object execution and disposes it before service execution stops.
        /// </summary>
        protected override void OnStop()
        {
            ServiceTimer.Stop();
            ServiceTimer.Dispose();
            ServiceTimer = null;

            LogEvent($"{SERVICE_NAME} stops on {DateTime.Now:yyyy-MM-dd hh:mm:ss tt}", EventLogEntryType.Information);
        }

        private void LogEvent(string message, EventLogEntryType entryType)
        {
            EventLog eventLog = new EventLog
            {
                Source = SERVICE_NAME,
                Log = APP_NAME
            };
            eventLog.WriteEntry(message, entryType);
        }

        private void CheckParameters()
        {
            string paramsFolder = homeDir + "\\parameters";
            string paramsFile = paramsFolder + "\\srvparams.xml";
            isReady = false;
            if (!System.IO.Directory.Exists(paramsFolder))
            {
                System.IO.Directory.CreateDirectory(paramsFolder);
                LogEvent($"{SERVICE_NAME}: parameters file folder {paramsFolder} was just been created", EventLogEntryType.Information);
            }
            else
            {
                if (!System.IO.File.Exists(paramsFile))
                {
                    LogEvent($"Backup Service parameters file {paramsFile} doesn't exist", EventLogEntryType.Error);
                }
                else
                {
                    bool docParsed = true;
                    XmlDocument docParameters = new XmlDocument();
                    try
                    {
                        docParameters.Load(paramsFile);
                    }
                    catch (XmlException ex)
                    {
                        docParsed = false;
                        LogEvent($"Parameters file couldn't be read: {ex.Message}", EventLogEntryType.Error);
                    }
                    if (docParsed)
                    {
                        XmlNode backupParameters = docParameters.ChildNodes.Item(1).ChildNodes.Item(0);
                        sourcePath = getAttr(backupParameters, "source");
                        destinationPath = getAttr(backupParameters, "destination");
                        weekDay = Convert.ToInt32(getAttr(backupParameters, "dayofweek"));
                        time = getAttr(backupParameters, "hour");
                        isReady = true;

                        LogEvent("Backup Service parameters were loaded", EventLogEntryType.Information);
                    }
                }
            }
        }

        private string getAttr(XmlNode backupParameters, string itemName)
        {
            return backupParameters.Attributes.GetNamedItem(itemName).Value.Trim();
        }
    }
}
