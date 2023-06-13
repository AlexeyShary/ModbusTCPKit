using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ModbusTotalKit;

namespace ModbusTCPServer
{
    public class ModbusServer : INotifyPropertyChanged
    {
        SynchronizationContext uiContext;

        public ModbusServer()
        {
            uiContext = SynchronizationContext.Current;

            ServerPort = 502;
            ModbusServerState = ModbusServerStateEnum.ServerDown;

            listenThread = new Thread(new ThreadStart(Listen));
            listenThread.IsBackground = true;
        }

        int serverPort;
        public int ServerPort
        {
            get { return serverPort; }
            set { serverPort = value; OnPropertyChanged(); }
        }

        ObservableCollection<ModbusClient> clientList = new ObservableCollection<ModbusClient>();
        public ObservableCollection<ModbusClient> ClientList
        {
            get { return clientList; }
            private set { clientList = value; }
        }

        public void AddClientToList(ModbusClient client)
        {
            uiContext.Send(x => ClientList.Add(client), null);
        }

        public void RemoveClientFromList(ModbusClient client)
        {
            uiContext.Send(x => ClientList.Remove(client), null);
        }

        Logger serverLogger = new Logger();
        public Logger ServerLogger
        {
            get { return serverLogger; }
        }

        TcpListener tcpListener;
        Thread listenThread;

        public void Listen()
        {
            serverLogger.AddMessage("TCP listener thread started.");

            IPAddress ip = IPAddress.Parse("127.0.0.1");

            tcpListener = new TcpListener(IPAddress.Any, ServerPort);
            tcpListener.Start();

            serverLogger.AddMessage("TCP listener started.");

            ModbusServerState = ModbusServerStateEnum.ServerUp;

            while (true)
            {
                if (cancelListen)
                    break;

                try
                {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();

                    serverLogger.AddMessage("TCP client accepted. Client IP " + ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());

                    ModbusClient newClient = new ModbusClient(tcpClient, this);

                    serverLogger.AddMessage("New client ID " + newClient.ClientID);

                    newClient.StartHandleClient();
                }
                catch (Exception e) { Console.WriteLine(e.Message); }
            }

            serverLogger.AddMessage("TCP listener thread finished.");

            ModbusServerState = ModbusServerStateEnum.ServerDown;
        }

        public void ResponceOnMessage(ModbusClient modbusClient, byte[] incomingMessage)
        {
            byte functionCode = 0;

            try
            {
                functionCode = incomingMessage[7];
            }
            catch
            {
                serverLogger.AddMessage(String.Format("[{0} - {1}] Error during function code detection",
                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                    modbusClient.ClientID,
                    functionCode
                    ));
                return;
            }

            byte deviceAddress = incomingMessage[6];

            Device deviceToResponce = Device.DeviceList.FirstOrDefault(d => d.DeviceAddress == deviceAddress);

            if (deviceToResponce == null)
            {
                serverLogger.AddMessage(String.Format("[{0} - {1}] The device with the specified number ({2}) is not in the configured server data.",
                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                    modbusClient.ClientID,
                    deviceAddress
                    ));

                // No device exception code?
                return;
            }

            byte[] responce = null;

            switch(functionCode)
            {
                case 1:
                    responce = FC01Responce(modbusClient, incomingMessage, deviceToResponce);
                    break;
                case 2:
                    responce = FC02Responce(modbusClient, incomingMessage, deviceToResponce);
                    break;
                case 3:
                    responce = FC03Responce(modbusClient, incomingMessage, deviceToResponce);
                    break;
                case 4:
                    responce = FC04Responce(modbusClient, incomingMessage, deviceToResponce);
                    break;
                case 5:
                    responce = FC05Responce(modbusClient, incomingMessage, deviceToResponce);
                    break;
                case 6:
                    responce = FC06Responce(modbusClient, incomingMessage, deviceToResponce);
                    break;
                case 15:
                    responce = FC15Responce(modbusClient, incomingMessage, deviceToResponce);
                    break;
                case 16:
                    responce = FC16Responce(modbusClient, incomingMessage, deviceToResponce);
                    break;
                case 254:
                    responce = new byte[] { 254, 254, 254 }; 
                    break;
                default:
                    serverLogger.AddMessage(String.Format("[{0} - {1}] Illegal function code {2}",
                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                    modbusClient.ClientID,
                    functionCode
                    ));

                    if (functionCode > 128)
                        serverLogger.AddMessage(String.Format("[{0} - {1}] Request function code > 128 - can't responce with an error",
                        ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                        modbusClient.ClientID,
                        functionCode
                        ));
                    else
                        // Illegal function code
                        responce = new byte[] { incomingMessage[0], incomingMessage[1], incomingMessage[2], incomingMessage[3], 00, 03, incomingMessage[6], (byte)(128 + functionCode), 01 };

                    break;
            }

            if (responce == null)
                return;

            modbusClient.ClientStream.Write(responce, 0, responce.Length);

            serverLogger.AddMessage(String.Format("[{0} - {1}] Tx: {2} [ {3} bytes ]",
                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                    modbusClient.ClientID,
                    responce.ToHexString(),
                    responce.Count()
                    ));
        }

