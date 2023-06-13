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
using System.Windows.Shapes;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ModbusTotalKit;

namespace ModbusTCPServer
{
    public partial class ServerDataWindow : Window, INotifyPropertyChanged
    {
        public ServerDataWindow()
        {
            InitializeComponent();
            DataContext = this;

            // DevicePanelVisibility = Visibility.Hidden;
        }

        Device selectedDevice;
        public Device SelectedDevice
        {
            get { return selectedDevice; }
            set
            {
                selectedDevice = value;
                OnPropertyChanged();

                if (selectedDevice == null)
                {
                    SelectedDeviceView = null;
                    DevicePanelVisibility = Visibility.Hidden;
                }
                else
                {
                    SelectedDeviceView = new DeviceView(selectedDevice);
                    DevicePanelVisibility = Visibility.Visible;
                }
            }
        }

        DeviceView selectedDeviceView;
        public DeviceView SelectedDeviceView
        {
            get { return selectedDeviceView; }
            protected set { selectedDeviceView = value; OnPropertyChanged(); }
        }

        Visibility devicePanelVisibility = Visibility.Hidden;
        public Visibility DevicePanelVisibility
        {
            get { return devicePanelVisibility; }
            protected set { devicePanelVisibility = value; OnPropertyChanged(); }
        }

        #region Commands

        BaseCommand addNewDeviceCommand;
        public BaseCommand AddNewDeviceCommand
        {
            get
            {
                return addNewDeviceCommand ??
                  (addNewDeviceCommand = new BaseCommand(obj =>
                  {
                      Device newDevice = new Device();
                      SelectedDevice = newDevice;
                  }));
            }
        }

        BaseCommand deleteSelectedDeviceCommand;
        public BaseCommand DeleteSelectedDeviceCommand
        {
            get
            {
                return deleteSelectedDeviceCommand ??
                    (deleteSelectedDeviceCommand = new BaseCommand(obj =>
                    {
                        Device selectedDevice = obj as Device;
                        Device.DeviceList.Remove(selectedDevice);
                    },
                    (obj) => SelectedDevice != null && Device.DeviceList.Count > 1));
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
