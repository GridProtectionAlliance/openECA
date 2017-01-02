using System;

namespace openECAClient.Model
{
    public class StatusLight
    {
        public string DeviceAcronym { get; set; }
        public DateTime Timestamp { get; set; }
        public bool GoodData { get; set; }
    }
}
