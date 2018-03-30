using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.IO;
using System.IO.Compression;

namespace ItAintBoring.ConfigurationData
{
    public class Common
    {
        public static string WebResourceKey = "ConfigurationDataResource";
        public static string START_FETCH_TAG = "STARTFETCH";
        public static string START_DATA_TAG = "STARTDATA";

        public static T GetAttribute<T>(Entity entity, Entity image, string attributeName)
        {
            if (entity.Contains(attributeName)) return (T)entity[attributeName];
            else if (image != null && image.Contains(attributeName)) return (T)image[attributeName];
            else return default(T);
        }

        public static void ParseContent(string content, out string fetchXml, out string data)
        {
            content = DecompressString(content);
            fetchXml = null;
            data = null;
            if (content != null)
            {

                int i = content.IndexOf(START_FETCH_TAG);
                int j = content.IndexOf(START_DATA_TAG);

                if (i >= 0)
                {
                    if (j < 0) j = content.Length;
                    fetchXml = content.Substring(i + START_DATA_TAG.Length+1, j - i - START_DATA_TAG.Length-1);
                }

                j = content.IndexOf(START_DATA_TAG);

                if (j < 0)
                {
                    if (i < 0) j = 0;
                    else j = content.Length; //to stop it
                }
                else j = j + START_DATA_TAG.Length;
                if (j < content.Length) data = content.Substring(j);
            }

        }

        public static string PackContent(string fetchXml, string data)
        {
            return CompressString(START_FETCH_TAG + (fetchXml != null ? fetchXml : "") + START_DATA_TAG + (data != null ? data : ""));
        }

        public static string SerializeEntity(Entity entity)
        {
            string result = null;
            var lateBoundSerializer = new DataContractSerializer(typeof(Entity));
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                lateBoundSerializer.WriteObject(ms, entity);
                ms.Flush();
                ms.Position = 0;
                System.IO.StreamReader sr = new System.IO.StreamReader(ms);
                result = sr.ReadToEnd();
            }
            return result;
        }

        public static Entity DeSerializeEntity(string data)
        {
            
            Entity result = null;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                var lateBoundSerializer = new DataContractSerializer(typeof(Entity));
                System.IO.StreamWriter sw = new System.IO.StreamWriter(ms);
                sw.Write(data);
                sw.Flush();
                ms.Position = 0;
                result = (Entity)lateBoundSerializer.ReadObject(ms);
            }
            return result;
        }

        /// <summary>
        /// Decompresses the string.
        /// </summary>
        /// <param name="compressedText">The compressed text.</param>
        /// <returns></returns>
        public static string DecompressString(string compressedText)
        {
            if (compressedText == null) return null;
            byte[] gZipBuffer = Convert.FromBase64String(compressedText);
            using (var memoryStream = new MemoryStream())
            {
                int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
                memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

                var buffer = new byte[dataLength];

                memoryStream.Position = 0;
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    gZipStream.Read(buffer, 0, buffer.Length);
                }

                return Encoding.UTF8.GetString(buffer);
            }
            
        }

        public static string CompressString(string text)
        {
            
            
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;

            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            var gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return Convert.ToBase64String(gZipBuffer);
            
        }

        public static List<SerializableEntity> GetSerializableList(List<Entity> entities)
        {
            if (entities == null) return null;

            List<SerializableEntity> result = new List<SerializableEntity>();
            foreach (var e in entities)
            {
                SerializableEntity ent = new SerializableEntity();
                ent.LogicalName = e.LogicalName;
                ent.Id = e.Id;
                var attribList = e.Attributes.ToList();
                var removeList = attribList.FindAll(a => a.Value is AliasedValue);
                foreach (var a in removeList) e.Attributes.Remove(a.Key);

                foreach (var a in e.Attributes)
                { 
                    var sa = new SerializableEntityAttribute(a);
                    ent.Attributes.Add(sa);
                }
                result.Add(ent);
            }
            return result;
        }

        public static List<Entity> GetRegularEntityList(List<SerializableEntity> entities)
        {
            if(entities == null) return null;

            List<Entity> result = new List<Entity>();
            foreach (var e in entities)
            {
                Entity ent = new Entity();
                ent.LogicalName = e.LogicalName;
                ent.Id = e.Id;

                foreach (var sa in e.Attributes)
                {
                    var a = sa.ConvertToAttribute();
                    ent.Attributes[a.Key] = a.Value;
                }
                result.Add(ent);
            }
            return result;
        }

        public static string SerializeEntityList(List<Entity> entities)
        {
            string result = null;
            var serializableList = GetSerializableList(entities);

            
            var lateBoundSerializer = new DataContractSerializer(typeof(List<SerializableEntity>));
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                lateBoundSerializer.WriteObject(ms, serializableList);
                
                ms.Flush();
                ms.Position = 0;
                System.IO.StreamReader sr = new System.IO.StreamReader(ms);
                result = sr.ReadToEnd();
                
                //result = CompressString(result);
            }
            return result;
        }

        public static List<Entity> DeSerializeEntityList(string data)
        {
            if (data == null) return null;
            //data = DecompressString(data);
            List<SerializableEntity> result = null;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                var lateBoundSerializer = new DataContractSerializer(typeof(List<SerializableEntity>));
                System.IO.StreamWriter sw = new System.IO.StreamWriter(ms);
                sw.Write(data);
                sw.Flush();
                ms.Position = 0;
                result = (List<SerializableEntity>)lateBoundSerializer.ReadObject(ms);
            }
            
            return GetRegularEntityList(result);
        }

        public static DataSetResource ResourceFromString(string data)
        {
            DataSetResource result = null;
            using (MemoryStream ms = new MemoryStream())
            {
                StreamWriter sw = new StreamWriter(ms);
                sw.Write(data);
                sw.Flush();
                ms.Position = 0;
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(DataSetResource));
                result = (DataSetResource)serializer.ReadObject(ms);
            }
            return result;
        }

        public static string ResourceToString(DataSetResource dsr)
        {
            string result = null;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(DataSetResource));
                serializer.WriteObject(ms, dsr);
                ms.Flush();
                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                result = sr.ReadToEnd();
                result = result.Replace("\\u000a", "");



            }
            return result;
        }

        public static string CurrentTime()
        {
            return DateTime.UtcNow.ToString();
        }
    }

    
}
