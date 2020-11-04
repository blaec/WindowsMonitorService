using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.ServiceProcess;
using System.Linq;

namespace MonitorServiceGUI
{
    public partial class BackupUI : Form
    {
        string HomeDir = Path.GetDirectoryName(Application.ExecutablePath).Trim();
        public BackupUI()
        {
            InitializeComponent();
        }

        private bool check_parameters()
        {
            Boolean result = default(Boolean);
            if (!System.IO.Directory.Exists(this.HomeDir + "\\parameters"))
            {
                System.IO.Directory.CreateDirectory(this.HomeDir + "\\parameters");
                result = false;
            }
            else
            {
                if (System.IO.File.Exists(this.HomeDir + "\\parameters\\srvparams.xml"))
                {
                    result = true;
                    XmlDocument parametersdoc = new XmlDocument();
                    try
                    {
                        parametersdoc.Load(this.HomeDir + "\\parameters\\srvparams.xml");
                    }
                    catch
                    {
                        result = false;
                    }
                    if (result)
                    {
                        XmlNode BackupParameters = parametersdoc.ChildNodes.Item(1).ChildNodes.Item(0);
                        this.textBox1.Text = BackupParameters.Attributes.GetNamedItem("source").Value.Trim();
                        this.textBox1.Refresh();
                        this.textBox2.Text = BackupParameters.Attributes.GetNamedItem("destination").Value.Trim();
                        this.textBox2.Refresh();
                        this.comboBox1.SelectedIndex = Convert.ToInt32(BackupParameters.Attributes.GetNamedItem("dayofweek").Value.Trim());
                        this.comboBox1.Refresh();
                        this.maskedTextBox1.Text = BackupParameters.Attributes.GetNamedItem("hour").Value.Trim();
                        this.maskedTextBox1.Refresh();
                    }
                    parametersdoc = null;
                }
                else
                {
                    result = false;
                }
            }
            return (result);
        }

        private void BackupUI_Load(object sender, EventArgs e)
        {
            if (!check_parameters())
            {
                comboBox1.SelectedIndex = 0;
                comboBox1.Refresh();
                maskedTextBox1.Text = "00:00";
                maskedTextBox1.Refresh();
                textBox1.Text = "";
                textBox1.Refresh();
                textBox2.Text = "";
                textBox2.Refresh();
            }
        }

        private void maskedTextBox1_TypeValidationCompleted(object sender, TypeValidationEventArgs e)
        {
            if (!e.IsValidInput)
            {
                e.Cancel = true;
            }
        }

        private void textBox1_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.textBox1.Text.Trim().Length == 0)
            {
                e.Cancel = true;
            }
            else
            {
                if (!System.IO.Directory.Exists(this.textBox1.Text.Trim()))
                {
                    MessageBox.Show("The Source Path entered doesn't exist.", "Backup Service Interface");
                    e.Cancel = true;
                }
            }
        }

        private void textBox2_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.textBox2.Text.Trim().Length == 0)
            {
                e.Cancel = true;
            }
            else
            {
                if (!System.IO.Directory.Exists(this.textBox2.Text.Trim()))
                {
                    MessageBox.Show("The Destination Path entered doesn't exist.", "Backup Service Interface");
                    e.Cancel = true;
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            this.Save_Parameters();
            this.Notify_Changes();
            this.Close();
        }

        private void Save_Parameters()
        {
            XmlDocument oparamsxml = new XmlDocument();
            XmlProcessingInstruction _xml_header = oparamsxml.CreateProcessingInstruction("xml", "version = '1.0' encoding = 'UTF-8'");
            oparamsxml.InsertBefore(_xml_header, oparamsxml.ChildNodes.Item(0));
            XmlNode parameters = oparamsxml.CreateNode(XmlNodeType.Element, "Parameters", "");
            XmlNode backup = oparamsxml.CreateNode(XmlNodeType.Element, "Backup", "");
            XmlAttribute attribute = oparamsxml.CreateAttribute("source");
            attribute.Value = this.textBox1.Text.Trim();
            backup.Attributes.Append(attribute);
            attribute = oparamsxml.CreateAttribute("destination");
            attribute.Value = this.textBox2.Text.Trim();
            backup.Attributes.Append(attribute);
            attribute = oparamsxml.CreateAttribute("dayofweek");
            attribute.Value = this.comboBox1.SelectedIndex.ToString("00");
            backup.Attributes.Append(attribute);
            attribute = oparamsxml.CreateAttribute("hour");
            attribute.Value = this.maskedTextBox1.Text.Trim();
            backup.Attributes.Append(attribute);
            parameters.AppendChild(backup);
            oparamsxml.AppendChild(parameters);
            if (!Directory.Exists(this.HomeDir + "\\parameters"))
            {
                Directory.CreateDirectory(this.HomeDir + "\\parameters");
            }
            oparamsxml.Save(this.HomeDir + "\\parameters\\srvparams.xml");
        }

        private void Notify_Changes()
        {
            ServiceController controller = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == "MonitorService");
            if (controller != null) //The service is installed
            {
                if (controller.Status == ServiceControllerStatus.Running) //The service is running, so it needs to be stopped and started again to reload the parameters
                {
                    controller.Stop(); //Stops the service
                    controller.WaitForStatus(ServiceControllerStatus.Stopped); //Waits until the service is really stoppedcontroller.Start(); //Starts the service and reload the parameters
                    controller.Start(); //Starts the service and reload the parameters
                }
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
