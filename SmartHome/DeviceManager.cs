using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace SmartHome
{
    public class DeviceManager
    {
        public static Device light1 = new Device("light1", 1, 1, "light");
        public static Device fan1 = new Device("fan1", 2, 2, "fan");
        public List<Device> Devices =new List<Device>()
        {
            light1,
            fan1
        };
        public void Set(string dev,int status)
        {
            string command = String.Empty;
            foreach(var items in Devices)
            {
                if(items.Name==dev)
                {
                    command = items.Port.ToString() +'-'+ (status % 2).ToString();
                    break;
                }
            }
            if (command !=null)
            {
                UDPSend(command);
            }
        }
        public void SetAll(string type,int status)
        {
            string command = String.Empty;
            foreach (var items in Devices)
            {
                if (items.Type == type)
                {
                    command = items.Port.ToString() + '-' + (status % 2).ToString();
                    UDPSend(command);
                    Thread.Sleep(500);
                }
            }
        }
        public bool IsExist(string dev)
        {
            foreach (var items in Devices)
            {
                if (items.Name == dev)
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsTypeExist(string type)
        {
            foreach (var items in Devices)
            {
                if (items.Type == type)
                    return true;
                
            }
            return false;
        }
        public void UDPSend(string command)
        {
            using (var client = new UdpClient())
            {
                client.EnableBroadcast = true;
                var endpoint = new IPEndPoint(IPAddress.Broadcast, 15000);
                Console.WriteLine(IPAddress.Broadcast);
                var message = Encoding.ASCII.GetBytes(command);
                client.SendAsync(message, message.Length, endpoint);
                client.Close();
            }
        }
    }
}
