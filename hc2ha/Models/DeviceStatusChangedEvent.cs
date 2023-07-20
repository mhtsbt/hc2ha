using System;

namespace hc2ha.Models
{
    public class DeviceStatusChangedEvent : EventArgs
    {
       
            public DeviceStatusChangedEvent(string deviceId, string newState, string source)
            {
                this.DeviceId = deviceId;
                this.NewState = newState;
                this.Source = source;
            }

            public string DeviceId { get; private set; }
            public string NewState { get; private set; }
            public string Source { get; private set; }
        }
    }
