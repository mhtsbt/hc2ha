using System;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using hc2ha.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace hc2ha
{
    public class HomeAssistantClient
    {
        private IMqttClient _mqttClient;
        private HomeControlClient _hcClient;
        
        public HomeAssistantClient(HomeControlClient hcClient)
        {
            _hcClient = hcClient;
            
            // Create a new MQTT client.
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.UseConnectedHandler(e =>
            {
                Console.WriteLine("Connected with HA server");
            });

            _mqttClient.UseApplicationMessageReceivedHandler(async e =>
            {
                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                
                var topic = e.ApplicationMessage.Topic.Split("/");
                string device_uuid = topic[1];
                string cmd_name = topic[2];

                var device = this._hcClient.GetDeviceByUuid(uuid: device_uuid);

                if (device.Type == "action" && device.Model == "light")
                {

                    if (cmd_name == "set")
                    {
                        if (payload == "{\"state\": \"ON\"}")
                        {
                            await _hcClient.TurnLightOn(device_uuid);
                        }
                        else
                        {
                            await _hcClient.TurnLightOff(device_uuid);
                        }
                    }

                } else if (device.Type == "virtual")
                {
                    if (payload == "ON")
                    {
                        await _hcClient.TurnVirtualOn(device_uuid);
                    }
                    else
                    {
                        await _hcClient.TurnVirtualOff(device_uuid);
                    }
                }

            });

        }
        
        public async void DeviceStateChanged(object sender, DeviceStatusChangedEvent e)
        {
            var device = _hcClient.GetDeviceByUuid(uuid: e.DeviceId);
           
            if (device.Model == "light")
            {
                // HC changed the state of a device
                await SetStateOfLight(e.DeviceId, e.NewState);
            }
            else
            {
                await SetStateOfSwitch(e.DeviceId, e.NewState);
            }
        }

        public bool IsConnected()
        {
            return _mqttClient.IsConnected;
        }

        private async Task SetStateOfLight(string uuid, string state)
        {
            state = state.ToUpper();
            
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("homeassistant/" + uuid + "/state")
                .WithPayload("{\"state\":\""+state+"\"}")
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);
            
        }
        
        private async Task SetStateOfSwitch(string uuid, string state)
        {
            Console.WriteLine($"state: {state}");
            state = state.ToUpper();

            if (state == "FALSE")
            {
                state = "OFF";
            }
            else
            {
                state = "ON";
            }
            
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("homeassistant/" + uuid + "/state")
                .WithPayload(state)
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();
            

            await _mqttClient.PublishAsync(message, CancellationToken.None);
            
        }

        public async Task Connect(string ip)
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId("hc2ha")
                .WithTcpServer(ip, 18803)
                .WithCleanSession()
                .Build();
            
            await _mqttClient.ConnectAsync(options, CancellationToken.None);
        }

        public async Task RegisterLight(string name, string uuid)
        {
            name = name.Replace(' ', '_').ToLower();
            
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"homeassistant/light/{uuid}/config")
                .WithPayload("{\"~\": \"homeassistant/"+uuid+"\",\"name\": \""+name+"\", \"unique_id\": \""+uuid+"\", \"cmd_t\": \"~/set\", \"stat_t\": \"~/state\", \"schema\": \"json\", \"brightness\": false}")
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);

            await _mqttClient.SubscribeAsync("homeassistant/" + uuid + "/set");

        }
        
        public async Task RegisterSwitch(string name, string uuid)
        {
            name = name.Replace(' ', '_').ToLower();
            
            Console.WriteLine($"homeassistant/switch/{uuid}/config");
            Console.WriteLine("{\"~\": \"homeassistant/"+uuid+"\",\"name\": \""+name+"\", \"command_topic\": \"~/set\", \"state_topic\": \"~/state\"}");
            
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"homeassistant/switch/{uuid}/config")
                .WithPayload("{\"~\": \"homeassistant/"+uuid+"\",\"name\": \""+name+"\", \"command_topic\": \"~/set\", \"state_topic\": \"~/state\"}")
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);

            await _mqttClient.SubscribeAsync("homeassistant/" + uuid + "/set");

        }
        
    }
}