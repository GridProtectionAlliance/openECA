using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;

namespace VoltController.Testing
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class RawMeasurements
    {
        private RawMeasurementsMeasurement[] itemsField;
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Measurement", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public RawMeasurementsMeasurement[] Items
        {
            get
            {
                return this.itemsField;
            }
            set
            {
                this.itemsField = value;
            }
        }

        /// <summary>
        /// Deserializes the collection of raw measurement key-value pairs from the *.xml file.
        /// </summary>
        /// <param name="pathName">The path name to the file to be deserialized.</param>
        /// <returns>A <see cref="SynchrophasorAnalytics.Testing.RawMeasurements"/> object.</returns>
        public static RawMeasurements DeserializeFromXml(string pathName)
        {
            try
            {
                RawMeasurements snapshot = null;

                XmlSerializer deserializer = new XmlSerializer(typeof(RawMeasurements));

                StreamReader reader = new StreamReader(pathName);

                snapshot = (RawMeasurements)deserializer.Deserialize(reader);

                reader.Close();

                return snapshot;
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to Deserialize the Raw Measurements from the Snapshot File: " + exception.ToString());
            }
        }
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class RawMeasurementsMeasurement
    {

        private string keyField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Key
        {
            get
            {
                return this.keyField;
            }
            set
            {
                this.keyField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }
    }
}
