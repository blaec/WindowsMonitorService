using System;
using System.IO;
using Ionic.Zip;

namespace monitorservice
{
    class BackupFiles
    {
        private string sourcePath;
        private string destinationPath;

        public bool IsBusy { get; private set; } = default;

        public BackupFiles(string sourcePath, string destinationPath)
        {
            this.sourcePath = sourcePath;
            this.destinationPath = destinationPath;
        }

        public bool DoBackup()
        {
            bool result = true;
            IsBusy = false;

            string destFileName = $"{destinationPath}\\backup_{DateTime.Now:yyyy-MM-dd hh-mm-ss}.zip";
            using (ZipFile zipFile = new ZipFile())
            {
                string[] fileList = new string[1];
                try
                {
                    fileList = Directory.GetFiles(sourcePath + "\\");
                }
                catch (Exception)
                {
                    result = false;
                }
                finally
                {
                    if (result)
                    {
                        IsBusy = true;
                        zipFile.AddDirectory(sourcePath);
                        zipFile.Save(destFileName);
                        IsBusy = false;
                    }
                }
            }
            return result;
        }
    }
}
