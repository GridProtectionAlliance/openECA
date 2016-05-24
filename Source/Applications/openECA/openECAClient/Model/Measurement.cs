using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace openECAClient.Model
{
    public class Measurement
    {
        public Double Timestamp { get; set; }
        public Double Value { get; set; }
        public Guid ID { get; set; }
    }
}
