using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dropbox.Api;
using IWshRuntimeLibrary;
using Prism.Mvvm;
using System.IO.Compression;

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


        public CancellationTokenSource CancellationTokenSource
        {
            get => cancellationTokenSource;
            set
            {
                SetProperty(ref cancellationTokenSource, value);
                cancellationToken = cancellationTokenSource.Token;
            }
        }        
        public ProgressData ProgressData { get; }


        public DownloaderClient(string dropboxToken, string dropboxRootFolder, CancellationTokenSource cancellationTokenSource)
        {
            this.dropboxToken = dropboxToken;    
            this.dropboxRootFolder = dropboxRootFolder;           
            this.dropboxClient = new DropboxClient(dropboxToken);
            this.remoteModVersion = new Version();
            ProgressData = new ProgressData();            
            CancellationTokenSource = cancellationTokenSource;            
        }


        public async Task DownloadFilesAsync(string installationPath)
        {
            await Task.Run(async () =>
            {
                this.installationPath = installationPath;
                ProgressData.StatusText = "Starting download...";
                var downloadList = new List<string>(); //список файлов для загрузки
                var response = await dropboxClient.Files.ListFolderAsync(dropboxRootFolder, true);
                ProgressData.StatusText = "Downloading metadata...";

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    foreach (var metadata in response.Entries)
                    {
                        if (metadata.IsFile && //Не скачать файлы гита и файлы конфига
                            !metadata.PathDisplay.Contains(".git") &&
                            !metadata.Name.Contains(".git") &&
                            !metadata.Name.Contains("Settings.xml"))
                        {
                            downloadList.Add(metadata.PathDisplay.Remove(0, dropboxRootFolder.Length));
                            ProgressData.MaxDownloadingFiles = (ulong)downloadList.Count();
                        }
                    }

                    if (!response.HasMore)                   
                        break;
                   
                    response = await dropboxClient.Files.ListFolderContinueAsync(response.Cursor);
                }

                this.remoteModVersion = await GetRemoteModVersionAsync();              

                foreach (var file in downloadList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ProgressData.StatusText = "Latest version: " + remoteModVersion + "\tDownloading: " + Path.GetFileName(file);
                    await DownloadFromDropboxAsync(file);
                    ProgressData.DownloadedFiles++;                                                     
                }

                //Копировать файл .mod из папке мода на папку My Documents\Paradox Interactive\Hearts of Iron IV\mod\
                System.IO.File.Copy(installationPath + @"\launcher\Economic_Crisis.mod", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod") + @"\Economic_Crisis.mod", true);
                downloadedAllFiles = true;
                ProgressData.StatusText = "Finished downloading!";

            }, cancellationToken);
        }
     
        public async Task DownloadFilesAsZipAsync(string installationPath)
        {
            await Task.Run(async () =>
            {
                this.installationPath = installationPath;
                ProgressData.StatusText = "Starting download...";
                var downloadList = new List<string>(); //список папки для загрузки
                var response = await dropboxClient.Files.ListFolderAsync(dropboxRootFolder);
                ProgressData.StatusText = "Downloading metadata...";

                while (true)
                {
                    foreach (var metadata in response.Entries)
                    {
                        if(metadata.IsFolder && !metadata.PathDisplay.Contains("git") && !metadata.PathDisplay.Contains("gfx"))
                        {
                            downloadList.Add(metadata.Name);                          
                        }
                    }

                    if (!response.HasMore)
                        break;

                    response = await dropboxClient.Files.ListFolderContinueAsync(response.Cursor);
                }

                // add gfx subfolders to downloading list
                response = await dropboxClient.Files.ListFolderAsync(dropboxRootFolder + "/gfx");
                while (true)
                {
                    foreach (var metadata in response.Entries)
                    {
                        if (metadata.IsFolder && !metadata.PathDisplay.Contains("git"))
                        {
                            downloadList.Add(metadata.PathDisplay.Remove(0, metadata.PathDisplay.IndexOf("gfx")));
                        }
                    }

                    if (!response.HasMore)
                        break;

                    response = await dropboxClient.Files.ListFolderContinueAsync(response.Cursor);
                }

                ProgressData.MaxDownloadingFiles = (ulong)downloadList.Count;
                ProgressData.ProgressIndeterminate = true;
                this.remoteModVersion = await GetRemoteModVersionAsync();

                foreach (var folder in downloadList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string fileName = folder + ".zip";

                    if (folder.Contains('/'))
                        fileName = folder.Replace('/', '.') + ".zip";

                    ProgressData.StatusText = $"Latest version: {remoteModVersion} \tDownloading file: {fileName}";
                    await DownloadFolderAsZipAsync(folder, installationPath);                                 
                    ProgressData.DownloadedFiles++;                  
                }

                // После установки копировать .mod файл из папке мода на папку My Documents\Paradox Interactive\Hearts of Iron IV\mod\
                System.IO.File.Copy(installationPath + @"\launcher\Economic_Crisis.mod", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod") + @"\Economic_Crisis.mod", true);
                downloadedAllFiles = true;
                ProgressData.StatusText = "Finished downloading!";
                ProgressData.ProgressIndeterminate = false;

            }, cancellationToken);
        }

        public async Task<Version> GetRemoteModVersionAsync()
        {
            var remoteModVersion = new Version();
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

        private async Task DownloadFolderAsZipAsync(string folderName, string extractingDirectoryName)
        {
            await Task.Run(async () =>
            {
                using (var response = await dropboxClient.Files.DownloadZipAsync($"{dropboxRootFolder}/{folderName}"))
                {
                    string sourceZipName = $"_cache\\{Path.GetFileName(folderName)}.zip";                   
                    const int bufferSize = 1024 * 1024; // 1 MB
                    byte[] buffer = new byte[bufferSize];

                    using (var stream = await response.GetContentAsStreamAsync())
                    {                       
                        using (var file = new FileStream(sourceZipName, FileMode.OpenOrCreate))
                        {
                            var length = await stream.ReadAsync(buffer, 0, bufferSize);
                            
                            while (length > 0)
                            {
                                file.Write(buffer, 0, length);
                                ProgressData.DownloadingSize = (ulong)file.Length / 1024;                                                          
                                length = await stream.ReadAsync(buffer, 0, bufferSize);                               
                            }

                            var zipArchive = new ZipArchive(file, ZipArchiveMode.Read, true);
                            try
                            {
                                zipArchive.ExtractToDirectory(extractingDirectoryName);
                            }
                            catch(Exception)
                            {
                                throw new ExistedModFilesException("Existed mod files in the installation path, please change the installation path or remove old mod files");
                            }
                        }                                                                 
                    }                    
                }
            }, cancellationToken);        
        }        
    }   
}
