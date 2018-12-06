using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using EC_OnlineInstaller.Models;
using Prism.Commands;
using Prism.Mvvm;

namespace EC_OnlineInstaller.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string installationPath;        
        private CancellationTokenSource cancellationTokenSource;
        private DownloaderClient downloaderClient;
        private string dropboxToken = "JCFYioFBHBAAAAAAAAAAFq4g6p6ZhtsYZJktjnNb_JFknLnJjKEMyASiPO7kKKK5";
        private string dropboxRootFolderName = "/EC_Server_Files";
        private bool installBtnEnabled;
        private bool pathSelectBtnEnabled;
        private bool cancelBtnEnabled;

        public string InstallationPath { get => installationPath; set { SetProperty(ref installationPath, value); } }
        public bool InstallBtnEnabled { get => installBtnEnabled; set { SetProperty(ref installBtnEnabled, value); } }
        public bool PathSelectBtnEnabled { get => pathSelectBtnEnabled; set { SetProperty(ref pathSelectBtnEnabled, value); } }
        public bool CancelBtnEnabled { get => cancelBtnEnabled; set { SetProperty(ref cancelBtnEnabled, value); } }
        public ProgressData ProgressData { get => downloaderClient.ProgressData; }
        public CancellationTokenSource CancellationTokenSource { get => cancellationTokenSource; set { SetProperty(ref cancellationTokenSource, value); } }
        public DelegateCommand InstallCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand ExitCommand { get; }
        public DelegateCommand PathSelectCommand { get; }


        public MainWindowViewModel()
        {
            InstallationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod", "Economic_Crisis");
            CancellationTokenSource = new CancellationTokenSource();
            
            if (!Directory.Exists(InstallationPath))            
                Directory.CreateDirectory(InstallationPath);
            
            downloaderClient = new DownloaderClient(dropboxToken, dropboxRootFolderName, InstallationPath, CancellationTokenSource);
            PathSelectBtnEnabled = true;
            InstallBtnEnabled = true;
            CancelBtnEnabled = true;

            // Commands
            PathSelectCommand = new DelegateCommand(() =>
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    //dialog.Description = this.FindResource("m_SetModDirDesc").ToString();
                    dialog.SelectedPath = InstallationPath;
                    dialog.ShowDialog();
                    InstallationPath = dialog.SelectedPath;                   
                }
            });

            InstallCommand = new DelegateCommand(async () =>
            {
                if (CancellationTokenSource.IsCancellationRequested)            
                    CancellationTokenSource = new CancellationTokenSource();
                
                CancelBtnEnabled = true;
                PathSelectBtnEnabled = false;
                InstallBtnEnabled = false;

                try
                {
                    await downloaderClient.DownloadFilesAsync();
                    downloaderClient.CreateShortcutAfterDownloading();
                }
                catch(OperationCanceledException)
                {
                    ProgressData.StatusText = "Downloading has canceled";
                }
                catch(Exception)
                {
                    System.Windows.MessageBox.Show("Network Error", "ERROR".ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            CancelCommand = new DelegateCommand(() =>
            {
                if(CancellationTokenSource != null)                
                    CancellationTokenSource.Cancel();
                
                PathSelectBtnEnabled = true;
                InstallBtnEnabled = true;
            });

            ExitCommand = new DelegateCommand(() =>
            {
                if (CancellationTokenSource != null)               
                    CancellationTokenSource.Cancel();
                
                Environment.Exit(0);
            });
        }
    }
}
