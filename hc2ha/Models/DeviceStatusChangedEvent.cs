using System;

namespace hc2ha.Models
{
    public class DeviceStatusChangedEvent : EventArgs
    {
       
            public DeviceStatusChangedEvent(string deviceId, string newState)
            {
                this.DeviceId = deviceId;
                this.NewState = newState;
            }

            public string DeviceId { get; private set; }
            public string NewState { get; private set; }
        }
    }
