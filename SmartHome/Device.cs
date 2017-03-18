namespace SmartHome
{
    public class Device
    {
        public Device(string name,int id,int port,string type)
        {
            Name = name;
            ID = id;
            Port = port;
            Type = Type;
        }
        public string Name { get; set; }
        public int Port { get; set; }
        public int ID { get; set; }
        public string Type { get; set; }
    }
}