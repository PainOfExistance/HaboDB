using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace HaboDB
{
    public class Utils
    {
        public static string RandomString()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnoprstuvzxyq0123456789";
            string str = "";
            var random = new Random();

            for (int i = 0; i < 23; i++)
            {
                str += chars[random.Next(chars.Length)];
            }

            return str;
        }

        public static List<string> ProccessField(string field)
        {
            List<string> fields = field.Split(' ').ToList();
            if (fields.Count == 1)
            {
                fields.Add("");
                return fields;
            }

            return fields;
        }

        public static bool Operate(string comparator, object val1, object val2)
        {
            if (val1.GetType() != val2.GetType())
            {
                return false;
            }

            switch (comparator)
            {
                case "=":
                    return Comparer.DefaultInvariant.Compare(val1, val2) == 0;
                case "!=":
                    return Comparer.DefaultInvariant.Compare(val1, val2) != 0;
                case ">":
                    return Comparer.DefaultInvariant.Compare(val1, val2) > 0;
                case "<":
                    return Comparer.DefaultInvariant.Compare(val1, val2) < 0;
                case ">=":
                    return Comparer.DefaultInvariant.Compare(val1, val2) >= 0;
                case "<=":
                    return Comparer.DefaultInvariant.Compare(val1, val2) <= 0;
                default:
                    throw new InvalidOperationException($"Invalid comparator: {comparator}");
            }
        }

        public static JObject SetData(JObject operations, JObject json)
        {
            float num = 0;
            float prevNum = 0;

            if (json[operations["field"].ToString()].Type == JTokenType.Integer || json[operations["field"].ToString()].Type == JTokenType.Float)
            {
                float.TryParse(operations["value"].ToString(), out num);
                float.TryParse(json[operations["field"].ToString()].ToString(), out prevNum);
            }

            switch (operations["operand"].ToString())
            {
                case "=":
                    if (json[operations["field"].ToString()].Type == JTokenType.Integer || json[operations["field"].ToString()].Type == JTokenType.Float)
                    {
                        json[operations["field"].ToString()] = num;
                    }
                    else
                    {
                        json[operations["field"].ToString()] = operations["value"].ToString();
                    }
                    return json;
                case "pow":
                    json[operations["field"].ToString()] = Math.Pow(prevNum, num);
                    return json;
                case "root":
                    json[operations["field"].ToString()] = Math.Pow(prevNum, 1.0 / num);
                    return json;
                case "+":
                    json[operations["field"].ToString()] = prevNum + num;
                    return json;
                case "-":
                    json[operations["field"].ToString()] = prevNum - num;
                    return json;
                case "/":
                    json[operations["field"].ToString()] = prevNum / num;
                    return json;
                case "*":
                    json[operations["field"].ToString()] = prevNum * num;
                    return json;
                case "%":
                    json[operations["field"].ToString()] = prevNum % num;
                    return json;
                default:
                    throw new InvalidOperationException($"Invalid opearation");
            }
        }

        public static List<object> GetValuesInJson(JToken token, string targetKey, string targetArray)
        {
            var values = new List<object>();

            void TraverseJson(JToken currentToken, bool insideTargetArray)
            {
                switch (currentToken.Type)
                {
                    case JTokenType.Object:
                        foreach (var property in currentToken.Children<JProperty>())
                        {
                            if (property.Name == targetArray)
                            {
                                TraverseJson(property.Value, true);  // Set insideTargetArray to true when targetArray is found
                            }
                            else if (property.Name == targetKey && insideTargetArray)
                            {
                                var valueToken = property.Value;
                                values.Add(valueToken.Value<object>());
                            }
                            else
                            {
                                TraverseJson(property.Value, insideTargetArray);
                            }
                        }
                        break;

                    case JTokenType.Array:
                        foreach (var item in currentToken.Children())
                        {
                            TraverseJson(item, insideTargetArray);  // Continue with same insideTargetArray value
                        }
                        break;

                    case JTokenType.Property:
                        var propertyValue = ((JProperty)currentToken).Value;
                        if (propertyValue is JArray nestedArray)
                        {
                            TraverseJson(nestedArray, insideTargetArray);  // Continue with same insideTargetArray value
                        }
                        else if (propertyValue is JObject nestedObject)
                        {
                            TraverseJson(nestedObject, insideTargetArray);  // Continue with same insideTargetArray value
                        }
                        break;

                    default:
                        break;
                }
            }
            if (targetArray == "") TraverseJson(token, true);
            else TraverseJson(token, false);

            return values;
        }

        public static string SetJsonId(string path, string relations)
        {
            FileStream fileStream = System.IO.File.Open(path, FileMode.OpenOrCreate);
            fileStream.Close();
            fileStream.Dispose();

            List<string> fileNames = System.IO.File.ReadAllLines(path).ToList();
            string randomStr = RandomString();

            while (fileNames.FindIndex(str => str.Contains(randomStr)) != -1)
            {
                randomStr = RandomString();
            }
            try
            {
                fileNames.Add(randomStr + relations);
                System.IO.File.WriteAllLines(path, fileNames);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return "fail";
            }

            return randomStr;
        }

        public static List<string> GetJsonData(List<JObject> relations, string path)
        {
            List<string> jsonData = new List<string>();
            List<string> fileNames = new List<string>();

            try
            {
                fileNames = System.IO.File.ReadAllLines(path + "/log.habo").ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return jsonData;
            }

            relations.ForEach(jObject =>
            {
                fileNames = fileNames.Where(s => s.Contains("*" + jObject["node"] + "*" + jObject["relation"] + "*" + jObject["way"])).ToList();
            });
            if (fileNames.Count() == 0) return jsonData;

            return fileNames;
        }
    }
}
