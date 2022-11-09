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
        
        public event EventHandler<DeviceStatusChangedEvent> DeviceStateChanged;

        public HomeControlClient()
        {
            // Create a new MQTT client.
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.UseApplicationMessageReceivedHandler(e =>
            {

                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                string topic = e.ApplicationMessage.Topic;

                if (topic == "hobby/control/devices/evt")
                {
                    DeviceChangedMessage msg = JsonSerializer.Deserialize<DeviceChangedMessage>(payload);


                    foreach (var device_event in msg.Params[0].Devices)
                    {

                        var uuid = device_event.Uuid;
                        var device = GetDeviceByUuid(uuid: uuid);

                        Console.WriteLine(
                            $"Change state uuid: {uuid} status: {device_event.Properties[0].Status} name: {device.Name} type: {device.Type}");

                        if (device.Type == "virtual")
                        {
                            OnDeviceStateChanged(new DeviceStatusChangedEvent(device_event.Uuid,
                                device_event.Properties[0].Status));

                        }
                        else if (device.Model == "light")
                        {
                            OnDeviceStateChanged(new DeviceStatusChangedEvent(msg.Params[0].Devices[0].Uuid,
                                device_event.Properties[0].Status));
                        }
                    }

                }
                else if (topic == "hobby/control/devices/rsp")
                {
                    RegisterDevices(payload);
                }

            });
            
            _mqttClient.UseConnectedHandler(e =>
            {
                Console.WriteLine("Connected with HomeControl hub");
            });
            
        }

        private void RegisterDevices(string payload)
        {
            var deviceList = JsonSerializer.Deserialize<DeviceListModel>(payload);

            if (deviceList != null)
            {
                _devices = deviceList.Params[0].Devices;

                var lights = deviceList.Params.First().Devices.Where(x => (x.Type is "relay" || x.Type is "smartrelay"));
                var virtualDevices = deviceList.Params.First().Devices.Where(x => x.Type == "virtual");

                foreach (var light in lights)
                {
                    Console.WriteLine($"- Found light {light.Name} {light.Uuid}");
                }

                foreach (var device in virtualDevices)
                {
                    Console.WriteLine("- Found virtual device " + device.Name + " " + device.Uuid);
                }

            }
            else
            {
                Console.WriteLine("Could not find any devices");
            }

        }
        
        protected virtual void OnDeviceStateChanged(DeviceStatusChangedEvent e)
        {
            EventHandler<DeviceStatusChangedEvent> handler = DeviceStateChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        
        public async Task Connect(string ip, string password)
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(ip, 8884)
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
            await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("hobby/control/devices/evt").Build());
            
        }

        public Device GetDeviceByUuid(string uuid)
        {
            return  _devices.FirstOrDefault(x => x.Uuid == uuid);
        }

        public async Task<List<Device>> GetDevices()
        {
            // empty the list 
            _devices = null;
            
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("hobby/control/devices/cmd")
                .WithPayload("{\"Method\":\"devices.list\"}")
                .WithExactlyOnceQoS()
                //.WithRetainFlag()
                .Build();
            
            await _mqttClient.SubscribeAsync(new TopicFilterBuilder().WithTopic("hobby/control/devices/rsp").Build());
            
            while (_devices == null)
            {
                await _mqttClient.PublishAsync(message, CancellationToken.None);
                Console.WriteLine("waiting for devices");
                Thread.Sleep(1000);
            }

            return _devices;
        }
        
        public async Task TurnVirtualOn(string uuid)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("hobby/control/devices/cmd")
                .WithPayload("{\"Method\": \"devices.control\",\"Params\": [{\"Devices\": [{\"Properties\": [{\"Status\": \"True\"}],\"Uuid\": \""+uuid+"\"}]}]}")
                .WithExactlyOnceQoS()
                //.WithRetainFlag()
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);
        }
        
        public async Task TurnVirtualOff(string uuid)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("hobby/control/devices/cmd")
                .WithPayload("{\"Method\": \"devices.control\",\"Params\": [{\"Devices\": [{\"Properties\": [{\"Status\": \"False\"}],\"Uuid\": \""+uuid+"\"}]}]}")
                .WithExactlyOnceQoS()
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);
        }

        public async Task TurnLightOn(string uuid)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("hobby/control/devices/cmd")
                .WithPayload("{\"Method\": \"devices.control\",\"Params\": [{\"Devices\": [{\"Properties\": [ {\"Status\": \"On\"}],\"Uuid\": \""+uuid+"\"}]}]}")
                .WithExactlyOnceQoS()
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);
        }
        
        public async Task TurnLightOff(string uuid)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic("hobby/control/devices/cmd")
                .WithPayload("{\"Method\": \"devices.control\",\"Params\": [{\"Devices\": [{\"Properties\": [{\"Status\": \"Off\"}],\"Uuid\": \""+uuid+"\"}]}]}")
                .WithExactlyOnceQoS()
                .Build();

            await _mqttClient.PublishAsync(message, CancellationToken.None);
        }

        public bool IsConnected()
        {
            return _mqttClient.IsConnected;
        }
    }
}