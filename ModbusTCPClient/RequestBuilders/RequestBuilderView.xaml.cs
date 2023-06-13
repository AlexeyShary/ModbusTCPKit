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

namespace ModbusTCPClient
{
    public partial class RequestBuilderView : UserControl, INotifyPropertyChanged
    {
        RequestBuilder selectedRequestBuilder;
        public RequestBuilder SelectedRequestBuilder
        {
            get { return selectedRequestBuilder; }
            private set { selectedRequestBuilder = value; OnPropertyChanged(); }
        }

        public RequestBuilderView()
        {
            InitializeComponent();
            DataContext = this;
        }

        public RequestBuilderView(RequestBuilder requestBuilder)
        {
            InitializeComponent();

            SelectedRequestBuilder = requestBuilder;

            DataContext = this;
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