        byte[] FC01Responce(ModbusClient modbusClient, byte[] modbusRequest, Device modbusDevice)
        {
            if(modbusRequest.Length != 12)
            {
                serverLogger.AddMessage(String.Format("[{0} - {1}] Invalid request length - {2} bytes. 12 bytes expected.",
                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                    modbusClient.ClientID,
                    modbusRequest.Length
                    ));
                return null;

                // Illegal what
            }

            ushort startAddress = BitConverter.ToUInt16(new byte[] { modbusRequest[9], modbusRequest[8] }, 0);
            ushort dataCount = BitConverter.ToUInt16(new byte[] { modbusRequest[11], modbusRequest[10] }, 0);

            int valueBytesCount = 1 + (dataCount / 8);
            if (dataCount % 8 == 0)
                valueBytesCount--;

            byte messageSize = (byte)(9 + valueBytesCount);

            byte[] responce = new byte[messageSize];

            responce[0] = modbusRequest[0];     // Transaction identifier
            responce[1] = modbusRequest[1];     // Transaction identifier
            responce[2] = 0;     // Protocol identifier = 0
            responce[3] = 0;     // Protocol identifier = 0
            responce[4] = 0;     // Message size in bytes after [5] 
            responce[5] = (byte)(messageSize - 6);     // Message size in bytes after [5] 
            responce[6] = modbusDevice.DeviceAddress;     // Slave address
            responce[7] = 1;     // Function code
            responce[8] = (byte)valueBytesCount;     // Count of bytes more

            for (int i = 0; i < valueBytesCount; i++)
            {
                int bitArrayLength = 0;

                if (i < valueBytesCount - 1)
                    bitArrayLength = 8;
                else
                    bitArrayLength = dataCount - i * 8;

                bool[] values = new bool[8];

                for (int j = 0; j < bitArrayLength; j++)
                {
                    ushort dataAddress =  (ushort)(startAddress + j + i * 8);

                    Device.ModbusBit modbusBit = modbusDevice.CoilsList.FirstOrDefault(m => m.Address == dataAddress);

                    if (modbusBit == null)
                    {
                        if (modbusDevice.CreateDataOnRequest)
                        {
                            modbusBit = new Device.ModbusBit(dataAddress);
                            uiContext.Send(x => modbusDevice.CoilsList.Add(modbusBit), null);
                        }
                        else
                        {
                            serverLogger.AddMessage(String.Format("[{0} - {1}] Illegal data addres {2} on {3}",
                                        ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                                        modbusClient.ClientID,
                                        dataAddress,
                                        modbusDevice.DeviceName
                                        ));

                            return new byte[] { modbusRequest[0], modbusRequest[1], modbusRequest[2], modbusRequest[3], 00, 03, modbusRequest[6], (byte)(128 + 01), 02 };
                        }
                    }

                    values[j] = modbusBit.BitValue;
                }

                responce[9 + i] = values.Reverce().ToByte();
            }

            return responce;
        }

