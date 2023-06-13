using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ModbusTotalKit;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ModbusTCPClient
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            LoadData();

            InitializeComponent();
            DataContext = this;
        }

        ModbusServer selectedServer;
        public ModbusServer SelectedServer
        {
            get { return selectedServer; }
            set
            {
                selectedServer = value;
                OnPropertyChanged();

                if (selectedServer == null)
                {
                    SelectedServerView = null;
                    ServerPanelVisibility = Visibility.Hidden;
                }
                else
                {
                    SelectedServerView = new ModbusServerView(selectedServer);
                    ServerPanelVisibility = Visibility.Visible;
                }
            }
        }

        void LoadData()
        {
            ConfigFileService.LoadServerList();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ConfigFileService.SaveServerList();
        }

        Visibility serverPanelVisibility = Visibility.Hidden;
        public Visibility ServerPanelVisibility
        {
            get { return serverPanelVisibility; }
            private set { serverPanelVisibility = value; OnPropertyChanged(); }
        }

        ModbusServerView selectedServerView;
        public ModbusServerView SelectedServerView
        {
            get { return selectedServerView; }
            private set { selectedServerView = value; OnPropertyChanged(); }
        }

        #region Menu

        private void MenuItemInfo_Click(object sender, RoutedEventArgs e)
        {
            ProgramInfoWindow programInfoWindow = new ProgramInfoWindow() { Owner = this };
            programInfoWindow.Show();
        }

        #endregion

        #region Commands

        BaseCommand addNewServerCommand;
        public BaseCommand AddNewServerCommand
        {
            get
            {
                return addNewServerCommand ??
                  (addNewServerCommand = new BaseCommand(obj =>
                  {
                      ModbusServer newMosbusServer = new ModbusServer();
                      SelectedServer = newMosbusServer;
                  }));
            }
        }

        BaseCommand deleteServerCommand;
        public BaseCommand DeleteServerCommand
        {
            get
            {
                return deleteServerCommand ??
                    (deleteServerCommand = new BaseCommand(obj =>
                    {
                        ModbusServer selectedServer = obj as ModbusServer;
                        ModbusServer.ServerList.Remove(selectedServer);
                    },
                    (obj) => SelectedServer != null));
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
