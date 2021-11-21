using System;
using System.Data;
using System.Threading.Tasks;

namespace hc2ha
{
    public class hc2ha
    {
        private readonly HomeAssistantClient _haClient;
        private readonly HomeControlClient _hcClient;
        
        
        public hc2ha()
        {
            _hcClient = new HomeControlClient();
            _haClient = new HomeAssistantClient(_hcClient);
        }

        public bool IsConnected()
        {
            return _hcClient.IsConnected() & _haClient.IsConnected();
        }

        public async void Start()
        {

            var ha_ip = Environment.GetEnvironmentVariable("HA_MQTT_IP");
            var ha_port = Environment.GetEnvironmentVariable("HA_MQTT_PORT");
            var hc_ip = Environment.GetEnvironmentVariable("HC_IP");
            var hc_password = Environment.GetEnvironmentVariable("HC_PASSWORD");

            if (string.IsNullOrEmpty(ha_ip))
            {
                Console.WriteLine("Home assistant MQTT IP not set, please set the env var HA_MQTT_IP");
                Environment.Exit(1);
            }


            
            if (string.IsNullOrEmpty(hc_ip))
            {
                Console.WriteLine("HomeControl IP not set, please set the env var HC_IP");
                Environment.Exit(1);
            }

            if (string.IsNullOrEmpty(hc_password))
            {
                Console.WriteLine("HomeControl password not set, please set the env var HC_PASSWORD");
                Environment.Exit(1);
            }

            // connect both clients
            Console.WriteLine("Connecting clients");
            if (string.IsNullOrEmpty(ha_port))
            {
                await _haClient.Connect(ha_ip);
            }
            else
            {
                await _haClient.Connect(ha_ip, int.Parse(ha_port));
            }

            await _hcClient.Connect(hc_ip, hc_password);
            
            _hcClient.DeviceStateChanged += _haClient.DeviceStateChanged;

            RegisterDevices();

        }

        private async void RegisterDevices()
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