        byte[] FC02Responce(ModbusClient modbusClient, byte[] modbusRequest, Device modbusDevice)
        {
            if (modbusRequest.Length != 12)
            {
                serverLogger.AddMessage(String.Format("[{0} - {1}] Invalid request length - {2} bytes. 12 bytes expected.",
                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                    modbusClient.ClientID,
                    modbusRequest.Length
                    ));
                return null;

                // Illegal what
            }

            ushort startAddress = BitConverter.ToUInt16(new byte[] { modbusRequest[9], modbusRequest[8] }, 0);
            ushort dataCount = BitConverter.ToUInt16(new byte[] { modbusRequest[11], modbusRequest[10] }, 0);

            int valueBytesCount = 1 + (dataCount / 8);
            if (dataCount % 8 == 0)
                valueBytesCount--;

            byte messageSize = (byte)(9 + valueBytesCount);

            byte[] responce = new byte[messageSize];

            responce[0] = modbusRequest[0];     // Transaction identifier
            responce[1] = modbusRequest[1];     // Transaction identifier
            responce[2] = 0;     // Protocol identifier = 0
            responce[3] = 0;     // Protocol identifier = 0
            responce[4] = 0;     // Message size in bytes after [5] 
            responce[5] = (byte)(messageSize - 6);     // Message size in bytes after [5] 
            responce[6] = modbusDevice.DeviceAddress;     // Slave address
            responce[7] = 2;     // Function code
            responce[8] = (byte)valueBytesCount;     // Count of bytes more

            for (int i = 0; i < valueBytesCount; i++)
            {
                int bitArrayLength = 0;

                if (i < valueBytesCount - 1)
                    bitArrayLength = 8;
                else
                    bitArrayLength = dataCount - i * 8;

                bool[] values = new bool[8];

                for (int j = 0; j < bitArrayLength; j++)
                {
                    ushort dataAddress = (ushort)(startAddress + j + i * 8);

                    Device.ModbusBit modbusBit = modbusDevice.DiscreteInputsList.FirstOrDefault(m => m.Address == dataAddress);

                    if (modbusBit == null)
                    {
                        if (modbusDevice.CreateDataOnRequest)
                        {
                            modbusBit = new Device.ModbusBit(dataAddress);
                            uiContext.Send(x => modbusDevice.DiscreteInputsList.Add(modbusBit), null);
                        }
                        else
                        {
                            serverLogger.AddMessage(String.Format("[{0} - {1}] Illegal data addres {2} on {3}",
                                        ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                                        modbusClient.ClientID,
                                        dataAddress,
                                        modbusDevice.DeviceName
                                        ));

                            return new byte[] { modbusRequest[0], modbusRequest[1], modbusRequest[2], modbusRequest[3], 00, 03, modbusRequest[6], (byte)(128 + 02), 02 };
                        }
                    }

                    values[j] = modbusBit.BitValue;
                }

                responce[9 + i] = values.Reverce().ToByte();
            }

            return responce;
        }

