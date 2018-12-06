using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Threading;
using Dropbox.Api;
using IWshRuntimeLibrary;

namespace EC_OnlineInstaller
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string modPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod", "Economic_Crisis");
        private string tokenDropbox = "JCFYioFBHBAAAAAAAAAAFq4g6p6ZhtsYZJktjnNb_JFknLnJjKEMyASiPO7kKKK5";
        private string rootFolder = "/EC_Server_Files";      
        private DropboxClient dbx;       
        private Version remoteModVersion;
        private CancellationTokenSource cts;     

        struct ProgressData
        {
            public int downloadedFiles;
            public int maxDownloadingFiles;         
            public int progressPercent;
            public string statusText;
        }

        public MainWindow()
        {
            InitializeComponent();
            dbx = new DropboxClient(tokenDropbox);
            cts = new CancellationTokenSource();           

            if (!Directory.Exists(modPath))
            {
                Directory.CreateDirectory(modPath);
            }
            pathSelect_TB.Text = modPath;           
        }    

        private void PathSelect_Btn_Click(object sender, RoutedEventArgs e)
        {           
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = this.FindResource("m_SetModDirDesc").ToString();                               
                dialog.SelectedPath = modPath;                              
                dialog.ShowDialog();
                modPath = dialog.SelectedPath;
                pathSelect_TB.Text = modPath;
            }
        }

        private async void Install_Btn_Click(object sender, RoutedEventArgs e)
        {                    
            if (cts.IsCancellationRequested)
            {
                cts = new CancellationTokenSource();
            }
            try
            {               
                Install_Btn.IsEnabled = false;
                PathSelect_Btn.IsEnabled = false;
                progressBarStatusText.Text = this.FindResource("m_StartingDownload").ToString();

                this.remoteModVersion = await GetRemoteModVersionAsync(cts.Token);
                var progress = new Progress<ProgressData>(progressData =>
                {
                    progressBar.Dispatcher.Invoke(() => progressBar.Value = progressData.progressPercent);
                    progressBarStatusText.Dispatcher.Invoke(() =>
                    {
                        if(progressBar.Value != progressBar.Maximum)
                        {
                            progressBarStatusText.Text = $"{this.FindResource("m_DownloadingVersion")} {this.remoteModVersion} \t\t{this.FindResource("m_DownloadingFile")} {progressData.statusText}";
                        }                      
                    });

                    downloadingCountText.Dispatcher.Invoke(() =>
                    {
                        if (progressBar.Value != progressBar.Maximum)
                        {
                            downloadingCountText.Text = $"{progressData.downloadedFiles}/{progressData.maxDownloadingFiles}";
                        }
                    });
                });
                await DownloadAsync(cts.Token, progress);
            }
            catch (OperationCanceledException)
            {
                progressBarStatusText.Text = this.FindResource("m_DownloadCanceled").ToString();
                progressBar.Value = 0;
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(this, this.FindResource("m_NetworkErrorText").ToString(), this.FindResource("m_ERROR").ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Btn_Click(object sender, RoutedEventArgs e)
        {
            if(cts != null)
            {
                cts.Cancel();
                Install_Btn.IsEnabled = true;
                PathSelect_Btn.IsEnabled = true;
            }
        }

        private void Exit_Btn_Click(object sender, RoutedEventArgs e)
        {
            if (cts != null)
            {
                cts.Cancel();             
            }
            Environment.Exit(0);
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(language_CB.SelectedIndex == 0)
            {
                App.Language = App.Languages[0]; //en-US       
            }
            else
            {
                App.Language = App.Languages[1]; //ru-RU
            }
        }

        private async Task<Version> GetRemoteModVersionAsync(CancellationToken cancellationToken)
        {
            Version remoteModVersion = new Version();
            await Task.Run(async () =>
            {
                using (var response = await dbx.Files.DownloadAsync(rootFolder+"/launcher/Version.xml"))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var streamXML = await response.GetContentAsStreamAsync();
                    remoteModVersion = Version.Parse(XDocument.Load(streamXML).Element("Version").Element("Mod_Version").Value);
                }              
            }, cancellationToken);
            return remoteModVersion;
        }

        private async Task DownloadAsync(CancellationToken cancellationToken, IProgress<ProgressData> progress)
        {
            await Task.Run(async () =>
            {               
                ProgressData progressData = new ProgressData();
                List<string> downloadList = new List<string>(); //список файлов для загрузки
                
                var response = await dbx.Files.ListFolderAsync(rootFolder, true);
                
                while(true)
                {                   
                    foreach (var metadata in response.Entries)
                    {
                        if (metadata.IsFile && //Не скачать файлы гита и файлы конфига
                            !metadata.PathDisplay.Contains(".git") && 
                            !metadata.Name.Contains(".git") &&
                            !metadata.Name.Contains("Settings.xml"))
                        {
                            downloadList.Add(metadata.PathDisplay.Remove(0, rootFolder.Length));
                        }                           
                    }

                    if(!response.HasMore)
                    {
                        break;
                    }

                    response = await dbx.Files.ListFolderContinueAsync(response.Cursor);                   
                }
                
                progressData.maxDownloadingFiles = downloadList.Count(); //подсчитать файлов для загрузки              
                

                foreach (var file in downloadList)
                {
                    cancellationToken.ThrowIfCancellationRequested();                                                        

                    await DownloadFromDbx(rootFolder, file);
                       
                    progressData.downloadedFiles++;                     
                    progressData.progressPercent = GetPercentage(progressData.downloadedFiles, progressData.maxDownloadingFiles);
                    progressData.statusText = Path.GetFileName(file);

                    if (progress != null)
                    {
                        progress.Report(progressData);
                    }                    
                }

                //Копировать файл .mod из папке мода на папку My Documents\Paradox Interactive\Hearts of Iron IV\mod\
                System.IO.File.Copy(modPath + @"\launcher\Economic_Crisis.mod", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod") + @"\Economic_Crisis.mod", true);

                //Создать ярлык на рабочем столе
                CreateShortcut();

            }, cancellationToken);
        }

        private async Task DownloadFromDbx(string rootFolder, string file)
        {
            using (var response = await dbx.Files.DownloadAsync(rootFolder + file))
            {
                byte[] data = await response.GetContentAsByteArrayAsync();
                string fileNameWindows = file.Replace("/", "\\");

                //если не существует такой каталог, тогда создаем новый каталог
                if (!Directory.Exists(modPath + Path.GetDirectoryName(fileNameWindows)))
                {
                    Directory.CreateDirectory(modPath + Path.GetDirectoryName(fileNameWindows));
                }

                System.IO.File.WriteAllBytes(modPath + fileNameWindows, data);
            }
        }

        private int GetPercentage(int current, int max)
        {
            return (current * 100) / max;
        }

        // Создать ярлык лаунчера после завершение установки
        private void CreateShortcut()
        {
            object shDesktop = "Desktop";
            WshShell shell = new WshShell();
            string shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + @"\Hearts of Iron IV Economic Crisis.lnk";
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.Description = "Open the mod";
            shortcut.IconLocation = modPath + @"\launcher\Icon_EC.ico";
            shortcut.Hotkey = "Ctrl+Shift+N";
            shortcut.TargetPath = modPath + @"\launcher\EC_Launcher.exe";
            shortcut.Save();
        }

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(progressBar.Value==progressBar.Maximum)
            {
                progressBarStatusText.Text = "Finished downloading!";               
            }
        }
    }
}
