using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
namespace NetCorePal.AliyunExporter
{
    public static class Helper
    {
        public static Dictionary<string, string> GetLabels(object data)
        {
            var props = GetProperties(data);
            var dic = new Dictionary<string, string>();
            foreach (var item in props)
            {
                dic.Add(item.Name, item.GetValue(data)?.ToString());
            }
            return dic;
        }


        static PropertyInfo[] GetProperties(object data)
        {
            return data.GetType().GetProperties().Where(p => p.PropertyType.IsPublic).ToArray();
        }


        public static string ToLabelValue(this List<string> value)
        {
            if (value == null)
            {
                return string.Empty;
            }
            return string.Join(",", value);
        }

        public static string ToStringOrEmpty<T>(this T data)
        {
            return data == null ? string.Empty : data.ToString();
        }
    }
}
