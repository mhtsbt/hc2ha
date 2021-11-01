using System;
using System.Data;

namespace hc2ha
{
    public class hc2ha
    {
        private HomeAssistantClient _haClient;
        private HomeControlClient _hcClient;
        
        
        public hc2ha()
        {
            _hcClient = new HomeControlClient();
            _haClient = new HomeAssistantClient(_hcClient);
        }

        public bool IsConnected()
        {
            return _haClient.IsConnected() & _haClient.IsConnected();
        }

        public async void Start()
        {

            var ha_ip = Environment.GetEnvironmentVariable("HA_MQTT_IP");
            var hc_ip = Environment.GetEnvironmentVariable("HC_IP");
            var hc_password = Environment.GetEnvironmentVariable("HC_PASSWORD");
            
            // connect both clients
            Console.WriteLine("Connecting clients");
            await _haClient.Connect(ha_ip);
            await _hcClient.Connect(hc_ip, hc_password);
            
            _hcClient.DeviceStateChanged += _haClient.DeviceStateChanged;

            RegisterDevices();
        }

        public async void RegisterDevices()
        {
            // search for HC devices
            var devices = await _hcClient.GetDevices();

            foreach (var device in devices)
            {
                if (device.Model == "light")
                {
                   await _haClient.RegisterLight(device.Name, device.Uuid);
                } else if (device.Type == "virtual")
                {
                    await _haClient.RegisterSwitch(device.Name, device.Uuid);
                }
            }
            
        }
        
    }
}