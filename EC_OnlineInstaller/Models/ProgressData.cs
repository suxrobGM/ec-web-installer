using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace EC_OnlineInstaller.Models
{
    public class ProgressData : BindableBase
    {
        private int downloadedFiles;
        private int maxDownloadingFiles;
        private string statusText;

        public int DownloadedFiles
        {
            get => downloadedFiles;
            set
            {
                SetProperty(ref downloadedFiles, value);
                RaisePropertyChanged("ProgressPercent");
            }
        }
        public int MaxDownloadingFiles
        {
            get => maxDownloadingFiles;
            set
            {
                SetProperty(ref maxDownloadingFiles, value);
                RaisePropertyChanged("ProgressPercent");
            }
        }
        public int ProgressPercent
        {
            get
            {
                if(MaxDownloadingFiles > 0)
                    return (DownloadedFiles * 100) / MaxDownloadingFiles;
                return 0;
            }
        }
        public string StatusText
        {
            get => statusText;
            set
            {
                SetProperty(ref statusText, value);
            }
        }
    }
}
