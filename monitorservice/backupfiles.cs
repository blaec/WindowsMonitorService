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
        public string sourcePath = "";
        public string destinationPath = "";
        public string errorMessage = "";
        public bool isBusy = default;

        public bool DoBackup()
        {
            bool result = true;
            isBusy = false;

            string destFileName = $"{destinationPath}\\backup_{DateTime.Now:yyyy-MM-dd hh-mm-ss}.zip";
            using (ZipFile zipFile = new ZipFile())
            {
                string[] fileList = new string[1];
                errorMessage = "";
                try
                {
                    fileList = Directory.GetFiles(sourcePath + "\\");
                }
                catch (Exception exception)
                {
                    result = false;
                    errorMessage = $"MonitorService: Folder file list can't be read: {exception.Message}";
                }
                finally
                {
                    if (result)
                    {
                        isBusy = true;
                        //zipFile.Encryption = EncryptionAlgorithm.WinZipAes256;
                        zipFile.AddProgress += zipFile_AddProgress;
                        zipFile.AddDirectory(sourcePath);
                        zipFile.Save(destFileName);
                        isBusy = false;
                    }
                }
            }
            return result;
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
