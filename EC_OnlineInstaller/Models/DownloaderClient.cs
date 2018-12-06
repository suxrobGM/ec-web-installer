using Dropbox.Api;
using IWshRuntimeLibrary;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EC_OnlineInstaller.Models
{
    public class DownloaderClient : BindableBase
    {
        private string installationPath;
        private string dropboxToken;
        private string dropboxRootFolder;
        private bool downloadedAllFiles;
        private DropboxClient dropboxClient;
        private Version remoteModVersion;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;


        public CancellationTokenSource CancellationTokenSource { get => cancellationTokenSource; set { SetProperty(ref cancellationTokenSource, value); } }       
        public ProgressData ProgressData { get; }


        public DownloaderClient(string dropboxToken, string dropboxRootFolder, string installationPath, CancellationTokenSource cancellationTokenSource)
        {
            this.dropboxToken = dropboxToken;
            this.installationPath = installationPath;
            this.dropboxRootFolder = dropboxRootFolder;           
            this.dropboxClient = new DropboxClient(dropboxToken);
            this.remoteModVersion = new Version();
            ProgressData = new ProgressData();
            CancellationTokenSource = cancellationTokenSource;
            cancellationToken = cancellationTokenSource.Token;
        }


        public async Task DownloadFilesAsync()
        {
            await Task.Run(async () =>
            {
                ProgressData.StatusText = "Starting download...";
                var downloadList = new List<string>(); //список файлов для загрузки
                var response = await dropboxClient.Files.ListFolderAsync(dropboxRootFolder, true);
                ProgressData.StatusText = "Downloading metadata...";

                while (true)
                {
                    foreach (var metadata in response.Entries)
                    {
                        if (metadata.IsFile && //Не скачать файлы гита и файлы конфига
                            !metadata.PathDisplay.Contains(".git") &&
                            !metadata.Name.Contains(".git") &&
                            !metadata.Name.Contains("Settings.xml"))
                        {
                            downloadList.Add(metadata.PathDisplay.Remove(0, dropboxRootFolder.Length));
                            ProgressData.MaxDownloadingFiles = downloadList.Count();
                        }
                    }

                    if (!response.HasMore)                   
                        break;
                    
                    response = await dropboxClient.Files.ListFolderContinueAsync(response.Cursor);
                }                    


                foreach (var file in downloadList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await DownloadFromDropboxAsync(file);
                    ProgressData.DownloadedFiles++;                   
                    ProgressData.StatusText = "Downloading: " + Path.GetFileName(file);                   
                }

                //Копировать файл .mod из папке мода на папку My Documents\Paradox Interactive\Hearts of Iron IV\mod\
                System.IO.File.Copy(installationPath + @"\launcher\Economic_Crisis.mod", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod") + @"\Economic_Crisis.mod", true);
                downloadedAllFiles = true;
                ProgressData.StatusText = "Finished downloading!";

            }, cancellationToken);
        }

        public async Task<Version> GetRemoteModVersionAsync()
        {             
            await Task.Run(async () =>
            {
                using (var response = await dropboxClient.Files.DownloadAsync(dropboxRootFolder + "/launcher/Version.xml"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var streamXML = await response.GetContentAsStreamAsync();
                    remoteModVersion = Version.Parse(XDocument.Load(streamXML).Element("Version").Element("Mod_Version").Value);
                }
            }, cancellationToken);

            return remoteModVersion;
        }

        public void CreateShortcutAfterDownloading()
        {
            if (!downloadedAllFiles)
                return;

            object shDesktop = "Desktop";
            WshShell shell = new WshShell();
            string shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + @"\Hearts of Iron IV Economic Crisis.lnk";
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "Open the mod";
            shortcut.IconLocation = installationPath + @"\launcher\icon_EC.ico";
            shortcut.Hotkey = "Ctrl+Shift+N";
            shortcut.TargetPath = installationPath + @"\launcher\EC_Launcher.exe";
            shortcut.Save();
        }

        private async Task DownloadFromDropboxAsync(string file)
        {
            await Task.Run(async () =>
            {
                using (var response = await dropboxClient.Files.DownloadAsync(dropboxRootFolder + file))
                {
                    byte[] data = await response.GetContentAsByteArrayAsync();
                    string fileNameWindows = file.Replace("/", "\\");

                    //если не существует такой каталог, тогда создаем новый каталог
                    if (!Directory.Exists(installationPath + Path.GetDirectoryName(fileNameWindows)))
                    {
                        Directory.CreateDirectory(installationPath + Path.GetDirectoryName(fileNameWindows));
                    }

                    System.IO.File.WriteAllBytes(installationPath + fileNameWindows, data);
                }
            }, cancellationToken);         
        }
    }
}
