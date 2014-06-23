using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.Serialization;

namespace SkypeBot
{
    public sealed class Config
    {
        private const string XmlFilePath = "config.xml";
        private DataClass data = new DataClass();
        
        public Dictionary<string, string> LoadConfig()
        {
            data = ReadXmlData();
            Dictionary<string, string> dic = new Dictionary<string,string>();
            dic.Add("mainconf", data.MainConf);
            dic.Add("resender", (data.Resender ? "1" : "0"));

            return dic;
        }

        public void SetMainConf(string mainConf)
        {
            data.MainConf = mainConf;
            WriteXmlData(data);
        }

        public void SetReSender(bool reSender)
        {
            data.Resender = reSender;
            WriteXmlData(data);
        }

        public DataClass ReadXmlData()
        {
            DataClass dataClass = new DataClass();

            if (!System.IO.File.Exists(XmlFilePath))
            {
                WriteXmlData(dataClass);
                return dataClass;
            }

            XmlSerializer xmlserializer = new XmlSerializer(typeof(DataClass));
            object oDataClass;
            XmlReader xmlr = XmlReader.Create(XmlFilePath);
            try
            {
                oDataClass = xmlserializer.Deserialize(xmlr);
                dataClass = (DataClass)oDataClass;
                return dataClass;
            }
            catch
            {
                return dataClass;
            }
            finally
            {
                xmlr.Close();
            }
        }

        public void WriteXmlData(DataClass dataClass)
        {
            XmlWriter writer = XmlWriter.Create(XmlFilePath);
            XmlSerializer xmlserializer = new XmlSerializer(typeof(DataClass));
            xmlserializer.Serialize(writer, dataClass);
            
            writer.Close();
        }
    }

    [Serializable]
    public class DataClass
    {
        public bool Resender;
        public string MainConf;

        public DataClass()
        {
            Resender = false;
            MainConf = "";
        }
    }
}
