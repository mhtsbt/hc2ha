using System;
using System.Collections.Generic;
using System.Linq;
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
    public class HomeControlClient
    {
        private readonly IMqttClient _mqttClient;
        private List<Device> _devices;

        public HomeControlClient()
        {
            // Create a new MQTT client.
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                
                var deviceList = JsonSerializer.Deserialize<DeviceListModel>(Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
                _devices = deviceList.Params[0].Devices;

                var lights = deviceList.Params.First().Devices.Where(x => x.Model == "light" & x.Type == "action");

                foreach (var light in lights)
                {
                    Console.WriteLine("- Found light "+light.Name);
                }
                
            });
            
            _mqttClient.UseConnectedHandler(e =>
            {
                Console.WriteLine("Connected with HomeControl hub");
            });
        }
        
        public async Task Connect(string ip, string password)
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId("hc2ha")
                .WithTcpServer("10.0.3.29", 8884)
                .WithCredentials("hobby", password)
                .WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    CertificateValidationCallback = (X509Certificate x, X509Chain y, SslPolicyErrors z, IMqttClientOptions o) =>
                    {
                        // TODO: Check valid Niko certificate
                        return true;
                    }
                })
                .WithCleanSession()
                .Build();
            
            await _mqttClient.ConnectAsync(options, CancellationToken.None);
        }

        public async Task<List<Device>> GetDevices()
        {
            // empty the list 
            _devices = null;
            
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("hobby/control/devices/cmd")
                .WithPayload("{'Method':'devices.list'}")
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();
            
            await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("hobby/control/devices/rsp").Build());

            await _mqttClient.PublishAsync(message, CancellationToken.None);

            while (_devices == null)
            {
                Thread.Sleep(1000);
            }

            return _devices;
        }

        public async Task TurnLightOn(string uuid)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("hobby/control/devices/cmd")
                .WithPayload("{'Method': 'devices.control','Params': [{'Devices': [{'Properties': [{'Brightness': '100'}, {'Status': 'On'}],'Uuid': '"+uuid+"'}]}]}")
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);
        }
        
        public async Task TurnLightOff(string uuid)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("hobby/control/devices/cmd")
                .WithPayload("{'Method': 'devices.control','Params': [{'Devices': [{'Properties': [{'Brightness': '100'}, {'Status': 'Off'}],'Uuid': '"+uuid+"'}]}]}")
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);
        }

    }
}