using System;
using System.Collections.Generic;

namespace hc2ha.Models
{
public class DeviceListModel
{
    public string Method { get; set; }
    public List<DeviceListParam> Params { get; set; }
}

public class DeviceListParam
{
    public List<Device> Devices { get; set; }
}

public class Device
{
    //public List<Property> Properties { get; set; }
    public string Name { get; set; }
    public string Technology { get; set; }
    public string Uuid { get; set; }
    public string Identifier { get; set; }
    //public List<Dictionary<string, PropertyDefinition>> PropertyDefinitions { get; set; }
    public string Online { get; set; }
    public Property[] Properties { get; set; }
    public string Model { get; set; }
   // public List<Trait> Traits { get; set; }
    public string Type { get; set; }
  //  public List<Dictionary<string, string>> Parameters { get; set; }
}

public class Property
{
    public string Status { get; set; }
    public string ReportInstantUsage { get; set; }
    public string ElectricalPowerToGrid { get; set; }
    public string ElectricalEnergyFromGrid { get; set; }
    public string ElectricalPeakPowerToGrid { get; set; }
    public string ElectricalPowerFromGrid { get; set; }
    public string GasVolume { get; set; }
    public string ElectricalPeakPowerFromGrid { get; set; }
    public string ElectricalEnergyToGrid { get; set; }
    public string AvailableFwInfo { get; set; }
    public string CurrentFwInfo { get; set; }
    public string ErrorCode { get; set; }
    public string UpgradeStatus { get; set; }
}

public class PropertyDefinition
{
    public string Description { get; set; }
    public bool HasStatus { get; set; }
    public bool CanControl { get; set; }
}

public class Trait
{
    public string MacAddress { get; set; }
    public string ProductId { get; set; }
    public long? Channel { get; set; }
    public string HubType { get; set; }
}

}