using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Prism.Commands;
using Prism.Mvvm;
using EC_OnlineInstaller.Models;

namespace EC_OnlineInstaller.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string installationPath;        
        private CancellationTokenSource cancellationTokenSource;
        private DownloaderClient downloaderClient;
        private string dropboxToken = "JCFYioFBHBAAAAAAAAAAFq4g6p6ZhtsYZJktjnNb_JFknLnJjKEMyASiPO7kKKK5";
        private string dropboxRootFolderName = "/EC_Server_Files";
        private string title;
        private string selectedItemCB;
        private bool installBtnEnabled;
        private bool pathSelectBtnEnabled;
        private bool cancelBtnEnabled;


        public string InstallationPath { get => installationPath; set { SetProperty(ref installationPath, value); } }
        public string Title { get => title; set { SetProperty(ref title, value); } }
        public string SelectedItemCB
        {
            get => selectedItemCB;
            set
            {
                SetProperty(ref selectedItemCB, value);

                if(selectedItemCB.Contains("English"))
                    App.Language = App.Languages[0]; //en-US
                else if(selectedItemCB.Contains("Russian"))
                    App.Language = App.Languages[1]; //ru-RU
            }
        }
        public bool InstallBtnEnabled { get => installBtnEnabled; set { SetProperty(ref installBtnEnabled, value); } }
        public bool PathSelectBtnEnabled { get => pathSelectBtnEnabled; set { SetProperty(ref pathSelectBtnEnabled, value); } }
        public bool CancelBtnEnabled { get => cancelBtnEnabled; set { SetProperty(ref cancelBtnEnabled, value); } }
        public ProgressData ProgressData { get => downloaderClient.ProgressData; }
        public CancellationTokenSource CancellationTokenSource
        {
            get => cancellationTokenSource;
            set
            {
                SetProperty(ref cancellationTokenSource, value);
                if (downloaderClient != null)
                    downloaderClient.CancellationTokenSource = value;
            }
        }
        public DelegateCommand InstallCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public DelegateCommand ExitCommand { get; }
        public DelegateCommand PathSelectCommand { get; }


        public MainWindowViewModel()
        {
            InstallationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", "Hearts of Iron IV", "mod", "Economic_Crisis");
            CancellationTokenSource = new CancellationTokenSource();
            Title = $"Hearts of Iron IV: Economic Crisis Online Installer v{Assembly.GetExecutingAssembly().GetName().Version}";


            if (!Directory.Exists(InstallationPath))            
                Directory.CreateDirectory(InstallationPath);
            
            downloaderClient = new DownloaderClient(dropboxToken, dropboxRootFolderName, CancellationTokenSource);
            PathSelectBtnEnabled = true;
            InstallBtnEnabled = true;
            CancelBtnEnabled = true;

            // Commands
            PathSelectCommand = new DelegateCommand(() =>
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "Please set the correct path of mod folder";
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
                    //await downloaderClient.DownloadFilesAsync(InstallationPath);
                    //downloaderClient.CreateShortcutAfterDownloading();
                    await downloaderClient.DownloadFilesAsZipAsync(InstallationPath);
                }
                catch(OperationCanceledException)
                {
                    ProgressData.StatusText = "Downloading has canceled";                 
                }
                catch(Exception)
                {
                    System.Windows.MessageBox.Show("Network connection error", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    CancelBtnEnabled = false;
                    PathSelectBtnEnabled = true;
                    InstallBtnEnabled = true;
                }
            });

            CancelCommand = new DelegateCommand(() =>
            {
                if(CancellationTokenSource != null)                
                    CancellationTokenSource.Cancel();

                ProgressData.StatusText = "Downloading has canceled";
                CancelBtnEnabled = false;
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