        byte[] FC03Responce(ModbusClient modbusClient, byte[] modbusRequest, Device modbusDevice)
        {
            if (modbusRequest.Length != 12)
            {
                serverLogger.AddMessage(String.Format("[{0} - {1}] Invalid request length - {2} bytes. 12 bytes expected.",
                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                    modbusClient.ClientID,
                    modbusRequest.Length
                    ));
                return null;

                // Illegal what
            }

            ushort startAddress = BitConverter.ToUInt16(new byte[] { modbusRequest[9], modbusRequest[8] }, 0);
            ushort dataCount = BitConverter.ToUInt16(new byte[] { modbusRequest[11], modbusRequest[10] }, 0);

            int valueBytesCount = dataCount * 2;

            byte messageSize = (byte)(9 + valueBytesCount);

            byte[] responce = new byte[messageSize];

            responce[0] = modbusRequest[0];     // Transaction identifier
            responce[1] = modbusRequest[1];     // Transaction identifier
            responce[2] = 0;     // Protocol identifier = 0
            responce[3] = 0;     // Protocol identifier = 0
            responce[4] = 0;     // Message size in bytes after [5] 
            responce[5] = (byte)(messageSize - 6);     // Message size in bytes after [5] 
            responce[6] = modbusDevice.DeviceAddress;     // Slave address
            responce[7] = 3;     // Function code
            responce[8] = (byte)valueBytesCount;     // Count of bytes more

            for(int i = 0; i < dataCount; i++)
            {
                byte[] valBytes = new byte[2];

                ushort dataAddress = (ushort)(startAddress + i);

                Device.ModbusRegister modbusRegister = modbusDevice.HoldingRegistersList.FirstOrDefault(m => m.Address == dataAddress);

                if (modbusRegister == null)
                {
                    if (modbusDevice.CreateDataOnRequest)
                    {
                        modbusRegister = new Device.ModbusRegister(dataAddress);
                        uiContext.Send(x => modbusDevice.HoldingRegistersList.Add(modbusRegister), null);
                    }
                    else
                    {
                        serverLogger.AddMessage(String.Format("[{0} - {1}] Illegal data addres {2} on {3}",
                                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                                    modbusClient.ClientID,
                                    dataAddress,
                                    modbusDevice.DeviceName
                                    ));

                        return new byte[] { modbusRequest[0], modbusRequest[1], modbusRequest[2], modbusRequest[3], 00, 03, modbusRequest[6], (byte)(128 + 03), 02 };
                    }
                }

                valBytes = BitConverter.GetBytes(modbusRegister.RegisterValue).SwapBytes();

                responce[9 + i * 2] = valBytes[0];
                responce[9 + 1 + i * 2] = valBytes[1];
            }

            return responce;
        }

        byte[] FC04Responce(ModbusClient modbusClient, byte[] modbusRequest, Device modbusDevice)
        {
            if (modbusRequest.Length != 12)
            {
                serverLogger.AddMessage(String.Format("[{0} - {1}] Invalid request length - {2} bytes. 12 bytes expected.",
                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                    modbusClient.ClientID,
                    modbusRequest.Length
                    ));
                return null;

                // Illegal what
            }

            ushort startAddress = BitConverter.ToUInt16(new byte[] { modbusRequest[9], modbusRequest[8] }, 0);
            ushort dataCount = BitConverter.ToUInt16(new byte[] { modbusRequest[11], modbusRequest[10] }, 0);

            int valueBytesCount = dataCount * 2;

            byte messageSize = (byte)(9 + valueBytesCount);

            byte[] responce = new byte[messageSize];

            responce[0] = modbusRequest[0];     // Transaction identifier
            responce[1] = modbusRequest[1];     // Transaction identifier
            responce[2] = 0;     // Protocol identifier = 0
            responce[3] = 0;     // Protocol identifier = 0
            responce[4] = 0;     // Message size in bytes after [5] 
            responce[5] = (byte)(messageSize - 6);     // Message size in bytes after [5] 
            responce[6] = modbusDevice.DeviceAddress;     // Slave address
            responce[7] = 4;     // Function code
            responce[8] = (byte)valueBytesCount;     // Count of bytes more

            for (int i = 0; i < dataCount; i++)
            {
                byte[] valBytes = new byte[2];

                ushort dataAddress = (ushort)(startAddress + i);

                Device.ModbusRegister modbusRegister = modbusDevice.InputRegistersList.FirstOrDefault(m => m.Address == dataAddress);

                if (modbusRegister == null)
                {
                    if (modbusDevice.CreateDataOnRequest)
                    {
                        modbusRegister = new Device.ModbusRegister(dataAddress);
                        uiContext.Send(x => modbusDevice.InputRegistersList.Add(modbusRegister), null);
                    }
                    else
                    {
                        serverLogger.AddMessage(String.Format("[{0} - {1}] Illegal data addres {2} on {3}",
                                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                                    modbusClient.ClientID,
                                    dataAddress,
                                    modbusDevice.DeviceName
                                    ));

                        return new byte[] { modbusRequest[0], modbusRequest[1], modbusRequest[2], modbusRequest[3], 00, 03, modbusRequest[6], (byte)(128 + 04), 02 };
                    }
                }

                valBytes = BitConverter.GetBytes(modbusRegister.RegisterValue).SwapBytes();

                responce[9 + i * 2] = valBytes[0];
                responce[9 + 1 + i * 2] = valBytes[1];
            }

            return responce;
        }

