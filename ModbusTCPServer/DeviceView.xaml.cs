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
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ModbusTCPServer
{
    public partial class DeviceView : UserControl, INotifyPropertyChanged
    {
        Device associatedDevice;
        public Device AssociatedDevice
        {
            get { return associatedDevice; }
            private set { associatedDevice = value; OnPropertyChanged(); }
        }

        public DeviceView()
        {
            InitializeComponent();
        }

        public DeviceView(Device device)
        {
            AssociatedDevice = device;
            DataContext = this;

            InitializeComponent();
        }

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
