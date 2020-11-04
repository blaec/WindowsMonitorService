using System;
using System.IO;
using System.Linq;
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
            bool result = false;

            using (ZipFile zipFile = new ZipFile())
            {
                if (!isSourceFolderEmpty())
                {
                    IsBusy = true;
                    zipFile.AddDirectory(sourcePath);
                    zipFile.Save($"{destinationPath}\\backup_{DateTime.Now:yyyy-MM-dd hh-mm-ss}.zip");
                    result = true;
                    IsBusy = false;
                }
            }
            return result;
        }

        private bool isSourceFolderEmpty()
        {
            return !Directory.EnumerateFileSystemEntries(sourcePath + "\\").Any();
        }
    }
}
