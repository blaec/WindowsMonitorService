using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace monitorservice
{
    public partial class monitorservice : ServiceBase
    {
        private const string SERVICE_NAME = "MonitorService";
        private const string APP_NAME = "Application";

        private string HomeDir = (new System.IO.DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory)).FullName.Trim();
        private string source_path = "";
        private string destination_path = "";
        private int weekday = 0;
        private string time = "";
        private Boolean IsReady = false;
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
            LogEvent(string.Format($"{SERVICE_NAME} starts on {DateTime.Now:yyyy-MM-dd hh:mm:ss tt}"),
                     EventLogEntryType.Information);

            this.check_parameters(); //Need to load service behavior parameters

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

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!this.IsReady)
            {
                return;
            }
            if (this.weekday != 0) //Need to know if current weekday matches parameter's weekday
{
                if (((int)System.DateTime.Now.DayOfWeek) + 1 != this.weekday)
                {
                    return;
                }
            }
            if (DateTime.Now.TimeOfDay < System.TimeSpan.Parse(this.time)) //If current daytime is earlier than defined in parameters, the process is stoped
            {
                return;
            }
            if (this.BackupEngine.IsBusy) //If backup process was previously started we do nothing
            {
                return;
            }
            this.BackupEngine.source_path = this.source_path;
            this.BackupEngine.destination_path = this.destination_path;
            this.BackupEngine.DoBackup();
        }

        /// <summary>
        /// This code stops the Timer object execution and disposes it before service execution stops.
        /// </summary>
        protected override void OnStop()
        {
            ServiceTimer.Stop();
            ServiceTimer.Dispose();
            ServiceTimer = null;

            LogEvent(string.Format($"{SERVICE_NAME} stops on {DateTime.Now:yyyy-MM-dd hh:mm:ss tt}"),
                     EventLogEntryType.Information);
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

        private void check_parameters()
        {
            if (!System.IO.Directory.Exists(this.HomeDir + "\\parameters"))
            {
                System.IO.Directory.CreateDirectory(this.HomeDir + "\\parameters");
                this.LogEvent(String.Format("MonitorService: parameters file folder was just been created"), EventLogEntryType.Information);
                this.IsReady = false;
            }
            else
            {
                if (System.IO.File.Exists(this.HomeDir + "\\parameters\\srvparams.xml"))
                {
                    Boolean docparsed = true;
                    XmlDocument parametersdoc = new XmlDocument();
                    try
                    {
                        parametersdoc.Load(this.HomeDir + "\\parameters\\srvparams.xml");
    }
                    catch (XmlException ex)
                    {
                        docparsed = false;
                        this.IsReady = false;
                        this.LogEvent(String.Format("Parameters file couldn't be read: {0}", ex.Message), EventLogEntryType.Error);
                    }
                    if (docparsed)
                    {
                        XmlNode BackupParameters = parametersdoc.ChildNodes.Item(1).ChildNodes.Item(0);
                        this.source_path = BackupParameters.Attributes.GetNamedItem("source").Value.Trim();
                        this.destination_path = BackupParameters.Attributes.GetNamedItem("destination").Value.Trim();
                        this.weekday = Convert.ToInt32(BackupParameters.Attributes.GetNamedItem("dayofweek").Value.Trim());
                        this.time = BackupParameters.Attributes.GetNamedItem("hour").Value.Trim();
                        this.IsReady = true;

                        this.LogEvent(String.Format("Backup Service parameters were loaded"), EventLogEntryType.Information);
                    }
                    parametersdoc = null;
                }
                else
                {
                    this.LogEvent(String.Format("Backup Service parameters file doesn't exist"), EventLogEntryType.Error);
                    this.IsReady = false;
                }
            }
        }
    }
}