        byte[] FC05Responce(ModbusClient modbusClient, byte[] modbusRequest, Device modbusDevice)
        {
            if (modbusRequest.Length < 12)
            {
                serverLogger.AddMessage(String.Format("[{0} - {1}] Invalid request length - {2} bytes. 14+ bytes expected.",
                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                    modbusClient.ClientID,
                    modbusRequest.Length
                    ));
                return null;
            }

            ushort startAddress = BitConverter.ToUInt16(new byte[] { modbusRequest[9], modbusRequest[8] }, 0);

            byte[] responce = new byte[12];

            responce[0] = modbusRequest[0];     // Transaction identifier
            responce[1] = modbusRequest[1];     // Transaction identifier
            responce[2] = 0;     // Protocol identifier = 0
            responce[3] = 0;     // Protocol identifier = 0
            responce[4] = 0;     // Message size in bytes after [5] 
            responce[5] = 6;     // Message size in bytes after [5] 
            responce[6] = modbusDevice.DeviceAddress;     // Slave address
            responce[7] = 5;     // Function code

            responce[8] = modbusRequest[8];
            responce[9] = modbusRequest[9];
            responce[10] = modbusRequest[10];
            responce[11] = modbusRequest[11];

            // Check for execute possibility

            Device.ModbusBit modbusBit = modbusDevice.CoilsList.FirstOrDefault(m => m.Address == startAddress);

            if (modbusBit == null)
            {
                if (modbusDevice.CreateDataOnRequest)
                {
                    modbusBit = new Device.ModbusBit(startAddress);
                    uiContext.Send(x => modbusDevice.CoilsList.Add(modbusBit), null);
                }
                else
                {
                    serverLogger.AddMessage(String.Format("[{0} - {1}] Illegal data addres {2} on {3}",
                                ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                                modbusClient.ClientID,
                                startAddress,
                                modbusDevice.DeviceName
                                ));

                    return new byte[] { modbusRequest[0], modbusRequest[1], modbusRequest[2], modbusRequest[3], 00, 03, modbusRequest[6], (byte)(128 + 05), 02 };
                }
            }

            // Command execute

            if (modbusRequest[10] == 255)
                modbusBit.BitValue = true;
            else
                modbusBit.BitValue = false;

            return responce;
        }

        byte[] FC06Responce(ModbusClient modbusClient, byte[] modbusRequest, Device modbusDevice)
        {
            if (modbusRequest.Length != 12)
            {
                serverLogger.AddMessage(String.Format("[{0} - {1}] Invalid request length - {2} bytes. 12 bytes expected.",
                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                    modbusClient.ClientID,
                    modbusRequest.Length
                    ));
                return null;

                // Illegal what
            }

            ushort startAddress = BitConverter.ToUInt16(new byte[] { modbusRequest[9], modbusRequest[8] }, 0);
            // ushort dataCount = 1;

            byte[] responce = new byte[12];

            responce[0] = modbusRequest[0];     // Transaction identifier
            responce[1] = modbusRequest[1];     // Transaction identifier
            responce[2] = 0;     // Protocol identifier = 0
            responce[3] = 0;     // Protocol identifier = 0
            responce[4] = 0;     // Message size in bytes after [5] 
            responce[5] = 6;     // Message size in bytes after [5] 
            responce[6] = modbusDevice.DeviceAddress;     // Slave address
            responce[7] = 6;     // Function code

            responce[8] = modbusRequest[8];
            responce[9] = modbusRequest[9];
            responce[10] = modbusRequest[10];
            responce[11] = modbusRequest[11];

            // Check for execute possibility

            Device.ModbusRegister modbusRegister = modbusDevice.HoldingRegistersList.FirstOrDefault(m => m.Address == startAddress);

            if (modbusRegister == null)
            {
                if (modbusDevice.CreateDataOnRequest)
                {
                    modbusRegister = new Device.ModbusRegister(startAddress);
                    uiContext.Send(x => modbusDevice.HoldingRegistersList.Add(modbusRegister), null);
                }
                else
                {
                    serverLogger.AddMessage(String.Format("[{0} - {1}] Illegal data addres {2} on {3}",
                                ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                                modbusClient.ClientID,
                                startAddress,
                                modbusDevice.DeviceName
                                ));

                    return new byte[] { modbusRequest[0], modbusRequest[1], modbusRequest[2], modbusRequest[3], 00, 03, modbusRequest[6], (byte)(128 + 06), 02 };
                }
            }

            // Command execute

            byte[] valBytes = new byte[2];

            valBytes[0] = modbusRequest[11];
            valBytes[1] = modbusRequest[10];

            modbusRegister.RegisterValue = BitConverter.ToUInt16(valBytes, 0);

            return responce;
        }

