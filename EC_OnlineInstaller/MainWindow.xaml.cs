using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Dropbox.Api;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Threading;

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
            progressBarStatusText.Text = this.FindResource("m_StartingDownload").ToString(); 
            if(cts.IsCancellationRequested)
            {
                cts = new CancellationTokenSource();
            }

            try
            {
                this.remoteModVersion = await GetRemoteModVersionAsync(cts.Token);
                var progress = new Progress<ProgressData>(progressData =>
                {
                    progressBar.Dispatcher.Invoke(() => progressBar.Value = progressData.progressPercent);
                    progressBarStatusText.Dispatcher.Invoke(() => progressBarStatusText.Text = $"{this.FindResource("m_DownloadingVersion")} {this.remoteModVersion} \t\t{this.FindResource("m_DownloadingFile")} {progressData.statusText} \t\t{progressData.downloadedFiles}/{progressData.maxDownloadingFiles}");
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
            }
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
                List<string> filesList = new List<string>();

                var response = await dbx.Files.ListFolderAsync(rootFolder, true);
                progressData.maxDownloadingFiles = (from item in response.Entries where item.IsFile select item).Count();
                filesList = (from item in response.Entries where item.IsFile select item.PathDisplay.Remove(0, rootFolder.ToLower().Length)).ToList();

                foreach (var file in filesList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await DownloadFromDbx(rootFolder, file);

                    string fileName = Path.GetFileName(file);
                    progressData.downloadedFiles++;                     
                    progressData.progressPercent = GetPercentage(progressData.downloadedFiles, progressData.maxDownloadingFiles);
                    progressData.statusText = fileName;

                    if (progress != null)
                    {
                        progress.Report(progressData);
                    }                    
                }

                //Копировать файл .mod из папке мода в папку My Documents\Paradox Interactive\Hearts of Iron Iv\mod\
                File.Copy(modPath + @"\launcher\Economic_Crisis.mod", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod") + @"\Economic_Crisis.mod", true);

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

                File.WriteAllBytes(modPath + fileNameWindows, data);
            }
        }

        private int GetPercentage(int current, int max)
        {
            return (current * 100) / max;
        }      
    }
}
