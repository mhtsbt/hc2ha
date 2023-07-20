using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using hc2ha.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace hc2ha
{

    public class SwitchMapping
    {
        public string SwitchName { get; set; }
        public string HaTopic { get; set; }
        public string HaName { get; set; }  
    }
    
    public class HomeAssistantClient
    {
        private readonly IMqttClient _mqttClient;
        private readonly HomeControlClient _hcClient;
        private readonly List<SwitchMapping> _switchMapping;
        
        private readonly string _restApiKey = "";

        public HomeAssistantClient(HomeControlClient hcClient)
        {
            _hcClient = hcClient;
            
            _restApiKey = Environment.GetEnvironmentVariable("HA_REST_KEY");

            _switchMapping = new List<SwitchMapping>();
            _switchMapping.Add(new SwitchMapping()
            {
                SwitchName = "stubru_keuken",
                HaTopic = "hastates/switch/stubru_living2/state",
                HaName = "stubru_living2"
            });
            
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

                var mappedSwitch = _switchMapping.FirstOrDefault(x => x.HaTopic == e.ApplicationMessage.Topic);
                Console.WriteLine(e.ApplicationMessage.Topic);

                if (mappedSwitch != null)
                {
                    Console.WriteLine("mapped item changed");
                    var mappedDevice = this._hcClient.GetDeviceByName(name: mappedSwitch.SwitchName);

                    if (payload.ToUpper() == "ON")
                    {
                        await _hcClient.TurnVirtualOn(mappedDevice.Uuid);
                    }
                    else
                    {
                        await _hcClient.TurnVirtualOff(mappedDevice.Uuid);
                    }
                    
                    return;
                }
                
                
                var topic = e.ApplicationMessage.Topic.Split("/");
                string device_uuid = topic[1];
                string cmd_name = topic[2];

                var device = this._hcClient.GetDeviceByUuid(uuid: device_uuid);

                if (device.Type == "action" && device.Model == "light")
                {
                    if (cmd_name == "set")
                    {
                        var payloadObj = JsonSerializer.Deserialize<LightSetMessage>(payload); 

                        if (payloadObj.state == "ON")
                        {
                            await _hcClient.TurnLightOn(device_uuid);
                        }
                        else if (payloadObj.state == "OFF")
                        {
                            await _hcClient.TurnLightOff(device_uuid);
                        }
                        else
                        {
                            Console.WriteLine("Did not understand the message");
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
                await SetStateOfSwitch(device, e.NewState);
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
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);
            
        }
        
        private async Task SetStateOfSwitch(Device device, string state)
        {
            var deviceName = device.Name.Replace(' ', '_').ToLower();
            var mappedDevice = _switchMapping.FirstOrDefault(x => x.SwitchName == deviceName);
            
            Console.WriteLine($"state: {state}");

            if (mappedDevice != null)
            {

                string service =  (state.ToUpper() == "FALSE") ? "turn_off" : "turn_on";
                
                // use HA rest api, cannot use mqtt
                using (var client = new HttpClient())
                {
                    var content = new StringContent("{\"entity_id\": \"switch."+mappedDevice.HaName+"\"}", Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_restApiKey}");
                    var request = client.PostAsync($"https://ha.epsgreedy.be/api/services/switch/{service}", content);
                    var response = await request.Result.Content.ReadAsStringAsync();
                    Console.WriteLine($"REST {service} result: {response}");
                    
                }
            }
            else
            {
                // device is configured through mqtt
                

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
                    .WithTopic("homeassistant/" + device.Uuid + "/state")
                    .WithPayload(state)
                    .WithExactlyOnceQoS()
                    .Build();

                await _mqttClient.PublishAsync(message, CancellationToken.None);

            }

        }

        public async Task Connect(string ip, int port = 1883)
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId("hc2ha")
                .WithTcpServer(ip, port)
                .WithCleanSession()
                .Build();
            
            await _mqttClient.ConnectAsync(options, CancellationToken.None);
        }

        public async Task RegisterLight(string name, string uuid)
        {
            name = name.Replace(' ', '_').ToLower();
            
            var message = new MqttApplicationMessageBuilder()
                .WithRetainFlag()
                .WithTopic($"homeassistant/light/{uuid}/config")
                .WithPayload("{\"~\": \"homeassistant/"+uuid+"\",\"name\": \""+name+"\", \"unique_id\": \""+uuid+"\", \"cmd_t\": \"~/set\", \"stat_t\": \"~/state\", \"schema\": \"json\", \"brightness\": false}")
                .WithExactlyOnceQoS()
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);
            await _mqttClient.SubscribeAsync("homeassistant/" + uuid + "/set");
        }
        
        public async Task RegisterSwitch(string name, string uuid)
        {
            name = name.Replace(' ', '_').ToLower();
            var mappedItem = _switchMapping.FirstOrDefault(x => x.SwitchName == name);
            
            if (mappedItem != null)
            {
                await _mqttClient.SubscribeAsync(mappedItem.HaTopic);
            }
            else
            {

                Console.WriteLine($"homeassistant/switch/{uuid}/config");
                Console.WriteLine("{\"~\": \"homeassistant/" + uuid + "\",\"name\": \"" + name +
                                  "\", \"command_topic\": \"~/set\", \"state_topic\": \"~/state\"}");

                var message = new MqttApplicationMessageBuilder()
                    .WithRetainFlag()
                    .WithTopic($"homeassistant/switch/{uuid}/config")
                    .WithPayload("{\"~\": \"homeassistant/" + uuid + "\",\"name\": \"" + name +
                                 "\", \"command_topic\": \"~/set\", \"state_topic\": \"~/state\"}")
                    .WithExactlyOnceQoS()
                    .Build();

                await _mqttClient.PublishAsync(message, CancellationToken.None);
                await _mqttClient.SubscribeAsync("homeassistant/" + uuid + "/set");

            }
        }
        
    }
}