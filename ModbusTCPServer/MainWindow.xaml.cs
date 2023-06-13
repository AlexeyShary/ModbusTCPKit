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

namespace ModbusTCPServer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ModbusServer Server { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Server = new ModbusServer();
            ConfigFileService.LoadDeviceList();

            DataContext = this;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ConfigFileService.SaveDeviceList();
        }

        #region Commands

        BaseCommand startServerCommand;
        public BaseCommand StartServerCommand
        {
            get
            {
                return startServerCommand ??
                    (startServerCommand = new BaseCommand(obj =>
                    {
                        Server.StartServer();
                    },
                    (obj) => Server.ModbusServerState == ModbusServer.ModbusServerStateEnum.ServerDown));
            }
        }

        BaseCommand stopServerCommand;
        public BaseCommand StopServerCommand
        {
            get
            {
                return stopServerCommand ??
                    (stopServerCommand = new BaseCommand(obj =>
                    {
                        Server.StopServer();
                    },
                    (obj) => Server.ModbusServerState == ModbusServer.ModbusServerStateEnum.ServerUp));
            }
        }

        #endregion

        #region Menu

        private void MenuItemInfo_Click(object sender, RoutedEventArgs e)
        {
            ProgramInfoWindow programInfoWindow = new ProgramInfoWindow() { Owner = this };
            programInfoWindow.Show();
        }

        private void MenuItemConfig_Click(object sender, RoutedEventArgs e)
        {
            ServerDataWindow serverDataWindow = new ServerDataWindow() { Owner = this };
            serverDataWindow.Show();
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
