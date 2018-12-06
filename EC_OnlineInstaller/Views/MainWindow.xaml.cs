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
                           
        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(language_CB.SelectedIndex == 0)           
                App.Language = App.Languages[0]; //en-US                
            else         
                App.Language = App.Languages[1]; //ru-RU      
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start("https://www.facebook.com/suxrobgm");
        }
    }
}