        byte[] FC15Responce(ModbusClient modbusClient, byte[] modbusRequest, Device modbusDevice)
        {
            if (modbusRequest.Length < 14)
            {
                serverLogger.AddMessage(String.Format("[{0} - {1}] Invalid request length - {2} bytes. 14+ bytes expected.",
                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                    modbusClient.ClientID,
                    modbusRequest.Length
                    ));
                return null;
            }

            ushort startAddress = BitConverter.ToUInt16(new byte[] { modbusRequest[9], modbusRequest[8] }, 0);
            ushort dataCount = BitConverter.ToUInt16(new byte[] { modbusRequest[11], modbusRequest[10] }, 0);

            int valueBytesCount = modbusRequest.Length - 13;

            // Check for execute possibility
            for (int i = 0; i < valueBytesCount; i++)
            {
                int bitArrayLength = 0;

                if (i < valueBytesCount - 1)
                    bitArrayLength = 8;
                else
                    bitArrayLength = dataCount - i * 8;

                for (int j = 0; j < bitArrayLength; j++)
                {
                    ushort dataAddress = (ushort)(startAddress + j + i * 8);

                    Device.ModbusBit modbusBit = modbusDevice.CoilsList.FirstOrDefault(m => m.Address == dataAddress);

                    if (modbusBit == null)
                    {
                        if (modbusDevice.CreateDataOnRequest)
                        {
                            modbusBit = new Device.ModbusBit(dataAddress);
                            uiContext.Send(x => modbusDevice.CoilsList.Add(modbusBit), null);
                        }
                        else
                        {
                            serverLogger.AddMessage(String.Format("[{0} - {1}] Illegal data addres {2} on {3}",
                                        ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                                        modbusClient.ClientID,
                                        dataAddress,
                                        modbusDevice.DeviceName
                                        ));

                            return new byte[] { modbusRequest[0], modbusRequest[1], modbusRequest[2], modbusRequest[3], 00, 03, modbusRequest[6], (byte)(128 + 01), 02 };
                        }
                    }
                }
            }

            // Command execute
            for (int i = 0; i < valueBytesCount; i++)
            {
                int bitArrayLength = 0;

                if (i < valueBytesCount - 1)
                    bitArrayLength = 8;
                else
                    bitArrayLength = dataCount - i * 8;

                for (int j = 0; j < bitArrayLength; j++)
                {
                    ushort dataAddress = (ushort)(startAddress + j + i * 8);

                    Device.ModbusBit modbusBit = modbusDevice.CoilsList.FirstOrDefault(m => m.Address == dataAddress);

                    modbusBit.BitValue = modbusRequest[13 + i].GetBit(j);
                }
            }

            byte[] responce = new byte[12];

            responce[0] = modbusRequest[0];
            responce[1] = modbusRequest[1];
            responce[2] = modbusRequest[2];
            responce[3] = modbusRequest[3];
            responce[4] = modbusRequest[4];
            responce[5] = modbusRequest[5];
            responce[6] = modbusRequest[6];
            responce[7] = modbusRequest[7];
            responce[8] = modbusRequest[8];
            responce[9] = modbusRequest[9];
            responce[10] = modbusRequest[10];
            responce[11] = modbusRequest[11];

            return responce;
        }

