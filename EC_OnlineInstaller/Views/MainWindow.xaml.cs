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
using EC_OnlineInstaller.Models;
using System.Diagnostics;

namespace EC_OnlineInstaller
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {               
        public MainWindow()
        {
            InitializeComponent();         
        }    
                                
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start("https://www.facebook.com/suxrobgm");
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            try
            {
                if (!Directory.Exists("_cache"))
                    Directory.CreateDirectory("_cache");
            }
            catch (Exception) { }           
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (Directory.Exists("_cache"))
                    Directory.Delete("_cache", true);
            }
            catch (Exception) { }             
        }  
    }
}
