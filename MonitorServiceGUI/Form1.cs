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
        private static string homeDir = Path.GetDirectoryName(Application.ExecutablePath).Trim();
        private static string paramsFolder = homeDir + "\\parameters";
        private static string paramsFile = paramsFolder + "\\srvparams.xml";

        public BackupUI()
        {
            InitializeComponent();
        }

        private void BackupUI_Load(object sender, EventArgs e)
        {
            if (!checkParameters())
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

        private bool checkParameters()
        {
            bool result = default;

            if (!Directory.Exists(paramsFolder))
            {
                Directory.CreateDirectory(paramsFolder);
            }
            else
            {
                if (File.Exists(paramsFile))
                {
                    
                    XmlDocument docParameters = new XmlDocument();
                    try
                    {
                        docParameters.Load(paramsFile);
                        result = true;
                    }
                    finally 
                    {
                        if (result)
                        {
                            XmlNode backupParameters = docParameters.ChildNodes.Item(1).ChildNodes.Item(0);
                            textBox1.Text = getAttr(backupParameters, "source");
                            textBox1.Refresh();
                            textBox2.Text = getAttr(backupParameters, "destination");
                            textBox2.Refresh();
                            comboBox1.SelectedIndex = Convert.ToInt32(getAttr(backupParameters, "dayofweek"));
                            comboBox1.Refresh();
                            maskedTextBox1.Text = getAttr(backupParameters, "hour");
                            maskedTextBox1.Refresh();
                        }
                    }
                }
            }

            return result;
        }

        private string getAttr(XmlNode backupParameters, string itemName)
        {
            return backupParameters.Attributes.GetNamedItem(itemName).Value.Trim();
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
            validateTextBox(textBox1, e);
        }

        private void textBox2_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            validateTextBox(textBox2, e);
        }

        private void validateTextBox(Control textBox, System.ComponentModel.CancelEventArgs e)
        {
            if (textBox.Text.Trim().Length == 0)
            {
                e.Cancel = true;
            }
            else
            {
                if (!Directory.Exists(textBox.Text.Trim()))
                {
                    MessageBox.Show("The Destination Path entered doesn't exist.", "Backup Service Interface");
                    e.Cancel = true;
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveParameters();
            NotifyChanges();
            Close();
        }

        private void SaveParameters()
        {
            XmlDocument paramsXml = new XmlDocument();
            XmlProcessingInstruction _xml_header = paramsXml.CreateProcessingInstruction("xml", "version = '1.0' encoding = 'UTF-8'");
            paramsXml.InsertBefore(_xml_header, paramsXml.ChildNodes.Item(0));
            XmlNode parameters = paramsXml.CreateNode(XmlNodeType.Element, "Parameters", "");
            XmlNode backup = paramsXml.CreateNode(XmlNodeType.Element, "Backup", "");
            XmlAttribute attribute = paramsXml.CreateAttribute("source");
            attribute.Value = textBox1.Text.Trim();
            backup.Attributes.Append(attribute);
            attribute = paramsXml.CreateAttribute("destination");
            attribute.Value = textBox2.Text.Trim();
            backup.Attributes.Append(attribute);
            attribute = paramsXml.CreateAttribute("dayofweek");
            attribute.Value = comboBox1.SelectedIndex.ToString("00");
            backup.Attributes.Append(attribute);
            attribute = paramsXml.CreateAttribute("hour");
            attribute.Value = maskedTextBox1.Text.Trim();
            backup.Attributes.Append(attribute);
            parameters.AppendChild(backup);
            paramsXml.AppendChild(parameters);
            if (!Directory.Exists(paramsFolder))
            {
                Directory.CreateDirectory(paramsFolder);
            }
            paramsXml.Save(paramsFile);
        }

        private void NotifyChanges()
        {
            ServiceController controller = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == "MonitorService");
            if (controller != null) //The service is installed
            {
                // The service is running, so it needs to be stopped and started again to reload the parameters
                if (controller.Status == ServiceControllerStatus.Running) 
                {
                    // Stops the service
                    controller.Stop();

                    // Waits until the service is really stopped
                    controller.WaitForStatus(ServiceControllerStatus.Stopped);

                    // Starts the service and reload the parameters
                    controller.Start(); 
                }
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
