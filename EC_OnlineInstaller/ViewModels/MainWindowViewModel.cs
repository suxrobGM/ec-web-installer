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


        public string InstallationPath
        {
            get => installationPath;
            set
            {
                if (!value.EndsWith("\\Economic_Crisis"))
                    value += "\\Economic_Crisis";

                SetProperty(ref installationPath, value);
                if (!Directory.Exists(installationPath))
                    Directory.CreateDirectory(installationPath);
            }
        }
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
            
            downloaderClient = new DownloaderClient(dropboxToken, dropboxRootFolderName, CancellationTokenSource);
            PathSelectBtnEnabled = true;
            InstallBtnEnabled = true;
            CancelBtnEnabled = false;

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
                    await downloaderClient.DownloadFilesAsZipAsync(InstallationPath);
                    downloaderClient.CreateShortcutAfterDownloading();
                }
                catch(OperationCanceledException)
                {
                    ProgressData.StatusText = "Downloading has canceled";
                    ProgressData.DownloadedFiles = 0;
                    ProgressData.MaxDownloadingFiles = 0;         
                }
                catch(ExistedModFilesException ex)
                {
                    var result = System.Windows.MessageBox.Show(ex.Message + "\nDo you want to delete old Economic Crisis files?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if(result == MessageBoxResult.Yes)
                    {
                        if (Directory.Exists(ex.ExistedModPath))
                            Directory.Delete(ex.ExistedModPath, true);

                        await downloaderClient.DownloadFilesAsZipAsync(InstallationPath);
                        downloaderClient.CreateShortcutAfterDownloading();
                    }
                }
                catch(Exception ex)
                {
                    System.Windows.MessageBox.Show($"Network connection error \n{ex.Message}" , "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    ResetFlags();
                }
            });

            CancelCommand = new DelegateCommand(() =>
            {
                if(CancellationTokenSource != null)                
                    CancellationTokenSource.Cancel();

                ProgressData.StatusText = "Downloading has canceled";
                ResetFlags();
            });

            ExitCommand = new DelegateCommand(() =>
            {
                if (CancellationTokenSource != null)               
                    CancellationTokenSource.Cancel();
                try
                {
                    if (Directory.Exists("_cache"))
                        Directory.Delete("_cache", true);
                }
                catch (Exception) { }
                finally
                {
                    Environment.Exit(0);
                }               
            });
        }

        private void ResetFlags()
        {
            CancelBtnEnabled = false;
            PathSelectBtnEnabled = true;
            InstallBtnEnabled = true;
            ProgressData.ProgressIndeterminate = false;
            ProgressData.DownloadingSize = 0;
        }
    }
}
