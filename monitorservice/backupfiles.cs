using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;

namespace monitorservice
{
    class backupfiles
    {
        public string source_path = "";
        public string destination_path = "";
        public string error_message = "";
        public Boolean IsBusy = false;
        public Boolean DoBackup()
        {
            Boolean result = default(Boolean);
            this.IsBusy = false;

            string destFileName = this.destination_path + 
                "\\backup_" + System.DateTime.Now.ToString("MMM-dd-yyyy") + "-" + 
                System.DateTime.Now.ToString("hh: mm: ss").Replace(":"," - ")+".zip";
            using (ZipFile zipFile = new ZipFile())
            {
                string[] fileList = new string[1];
                result = true;
                this.error_message = "";
                try
                {
                    fileList = Directory.GetFiles(this.source_path + "\\");
                }
                catch (Exception exception)
                {
                    this.error_message = String.Format("MonitorService: Folder file list can't be read: {0}", exception.Message);
                    result = false;
                }
                finally
                {
                    if (result)
                    {
                        this.IsBusy = true;
                        zipFile.Encryption = EncryptionAlgorithm.WinZipAes256;
                        zipFile.AddProgress += (this.zipFile_AddProgress);
                        zipFile.AddDirectory(this.source_path);
                        zipFile.Save(destFileName);
                        this.IsBusy = false;
                    }
                }
            }
            return (result);
        }

        void zipFile_AddProgress(object sender, AddProgressEventArgs e)
        {
            switch (e.EventType)
            {
                case ZipProgressEventType.Adding_Started:
                    break;
                case ZipProgressEventType.Adding_AfterAddEntry:
                    break;
                case ZipProgressEventType.Adding_Completed:
                    break;
            }
        }
    }
}
