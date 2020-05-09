using System;
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
    
    
    
    class Program
    {
        static void Main(string[] args)
        {
            var hc2ha = new hc2ha();
            hc2ha.Start();
            
            // keep running
            while (true)
            {
                Thread.Sleep(100000);
            }

        }
    }
}