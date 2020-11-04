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

        private string homeDir = (new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory)).FullName.Trim();
        private int backupWeekDay = default;
        private string afterTime = default;
        private bool isReady = default;
        private BackupFiles BackupEngine;

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
                if ((int)DateTime.Now.DayOfWeek + 1 == backupWeekDay        // Need to know if current weekday matches parameter's weekday
                    && DateTime.Now.TimeOfDay >= TimeSpan.Parse(afterTime)  // If current daytime is earlier than defined in parameters, the process is stoped
                    && !BackupEngine.IsBusy)                                // If backup process was previously started we do nothing
                {
                    bool isBackupSuccessful = false;
                    string errMessage = $"{SERVICE_NAME} failed to backup";
                    try
                    {
                        isBackupSuccessful = BackupEngine.DoBackup();
                    }
                    catch (Exception ex)
                    {
                        errMessage = $"{errMessage} {ex.Message}";
                    }

                    // Log backup result
                    if (!isBackupSuccessful)
                    {
                        LogEvent(errMessage, EventLogEntryType.Error);
                    }
                }
            }
        }

        /// <summary>
        /// This code stops the Timer object execution and disposes it before service execution stops.
        /// </summary>
        protected override void OnStop()
        {
            isReady = default;

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
                        BackupEngine = new BackupFiles(
                            getAttr(backupParameters, "source"),
                            getAttr(backupParameters, "destination"));
                        backupWeekDay = Convert.ToInt32(getAttr(backupParameters, "dayofweek"));
                        afterTime = getAttr(backupParameters, "hour");
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
