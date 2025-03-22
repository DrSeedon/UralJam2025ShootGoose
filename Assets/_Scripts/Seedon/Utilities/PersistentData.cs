using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EasyButtons;
using UnityEngine;

namespace Seedon
{
    /// <summary>
    /// Класс для работы с постоянными данными.
    /// </summary>
    public class PersistentData : PersistentSingleton<PersistentData>
    {
        private Dictionary<string, DataEntry> data = new Dictionary<string, DataEntry>();

        public string savePath;

        public float saveInterval = 5f; // Интервал автосохранения в секундах

        public void TestPersistentData()
        {
            // Удаляем сохраненные данные перед тестом
            if (SaveFileExists())
            {
                DeleteSaveFile();
            }

            // Устанавливаем и сохраняем 1000 разных значений
            for (int i = 0; i < 1000; i++)
            {
                Set($"intKey{i}", i);
                Set($"floatKey{i}", i + 0.5f);
                Set($"stringKey{i}", $"Test string {i}");
            }

            SaveData();

            // Загружаем данные обратно
            LoadData();

            // Проверяем, что значения были правильно сохранены и загружены
            for (int i = 0; i < 1000; i++)
            {
                Debug.Assert(Get<int>($"intKey{i}") == i);
                Debug.Assert(Math.Abs(Get<float>($"floatKey{i}") - (i + 0.5f)) < 0.001);
                Debug.Assert(Get<string>($"stringKey{i}") == $"Test string {i}");
            }

            Debug.Log("Test passed!");
            
            data.Clear();
            
            SaveData();
        }


        protected override void Awake()
        {
            base.Awake();
            savePath = Application.persistentDataPath + "/savefile.json";
            Debug.Log(savePath);
        }

        private void Start()
        {
            TestPersistentData();
            LoadData();
        }

        private void OnDestroy()
        {
            SaveData();
        }

        private IEnumerator<WaitForSeconds> AutoSaveCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(saveInterval);
                SaveData();
            }
        }

        public bool SaveFileExists()
        {
            return File.Exists(savePath);
        }

        [Button]
        public void DeleteSaveFile()
        {
            if (SaveFileExists())
            {
                data = new Dictionary<string, DataEntry>();
                File.Delete(savePath);
            }
        }

        public void Set(string key, string value)
        {
            DataEntry dataEntry = new DataEntry { stringValue = value, dataType = DataEntry.DataType.String };
            data[key] = dataEntry;
        }

        public void Set(string key, int value)
        {
            DataEntry dataEntry = new DataEntry { intValue = value, dataType = DataEntry.DataType.Int };
            data[key] = dataEntry;
        }

        public void Set(string key, float value)
        {
            DataEntry dataEntry = new DataEntry { floatValue = value, dataType = DataEntry.DataType.Float };
            data[key] = dataEntry;
        }

        public T Get<T>(string key, T defaultValue = default(T))
        {
            if (data.ContainsKey(key))
            {
                DataEntry entry = data[key];
        
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)entry.stringValue;
                }
                else if (typeof(T) == typeof(int))
                {
                    if (entry.dataType == DataEntry.DataType.Float)
                        return (T)(object)(int)entry.floatValue;
                    else
                        return (T)(object)entry.intValue;
                }
                else if (typeof(T) == typeof(float))
                {
                    if (entry.dataType == DataEntry.DataType.Int)
                        return (T)(object)(float)entry.intValue;
                    else
                        return (T)(object)entry.floatValue;
                }
            }

            return defaultValue;
        }

        public bool HasKey(string key)
        {
            return data.ContainsKey(key);
        }

        public void SaveData()
        {
            string jsonData = JsonUtility.ToJson(new SerializableDictionary<DataEntry>(data), true);
            File.WriteAllText(savePath, jsonData);
        }

        public void LoadData()
        {
            if (File.Exists(savePath))
            {
                string jsonData = File.ReadAllText(savePath);
                data = JsonUtility.FromJson<SerializableDictionary<DataEntry>>(jsonData).ToDictionary();
            }
            else
            {
                data = new Dictionary<string, DataEntry>();
            }

            StartCoroutine(AutoSaveCoroutine());
        }

        
        [Button]
        public void ShowAllData()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("All stored data:\n");

            foreach (var entry in data)
            {
                string key = entry.Key;
                DataEntry value = entry.Value;

                string info = "";
                switch (value.dataType)
                {
                    case DataEntry.DataType.String:
                        info = value.stringValue;
                        break;
                    case DataEntry.DataType.Int:
                        info = value.intValue.ToString();
                        break;
                    case DataEntry.DataType.Float:
                        info = value.floatValue.ToString();
                        break;
                }

                sb.Append($"Key: {key}, Value: {info}, Type: {value.dataType}\n");
            }

            Debug.Log(sb.ToString());
        }
    }

    [System.Serializable]
    public class SerializableDictionary<T>
    {
        public List<string> keys = new List<string>();
        public List<T> values = new List<T>();

        public SerializableDictionary(Dictionary<string, T> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public Dictionary<string, T> ToDictionary()
        {
            var result = new Dictionary<string, T>();

            for (int i = 0; i < keys.Count; i++)
            {
                result.Add(keys[i], values[i]);
            }

            return result;
        }
    }

    [System.Serializable]
    public class DataEntry
    {
        public string stringValue;
        public int intValue;
        public float floatValue;
        public DataType dataType;

        public enum DataType
        {
            String,
            Int,
            Float
        }
    }
}