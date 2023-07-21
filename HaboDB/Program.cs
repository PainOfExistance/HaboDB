using Fleck;
using Fleck.Extended;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting.Server;
using System.Text;
using System.Net.WebSockets;
using System.Net.Sockets;

namespace HaboDB
{
    public class HaboDB
    {

        static void Main(string[] args)
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
                        ""value"": 60.0
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

            UserList tmp=new UserList();
            tmp.AddUser(new User("neke", "neke", "neke", "neke"));
            tmp.AddUser(new User("neke", "neke", "neke", "neke"));
            tmp.AddUser(new User("neke", "neke", "neke", "neke"));

            /*
            JObject jsonObject = JObject.Parse(jsonString);
            JObject relationsAndFieldsObject = JObject.Parse(relationsAndFields);
            JArray relationsObject = JArray.Parse(relations);

            WebSocketAbstraction abs = new WebSocketAbstraction("ws://192.168.0.22:5551");
            abs.Start();

            WebSocketServer ee = new WebSocketServer("ws://192.168.0.22:3000");
            ee.Start();

            LoadBalancer temp = new LoadBalancer("http://192.168.0.22:5552");
            Console.WriteLine("LoadBalancer running on http://192.168.0.22:5552");
            temp.Start();
            ClientWebSocket clientWebSocket = new ClientWebSocket();
            string k;
            int nm = 0;
            while (true)
            {
                k = Console.ReadLine();
                if (k != "k")
                {

                    using (HttpClient client = new HttpClient())
                    {
                        try
                        {
                            HttpContent message = new StringContent($"user1", Encoding.UTF8, "text/html");
                            HttpResponseMessage res = client.PostAsync("http://192.168.0.22:5552/user", message).Result;

                            string content = res.Content.ReadAsStringAsync().Result;
                            if (content != "")
                            {
                                Console.WriteLine(content);
                                string[] data = content.Split("||");
                                clientWebSocket.ConnectAsync(new Uri(data[1]), CancellationToken.None).Wait();
                                byte[] buffer = new byte[1000];
                                clientWebSocket.SendAsync(Encoding.UTF8.GetBytes(data[0]), WebSocketMessageType.Text, true, CancellationToken.None);

                                
                                WebSocketReceiveResult result = clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).Result;
                                string rs = Encoding.UTF8.GetString(buffer, 0, result.Count);
                                Console.WriteLine("woof: " + rs);
                                Array.Clear(buffer, 0, buffer.Length);
                            }
                            else
                            {
                                Console.WriteLine("Request failed with status code: " + res.StatusCode);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("An error occurred: " + ex.Message);
                        }
                    }
                }
                else
                {
                    if (nm>0)
                    {
                        relationsAndFields = @"{
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
                        ""value"": 70.0
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
                        nm = 0;

                    }
                    else
                    {
                        relationsAndFields = @"{
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
                        ""value"": 60.0
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
                        nm++;

                    }

                    byte[] buffer = new byte[1000];
                    clientWebSocket.SendAsync(Encoding.UTF8.GetBytes($"GetJson||E:/habodb/JsonIO/JsonIO/Utils/neke||neke||{relationsAndFields}"), WebSocketMessageType.Text, true, CancellationToken.None);
                    WebSocketReceiveResult result = clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).Result;
                    string rs = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine("meow: " + rs);

                }
            }
            */
            //Console.WriteLine(JsonIO.GetJson(relationsAndFieldsObject, "E:/habodb/JsonIO/JsonIO/Utils/neke", "neke"));
            //Console.WriteLine(JsonIO.GetJson(relationsAndFieldsObject, "E:/habodb/JsonIO/JsonIO/Utils/neke", "neke"));
            //Console.WriteLine(SaveJson(jsonObject, relationsObject, "E:/habodb/JsonIO/JsonIO/Utils/neke", "neke"));
            //Console.WriteLine(SetJson(relationsAndFieldsObject, "E:/habodb/JsonIO/JsonIO/Utils/neke", "neke"));
            //Console.WriteLine(DeleteJson(relationsAndFieldsObject, "E:/habodb/JsonIO/JsonIO/Utils/neke", "neke"));
        }



    }
}