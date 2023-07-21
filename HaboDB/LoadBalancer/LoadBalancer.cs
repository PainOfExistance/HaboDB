using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fleck;
using System.Text;

namespace HaboDB
{
    internal class DataShardConnection
    {
        public int NumOfConnections = 0;
        public ClientWebSocket DataShard;
        public DataShardConnection(string connection)
        {
            DataShard = new ClientWebSocket();
            DataShard.ConnectAsync(new Uri(connection), CancellationToken.None);
        }

        public async void SendLoad(User data)
        {
            string message = JsonConvert.SerializeObject(data);
            byte[] buffer = Encoding.UTF8.GetBytes("mtdt"+message);
            await DataShard.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
    public class LoadBalancer
    {
        UserList users = new UserList();
        private static Dictionary<string, string> userDataMap = new Dictionary<string, string>
        {
            { "user1", "ws://192.168.0.22:3000" },
            { "user2", "datashard2.example.com" },
            { "user2", "datashard2.example.com" },
        };

        private static Dictionary<string, DataShardConnection> dataShardLoad = new Dictionary<string, DataShardConnection>
        {
            { "ws://192.168.0.22:3000", new DataShardConnection("ws://192.168.0.22:3000") }
        };

        private static Dictionary<string, int> wsAbstractionLoad = new Dictionary<string, int>
        {
            { "ws://192.168.0.22:5551", 0 }
        };

        private readonly IWebHost host;

        public LoadBalancer(string url)
        {
            host = new WebHostBuilder()
                .UseUrls(url)
                .UseKestrel()
                .Configure(ConfigureApp)
                .Build();
        }

        private void ConfigureApp(IApplicationBuilder app)
        {
            app.Map("/ws", HandleWsEndpoint);
            app.Map("/login", HandleUserLogin);
            app.Map("/register", HandleUserRegister);
        }

        private void HandleUserLogin(IApplicationBuilder app)
        {
            app.Run(async (context) =>
            {
                try
                {
                    string data = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    Console.WriteLine(data);
                    string response = "";
                    User user = users.GetUser(data);
                    if (!users.CheckCredentials(user.UserName, user.PasswordHash))
                    {
                        string userData = GetUserData(user.hash);
                        string wsAbstraction = GetLeastLoadedWSAbstraction();
                        response = $"{userData}||{wsAbstraction}";
                    }
                    else
                    {
                        response = "You are not suposed to be here";
                    }
                    await context.Response.WriteAsync(response);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    await context.Response.WriteAsync("Error");
                }
            });
        }

        private void HandleWsEndpoint(IApplicationBuilder app)
        {
            app.Run(async (context) =>
            {
                try
                {
                    string data = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    string[] strings = data.Split("||");
                    Console.WriteLine(data);
                    wsAbstractionLoad[strings[1]] = int.Parse(strings[2]);
                    await context.Response.WriteAsync("ok");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    await context.Response.WriteAsync("Error");
                }
            });
        }

        private void HandleUserRegister(IApplicationBuilder app)
        {
            app.Run(async (context) =>
            {
                try
                {
                    string data = await new StreamReader(context.Request.Body).ReadToEndAsync();
                    Console.WriteLine(data);
                    User tmp = JsonConvert.DeserializeObject<User>(data);
                    tmp.hash = Utils.RandomString() + Utils.RandomString();
                    KeyValuePair<string, DataShardConnection> DSLoad = dataShardLoad.OrderBy(kv => kv.Value).First();
                    dataShardLoad[DSLoad.Key].NumOfConnections = DSLoad.Value.NumOfConnections + 1;
                    users.AddUser(tmp);
                    userDataMap.Add(tmp.hash, DSLoad.Key);
                    dataShardLoad[DSLoad.Key].SendLoad(tmp);
                    await context.Response.WriteAsync(tmp.PasswordHash);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    await context.Response.WriteAsync("Error");
                }
            });
        }

        private string GetUserData(string userHash)
        {
            if (userDataMap.ContainsKey(userHash))
            {
                return userDataMap[userHash];
            }
            return "User data not found";
        }

        private string GetLeastLoadedWSAbstraction()
        {
            KeyValuePair<string, int> leastLoaded = wsAbstractionLoad.OrderBy(kv => kv.Value).First();
            wsAbstractionLoad[leastLoaded.Key]++;
            return leastLoaded.Key;
        }

        public void Start()
        {
            Task.Run(() => host.RunAsync());
            Console.WriteLine("Load Balancer stopped...");
        }

        public void Stop()
        {
            host.StopAsync().Wait();
            Console.WriteLine("Load Balancer stopped...");
        }

        public void AddWsAbstraction(string url)
        {
            wsAbstractionLoad.TryAdd(url, 0);
        }

        public void RemoveWSAbstraction(string url)
        {
            wsAbstractionLoad.Remove(url);
        }
    }
}
