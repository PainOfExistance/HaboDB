using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Fleck;

namespace HaboDB
{
    public class WebSocketAbstraction
    {
        private Fleck.WebSocketServer server;
        private Dictionary<IWebSocketConnection, string> connections = new Dictionary<IWebSocketConnection, string>();
        private Dictionary<string, ClientWebSocket> dataConnections = new Dictionary<string, ClientWebSocket>();
        private System.Timers.Timer timer;

        public WebSocketAbstraction(string url)
        {
            server = new Fleck.WebSocketServer(url);
        }

        public void Start()
        {
            server.Start(socket =>
            {
                socket.OnOpen = () => OnSocketOpen(socket);
                socket.OnClose = () => OnSocketClose(socket);
                socket.OnMessage = message => OnSocketMessage(socket, message);
            });

            timer = new System.Timers.Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
            timer.Elapsed += async (sender, e) => await SendLoad();
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
            server.Dispose();
        }

        private void OnSocketOpen(IWebSocketConnection socket)
        {
            Console.WriteLine("Client connected.");
            connections.Add(socket, "");
        }

        private void OnSocketClose(IWebSocketConnection socket)
        {
            Console.WriteLine("Client disconnected.");
            connections.Remove(socket);
        }

        private async void OnSocketMessage(IWebSocketConnection socket, string message)
        {
            Console.WriteLine($"Received message: {message}");
            if (connections[socket] == "")
            {
                connections[socket] = message;
                ClientWebSocket tmp = new ClientWebSocket();
                dataConnections.Add(message, tmp);
                await dataConnections[connections[socket]].ConnectAsync(new Uri(message), CancellationToken.None);

                byte[] receiveBuffer = new byte[1024];
                WebSocketReceiveResult result;
                MemoryStream receivedData = new MemoryStream();
                do
                {
                    result = await dataConnections[connections[socket]].ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                    receivedData.Write(receiveBuffer, 0, result.Count);
                } while (!result.EndOfMessage);
                string data = Encoding.UTF8.GetString(receivedData.ToArray());

                Console.WriteLine("Received data: " + data);
                await socket.Send(data);
            }
            else
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await dataConnections[connections[socket]].SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);

                WebSocketReceiveResult result;
                MemoryStream receivedData = new MemoryStream();
                do
                {
                    result = await dataConnections[connections[socket]].ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    receivedData.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);
                string data = Encoding.UTF8.GetString(receivedData.ToArray());

                Console.WriteLine("Received data: " + data);
                await socket.Send(data);
            }
        }

        async Task SendLoad()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = "http://192.168.0.22:5552/ws";
                    HttpContent message = new StringContent($"wsabs||{server.Location}||{connections.Count()}", Encoding.UTF8, "text/html");
                    HttpResponseMessage res = await client.PostAsync(url, message);

                    string content = await res.Content.ReadAsStringAsync();
                    if (content == "ok")
                    {
                        Console.WriteLine(content);
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
    }
}
