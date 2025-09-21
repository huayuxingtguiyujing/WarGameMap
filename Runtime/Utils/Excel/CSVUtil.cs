using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class CSVUtil
    {

        public static void SaveCsv<T>(List<T> data, string filePath) where T : CSVInterface, new()
        {
            if (data == null || data.Count == 0)
            {
                return;
            }

            var type = typeof(T);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            using (var writer = new StreamWriter(filePath))
            {
                var headers = fields.Select(f => f.Name).Concat(props.Select(p => p.Name));
                writer.WriteLine(string.Join(",", headers));
                foreach (var item in data)
                {
                    writer.WriteLine(item.Serialize());
                }
            }
        }

        public static void LoadCsv<T>(List<T> data, string filePath) where T : CSVInterface, new()
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            var lines = File.ReadAllLines(filePath);
            if (lines.Length <= 1)
            {
                return;     // no datas
            }

            //  start serialize from 2nd line 
            data.Clear();
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) {  continue; }
                T obj = new T();
                obj.Deserialize(line);
                data.Add(obj);
            }
        }

        public static string FormatValue(object value)
        {
            if (value == null)
            {
                return "";
            }
            else if (value is string s)
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            } 
            else if (value is Color color)
            {
                return $"{color.r};{color.g};{color.b};{color.a}";
            }
            else if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                return string.Join(";", enumerable.Cast<object>());
            }
            else
            {
                return value.ToString();
            }
        }

        public static object ParseValue(string str, Type targetType)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            else if (targetType == typeof(string))
            {
                if (str.StartsWith("\"") && str.EndsWith("\""))
                {
                    str = str.Substring(1, str.Length - 2);
                    str = str.Replace("\"\"", "\""); // 还原转义
                }
                return str;
            }
            else if (targetType == typeof(Color))
            {
                var parts = str.Split(';');
                if (parts.Length == 4)
                {
                    return new Color(
                        float.Parse(parts[0], CultureInfo.InvariantCulture),
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)
                    );
                }
                throw new FormatException($"无法解析 Color: {str}");
            }
            else if (targetType == typeof(List<int>))
            {
                var list = new List<int>();
                foreach (var p in str.Split(';'))
                {
                    if (int.TryParse(p, out int val))
                    {
                        list.Add(val);
                    }
                }
                return list;
            }
            else if (targetType == typeof(List<ushort>))
            {
                var list = new List<ushort>();
                foreach (var p in str.Split(';'))
                {
                    if (ushort.TryParse(p, out ushort val))
                    {
                        list.Add(val);
                    }
                }
                return list;
            }
            else if (targetType == typeof(List<float>))
            {
                var list = new List<float>();
                foreach (var p in str.Split(';'))
                {
                    if (float.TryParse(p, NumberStyles.Any, CultureInfo.InvariantCulture, out float val))
                    {
                        list.Add(val);
                    }
                }
                return list;
            }
            else
            {
                // 其它基础类型(int, float, double, bool...)
                return Convert.ChangeType(str, targetType, CultureInfo.InvariantCulture);
            }
        }

    }
}
