using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HaboDB
{
    public class WebSocketServer
    {
        private readonly Fleck.WebSocketServer server;
        private readonly TaskManager taskManager;
        private UserList userList = new UserList();
        public WebSocketServer(string url)
        {
            server = new Fleck.WebSocketServer(url);
            taskManager = new TaskManager();
        }

        public void Start()
        {
            server.Start(socket =>
            {
                socket.OnOpen = () => OnSocketOpen(socket);
                socket.OnClose = () => OnSocketClose(socket);
                socket.OnMessage = message => OnSocketMessage(socket, message);
            });
        }

        public void Stop()
        {
            server.Dispose();
        }

        private async void OnSocketOpen(IWebSocketConnection socket)
        {
            var userId = socket.ConnectionInfo.Id.ToString();
            Console.WriteLine($"Client connected: {userId}");
            await taskManager.RegisterUser(socket, userId);
        }

        private void OnSocketClose(IWebSocketConnection socket)
        {
            var userId = socket.ConnectionInfo.Id.ToString();
            Console.WriteLine($"Client disconnected: {userId}");

            taskManager.UnregisterUser(userId);
        }

        private void OnSocketMessage(IWebSocketConnection socket, string message)
        {
            var userId = socket.ConnectionInfo.Id.ToString();
            string[] data = message.Split("||");
            if (data[0] == "mtdt")
            {
                User tmp = JsonConvert.DeserializeObject<User>(data[1]);
                userList.AddUser(tmp);
            }
            else
            {
                if (userList.CheckHash(data[0]))
                {
                    Task.Run(async () =>
                    {
                        await taskManager.ProcessFunction(socket, userId, data[1]);
                    });
                }
                else
                {
                    Console.WriteLine("You are tresspassing");
                }
               
            }
            Console.WriteLine($"Client sent a message: {userId}");
        }
    }
}
