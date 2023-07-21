using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;

namespace HaboDB
{
    internal class JsonIO
    {
        static public int SaveJson(JObject json, JArray relations, string db, string container)
        {
            if (!Directory.Exists(db) || db == "") return 2;
            string path;

            try
            {
                if (!Directory.Exists(db + "/" + container) && container != "")
                {
                    path = db + "/" + container;
                    Directory.CreateDirectory(path);
                }
                else if (container == "")
                {
                    path = db + "/" + Utils.RandomString();
                    Directory.CreateDirectory(path);
                }
                else
                {
                    path = db + "/" + container;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 1;
            }

            try
            {
                string relationsString = "";
                relations.Cast<JObject>().ToList().ForEach(jObject =>
                {
                    relationsString += "*";
                    relationsString += jObject["node"].ToString();
                    relationsString += "*";
                    relationsString += jObject["relation"].ToString();
                    relationsString += "*";
                    relationsString += jObject["way"].ToString();
                });

                string jsonName = Utils.SetJsonId(path + "/log.habo", relationsString);
                if (jsonName == "fail") return 3;
                json.Add("id", jsonName);
                string jsonString = JsonConvert.SerializeObject(json);
                File.WriteAllText(path + "/" + jsonName + ".json", jsonString);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 4;
            }

            return 0;
        }

        static public(string, int) GetJson(JObject RelationsAndFields, string db, string container)
        {
            JArray tmp = new JArray();
            List<JObject> relations = new List<JObject>();

            if (!Directory.Exists(db) || db == "") return (tmp.ToString(), 2);

            if (!Directory.Exists(db + "/" + container) || container == "") return (tmp.ToString(), 3);

            try
            {
                RelationsAndFields["Relations"].Cast<JObject>().ToList().ForEach(jObject =>
                {
                    relations.Add(jObject);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                return (JsonConvert.SerializeObject(tmp), 3);
            }

            List<string> fileNames = Utils.GetJsonData(relations, db + "/" + container);
            if (fileNames.Count() == 0)
            {
                return (JsonConvert.SerializeObject(tmp), 4);
            }

            try
            {
                fileNames.ForEach(files =>
                {
                    string json = File.ReadAllText(db + "/" + container + "/" + files.Split("*").First() + ".json");
                    JObject jsonObject = JObject.Parse(json);


                    RelationsAndFields["Fields"].Cast<JObject>().ToList().ForEach(jObject =>
                    {
                        List<string> fields = Utils.ProccessField(jObject["field"].ToString());
                        Utils.GetValuesInJson(jsonObject, fields[0], fields[1]).ForEach(value =>
                        {
                            if (Utils.Operate(jObject["operand"].ToString(), value, jObject["value"]))
                            {
                                tmp.Add(jsonObject);
                            }
                        });
                    });
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            return (JsonConvert.SerializeObject(tmp), 0);
        }

        static public int SetJson(JObject RelationsAndFields, string db, string container)
        {

            List<JObject> relations = new List<JObject>();
            List<JObject> tmp = new List<JObject>();

            if (!Directory.Exists(db) || db == "") return 2;

            if (!Directory.Exists(db + "/" + container) || container == "") return 3;

            try
            {
                RelationsAndFields["Relations"].Cast<JObject>().ToList().ForEach(jObject =>
                {
                    relations.Add(jObject);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                return 3;
            }

            List<string> fileNames = Utils.GetJsonData(relations, db + "/" + container);
            List<string> fileNamesToWrite = new List<string>();
            if (fileNames.Count() == 0)
            {
                return 4;
            }

            try
            {
                fileNames.ForEach(files =>
                {
                    string json = File.ReadAllText(db + "/" + container + "/" + files.Split("*").First() + ".json");
                    JObject jsonObject = JObject.Parse(json);


                    RelationsAndFields["Querry"].Cast<JObject>().ToList().ForEach(jObject =>
                    {
                        List<string> fields = Utils.ProccessField(jObject["field"].ToString());
                        Utils.GetValuesInJson(jsonObject, fields[0], fields[1]).ForEach(value =>
                        {
                            if (Utils.Operate(jObject["operand"].ToString(), value, jObject["value"]))
                            {
                                tmp.Add(jsonObject);
                                fileNamesToWrite.Add(db + "/" + container + "/" + files.Split("*").First() + ".json");
                            }
                        });
                    });
                });

                RelationsAndFields["Fields"].Cast<JObject>().ToList().ForEach(jObject =>
                {
                    tmp.ForEach(obj =>
                    {
                        File.WriteAllText(db + "/" + container + "/" + obj.GetValue("id") + ".json", Utils.SetData(jObject, obj).ToString());
                    });
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 5;
            }

            return 0;
        }

        static public int DeleteJson(JObject RelationsAndFields, string db, string container)
        {
            List<JObject> relations = new List<JObject>();
            bool operated = false;

            if (!Directory.Exists(db) || db == "") return 2;

            if (!Directory.Exists(db + "/" + container) || container == "") return 3;

            try
            {
                RelationsAndFields["Relations"].Cast<JObject>().ToList().ForEach(jObject =>
                {
                    relations.Add(jObject);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 3;
            }

            List<string> fileNames = Utils.GetJsonData(relations, db + "/" + container);
            if (fileNames.Count() == 0)
            {
                return 4;
            }

            try
            {
                List<string> deletedJsonNames = new List<string>();

                fileNames.ForEach(files =>
                {
                    string json = File.ReadAllText(db + "/" + container + "/" + files.Split("*").First() + ".json");
                    JObject jsonObject = JObject.Parse(json);

                    RelationsAndFields["Fields"].Cast<JObject>().ToList().ForEach(jObject =>
                    {
                        List<string> fields = Utils.ProccessField(jObject["field"].ToString());

                        Utils.GetValuesInJson(jsonObject, fields[0], fields[1]).ForEach(value =>
                        {
                            if (Utils.Operate(jObject["operand"].ToString(), value, jObject["value"]))
                            {
                                deletedJsonNames.Add(files);
                                operated = true;
                            }
                        });
                    });
                });

                if (operated)
                {
                    List<string> jsonNames = File.ReadAllLines(db + "/" + container + "/log.habo").ToList();
                    deletedJsonNames.ForEach(deletedJsonNames =>
                    {
                        jsonNames.Remove(deletedJsonNames);
                        File.Delete(db + "/" + container + "/" + deletedJsonNames.Split("*").First() + ".json");
                    });

                    File.WriteAllLines(db + "/" + container + "/log.habo", jsonNames);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 5;
            }

            return 0;
        }

        /*static void Main(string[] args)
        {

            string jsonString = @"{
                ""Name"": ""John"",
                ""Age"": 34,
                ""StateOfOrigin"": ""England"",
                ""Pets"": [
                    {""Type"": ""Cat"", ""Name"": ""MooMoo"", ""Age"": 3.4},
                    {""Type"": ""Squirrel"", ""Name"": ""Sandy"", ""Age"": 7}
                ],
                ""Books"": {
                    ""Fiction"": [
                        {""Title"": ""Harry Potter"", ""Author"": ""J.K. Rowling""},
                        {""Title"": ""The Great Gatsby"", ""Author"": ""F. Scott Fitzgerald""}
                ],
                    ""NonFiction"": [
                        {""Title"": ""Sapiens"", ""Author"": ""Yuval Noah Harari"", ""ee"": [
                            {""Type"": ""Cat"", ""Name"": ""dd"", ""Age"": 3.4},
                            {""Type"": ""Squirrel"", ""Name"": ""ee"", ""Age"": 7}
                ],},
                        {""Title"": ""Educated"", ""Author"": ""Tara Westover""}
                ]
            }
            }";

            var relations = @"[
                    {
                        ""node"": ""V61cVWIQc0yYOcbA3afLDeS"",
                        ""relation"": ""is parrent"",
                        ""way"": ""two way""
                    },
                    {
                        ""node"": ""jXljVC12Lrin5uM2FgdyHUX"",
                        ""relation"": ""is sibling"",
                        ""way"": ""to this""
                    }
                ]";

            var relationsAndFields = @"{
            ""Relations"": [
                    {
                        ""node"": ""V61cVWIQc0yYOcbA3afLDeS"",
                        ""relation"": ""is parrent"",
                        ""way"": ""two way""
                    }
                ],
            ""Fields"":  [
                    {
                        ""field"": ""Age"",
                        ""operand"": ""="",
                        ""value"": 60
                    }
                ],
            ""Querry"":  [
                    {
                        ""field"": ""Age"",
                        ""operand"": ""="",
                        ""value"": 50
                    }
                ]
        }";

            JObject jsonObject = JObject.Parse(jsonString);
            JArray relationsObject = JArray.Parse(relations);
            JObject relationsAndFieldsObject = JObject.Parse(relationsAndFields);
            //Console.WriteLine(SaveJson(jsonObject, relationsObject, "E:/habodb/JsonIO/JsonIO/Utils/neke", "neke"));
            //Console.WriteLine(GetJson(relationsAndFieldsObject, "E:/habodb/JsonIO/JsonIO/Utils/neke", "neke"));
            //Console.WriteLine(SetJson(relationsAndFieldsObject, "E:/habodb/JsonIO/JsonIO/Utils/neke", "neke"));
            //Console.WriteLine(DeleteJson(relationsAndFieldsObject, "E:/habodb/JsonIO/JsonIO/Utils/neke", "neke"));
        }*/

        static string OperateMessage(int res)
        {
            switch (res)
            {
                case 0:
                    return "Good";
                case 1:
                    return "Path problems";
                case 2:
                    return "Database problems";
                case 3:
                    return "Json name problems";
                case 4:
                    return "Json operations problems";
                case 5:
                    return "Json writting problems";
                default:
                    throw new NotImplementedException("Wrong error code, idk how you got here");
            }
        }

        static public (string, int) GetResources(string[] parts)
        {
            JObject jsonObject;
            JArray relationsObject;
            int res;
            string strRes;
            switch (parts[0]){
                case "SaveJson":
                    jsonObject = JObject.Parse(parts[3]);
                    relationsObject = JArray.Parse(parts[4]);
                    res = SaveJson(jsonObject, relationsObject, parts[1], parts[2]);
                    return (OperateMessage(res), res);
                case "GetJson":
                    jsonObject = JObject.Parse(parts[3]);
                    (strRes, res) = GetJson(jsonObject, parts[1], parts[2]);
                    return (strRes, res);
                case "SetJson":
                    jsonObject = JObject.Parse(parts[3]);
                    res = SetJson(jsonObject, parts[1], parts[2]);
                    return (OperateMessage(res), res);
                case "DeleteJson":
                    jsonObject = JObject.Parse(parts[3]);
                    res = DeleteJson(jsonObject, parts[1], parts[2]);
                    return (OperateMessage(res), res);
                default:
                    throw new InvalidOperationException("Invalid fucntion choice");
            }
        }
    }
}