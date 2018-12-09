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
        private ulong downloadedFiles;
        private ulong maxDownloadingFiles;
        private ulong downloadingSize;
        private bool progressIndeterminate;
        private string statusText;

        public ulong DownloadedFiles
        {
            get => downloadedFiles;
            set
            {
                SetProperty(ref downloadedFiles, value);
                RaisePropertyChanged("ProgressPercent");
            }
        }
        public ulong MaxDownloadingFiles
        {
            get => maxDownloadingFiles;
            set
            {
                SetProperty(ref maxDownloadingFiles, value);
                RaisePropertyChanged("ProgressPercent");
            }
        }
        public ulong ProgressPercent
        {
            get
            {
                if(MaxDownloadingFiles > 0)
                    return (DownloadedFiles * 100) / MaxDownloadingFiles;
                return 0;
            }
        }
        public ulong DownloadingSize
        {
            get => downloadingSize;
            set
            {
                SetProperty(ref downloadingSize, value);
            }
        }
        public bool ProgressIndeterminate
        {
            get => progressIndeterminate;
            set
            {
                SetProperty(ref progressIndeterminate, value);
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
