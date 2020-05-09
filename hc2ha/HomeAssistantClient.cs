using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

                if (cmd_name == "set")
                {
                    if (payload == "{\"state\": \"ON\"}")
                    {
                       await _hcClient.TurnLightOn(device_uuid);
                       await SetStateOfLight(device_uuid, "ON");
                    }
                    else
                    {
                        await _hcClient.TurnLightOff(device_uuid);
                        await SetStateOfLight(device_uuid, "OFF");
                    }
                }
            });

        }

        private async Task SetStateOfLight(string uuid, string state)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("homeassistant/" + uuid + "/state")
                .WithPayload("{\"state\":\""+state+"\"}")
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);
            
        }

        public async Task Connect(string ip)
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId("hc2ha")
                .WithTcpServer(ip, 1883)
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
        
    }
}