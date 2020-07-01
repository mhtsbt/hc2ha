namespace hc2ha.Models
{
    
    public class DeviceChangedMessage
    {
        public string Method { get; set; }
        public Param[] Params { get; set; }
    }
    
    public class Param
    {
        public Device[] Devices { get; set; }
    }

}