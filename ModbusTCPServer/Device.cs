using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows.Data;

namespace ModbusTCPServer
{
    [DataContract(IsReference = true)]
    public class Device : INotifyPropertyChanged
    {
        static ObservableCollection<Device> deviceList = new ObservableCollection<Device>();
        public static ObservableCollection<Device> DeviceList
        {
            get { return deviceList; }
        }

        public Device()
        {
            DeviceName = "New device";
            DeviceAddress = 1;

            Initialize();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context)
        {
            this.Initialize();
        }

        void Initialize()
        {
            DeviceList.Add(this);
        }

        string deviceName;
        [DataMember]
        public string DeviceName
        {
            get { return deviceName; }
            set { deviceName = value; OnPropertyChanged(); }
        }

        byte deviceAddress;
        [DataMember]
        public byte DeviceAddress
        {
            get { return deviceAddress; }
            set { deviceAddress = value; OnPropertyChanged(); }
        }

        bool createDataOnRequest;
        [DataMember]
        public bool CreateDataOnRequest
        {
            get { return createDataOnRequest; }
            set { createDataOnRequest = value; OnPropertyChanged(); }
        }

        ObservableCollection<ModbusBit> coilsList;
        [DataMember]
        public ObservableCollection<ModbusBit> CoilsList
        {
            get
            {
                if (coilsList == null)
                    coilsList = new ObservableCollection<ModbusBit>();

                return coilsList;
            }

            set { coilsList = value; OnPropertyChanged(); }
        }

        CollectionViewSource coilsListViewSource;
        public CollectionViewSource CoilsListViewSource
        {
            get
            {
                if (coilsListViewSource == null)
                {
                    coilsListViewSource = new CollectionViewSource();
                    coilsListViewSource.Source = CoilsList;

                    coilsListViewSource.SortDescriptions.Add(new SortDescription("Address", ListSortDirection.Ascending));
                    coilsListViewSource.View.Refresh();
                }

                return coilsListViewSource;
            }
        }

        ObservableCollection<ModbusBit> discreteInputsList;
        [DataMember]
        public ObservableCollection<ModbusBit> DiscreteInputsList
        {
            get
            {
                if (discreteInputsList == null)
                    discreteInputsList = new ObservableCollection<ModbusBit>();

                return discreteInputsList;
            }

            set { discreteInputsList = value; OnPropertyChanged(); }
        }

        CollectionViewSource discreteInputsListViewSource;
        public CollectionViewSource DiscreteInputsListViewSource
        {
            get
            {
                if (discreteInputsListViewSource == null)
                {
                    discreteInputsListViewSource = new CollectionViewSource();
                    discreteInputsListViewSource.Source = DiscreteInputsList;

                    discreteInputsListViewSource.SortDescriptions.Add(new SortDescription("Address", ListSortDirection.Ascending));
                    discreteInputsListViewSource.View.Refresh();
                }

                return discreteInputsListViewSource;
            }
        }

        ObservableCollection<ModbusRegister> inputRegistersList;
        [DataMember]
        public ObservableCollection<ModbusRegister> InputRegistersList
        {
            get
            {
                if (inputRegistersList == null)
                    inputRegistersList = new ObservableCollection<ModbusRegister>();

                return inputRegistersList;
            }

            set { inputRegistersList = value; OnPropertyChanged(); }
        }

        CollectionViewSource inputRegistersListViewSource;
        public CollectionViewSource InputRegistersListViewSource
        {
            get
            {
                if (inputRegistersListViewSource == null)
                {
                    inputRegistersListViewSource = new CollectionViewSource();
                    inputRegistersListViewSource.Source = InputRegistersList;

                    inputRegistersListViewSource.SortDescriptions.Add(new SortDescription("Address", ListSortDirection.Ascending));
                    inputRegistersListViewSource.View.Refresh();
                }

                return inputRegistersListViewSource;
            }
        }

        ObservableCollection<ModbusRegister> holdingRegistersList;
        [DataMember]
        public ObservableCollection<ModbusRegister> HoldingRegistersList
        {
            get
            {
                if (holdingRegistersList == null)
                    holdingRegistersList = new ObservableCollection<ModbusRegister>();

                return holdingRegistersList;
            }

            set { holdingRegistersList = value; OnPropertyChanged(); }
        }

        CollectionViewSource holdingRegistersListViewSource;
        public CollectionViewSource HoldingRegistersListViewSource
        {
            get
            {
                if (holdingRegistersListViewSource == null)
                {
                    holdingRegistersListViewSource = new CollectionViewSource();
                    holdingRegistersListViewSource.Source = HoldingRegistersList;

                    holdingRegistersListViewSource.SortDescriptions.Add(new SortDescription("Address", ListSortDirection.Ascending));
                    holdingRegistersListViewSource.View.Refresh();
                }

                return holdingRegistersListViewSource;
            }
        }

        [DataContract(IsReference = true)]
        public class ModbusBit : INotifyPropertyChanged
        {
            public ModbusBit()
            {

            }

            public ModbusBit(ushort address)
            {
                this.address = address;
            }

            ushort address;
            [DataMember]
            public ushort Address
            {
                get { return address; }
                set { address = value; OnPropertyChanged(); }
            }

            bool bitValue;
            public bool BitValue
            {
                get { return bitValue; }
                set { bitValue = value; OnPropertyChanged(); }
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

        [DataContract(IsReference = true)]
        public class ModbusRegister : INotifyPropertyChanged
        {
            public ModbusRegister()
            {

            }

            public ModbusRegister(ushort address)
            {
                this.address = address;
            }

            ushort address;
            [DataMember]
            public ushort Address
            {
                get { return address; }
                set { address = value; OnPropertyChanged(); }
            }

            ushort registerValue;
            public ushort RegisterValue
            {
                get { return registerValue; }
                set { registerValue = value; OnPropertyChanged(); }
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
