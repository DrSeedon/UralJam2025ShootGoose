using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using UnityEngine;

namespace Seedon
{
    /// <summary>
    /// Утилиты для работы с файлами.
    /// </summary>
    public class FileIOUtility : MonoBehaviour
    {
        public static object ReadFromBinary<T>(string file)
        {
            var path = Application.persistentDataPath + "/" + file;
            if (File.Exists(path))
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream stream = File.Open(path, FileMode.Open))
                {
                    var data = (T)bf.Deserialize(stream);

                    return data;
                }
            }
            return null;
        }

        public static void WriteToBinary<T>(string file, T data)
        {
            var path = Application.persistentDataPath + "/" + file;

            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream stream = File.Open(path, FileMode.OpenOrCreate))
            {
                bf.Serialize(stream, data);
            }
        }

        public static object ReadFromJson<T>(string file)
        {
            var path = Application.persistentDataPath + "/" + file;
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<T>(json);
        }

        public static void WriteToJson<T>(string file, T data)
        {
            var path = Application.persistentDataPath + "/" + file;
            if (!File.Exists(path))
            {
                var f = File.Create(path);
                f.Close();
            }

            var settings = new JsonSerializerSettings();
            //settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            
            

            var json = JsonConvert.SerializeObject(data, Formatting.Indented, settings);
            File.WriteAllText(path, json);
        }
    }
}