        byte[] FC16Responce(ModbusClient modbusClient, byte[] modbusRequest, Device modbusDevice)
        {
            if (modbusRequest.Length < 15)
            {
                serverLogger.AddMessage(String.Format("[{0} - {1}] Invalid request length - {2} bytes. 15+ bytes expected.",
                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                    modbusClient.ClientID,
                    modbusRequest.Length
                    ));
                return null;
            }

            ushort startAddress = BitConverter.ToUInt16(new byte[] { modbusRequest[9], modbusRequest[8] }, 0);
            ushort dataCount = BitConverter.ToUInt16(new byte[] { modbusRequest[11], modbusRequest[10] }, 0);

            // Check for execute possibility
            for (int i = 0; i < dataCount; i++)
            {
                ushort dataAddress = (ushort)(startAddress + i);

                Device.ModbusRegister modbusRegister = modbusDevice.HoldingRegistersList.FirstOrDefault(m => m.Address == dataAddress);

                if (modbusRegister == null)
                {
                    if (modbusDevice.CreateDataOnRequest)
                    {
                        modbusRegister = new Device.ModbusRegister(dataAddress);
                        uiContext.Send(x => modbusDevice.HoldingRegistersList.Add(modbusRegister), null);
                    }
                    else
                    {
                        serverLogger.AddMessage(String.Format("[{0} - {1}] Illegal data addres {2} on {3}",
                                    ((IPEndPoint)modbusClient.client.Client.RemoteEndPoint).Address.ToString(),
                                    modbusClient.ClientID,
                                    dataAddress,
                                    modbusDevice.DeviceName
                                    ));

                        return new byte[] { modbusRequest[0], modbusRequest[1], modbusRequest[2], modbusRequest[3], 00, 03, modbusRequest[6], (byte)(128 + 16), 02 };
                    }
                }
            }

            // Command execute
            for (int i = 0; i < dataCount; i++)
            {
                byte[] valBytes = new byte[2];

                ushort dataAddress = (ushort)(startAddress + i);

                Device.ModbusRegister modbusRegister = modbusDevice.HoldingRegistersList.FirstOrDefault(m => m.Address == dataAddress);

                valBytes[0] = modbusRequest[13 + 1 + i * 2];
                valBytes[1] = modbusRequest[13 + 0 + i * 2];

                modbusRegister.RegisterValue = BitConverter.ToUInt16(valBytes, 0);
            }

            byte[] responce = new byte[12];

            responce[0] = modbusRequest[0];
            responce[1] = modbusRequest[1];
            responce[2] = modbusRequest[2];
            responce[3] = modbusRequest[3];
            responce[4] = modbusRequest[4];
            responce[5] = modbusRequest[5];
            responce[6] = modbusRequest[6];
            responce[7] = modbusRequest[7];
            responce[8] = modbusRequest[8];
            responce[9] = modbusRequest[9];
            responce[10] = modbusRequest[10];
            responce[11] = modbusRequest[11];

            return responce;
        }

        bool cancelListen;

        public void StartServer()
        {
            ModbusServerState = ModbusServerStateEnum.ServerStarting;

            serverLogger.AddMessage("Start server command.");
            cancelListen = false;

            if (!listenThread.IsAlive)
            {
                listenThread = new Thread(new ThreadStart(Listen));
                listenThread.IsBackground = true;
                listenThread.Start();
            }
        }

        public void StopServer()
        {
            ModbusServerState = ModbusServerStateEnum.ServerShuttingDown;

            serverLogger.AddMessage("Stop server command.");
            ServerShutdown();
        }

        public void ServerShutdown()
        {
            serverLogger.AddMessage("Server shutdown started.");

            for (int i = 0; i < clientList.Count; i++)
                clientList[i].CloseConnection();
            serverLogger.AddMessage("Closed connections with clients.");

            tcpListener.Stop();
            serverLogger.AddMessage("TCP Listener stoped.");

            cancelListen = true;

            serverLogger.AddMessage("Server shutdown completed.");
        }

        ModbusServerStateEnum modbusServerState;
        public ModbusServerStateEnum ModbusServerState
        {
            get { return modbusServerState; }
            set
            {
                modbusServerState = value;
                OnPropertyChanged();
            }
        }

        public enum ModbusServerStateEnum
        {
            ServerDown,
            ServerStarting,
            ServerUp,
            ServerShuttingDown
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
