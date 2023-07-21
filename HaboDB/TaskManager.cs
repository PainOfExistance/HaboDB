using Fleck;
using Microsoft.AspNetCore.Hosting.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace HaboDB
{
    internal class TaskManager
    {
        private ConcurrentDictionary<string, SemaphoreSlim> folderAndContainerLocks;
        private ConcurrentDictionary<string, SemaphoreSlim> userLocks;
        public TaskManager()
        {
            folderAndContainerLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
            userLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        public async Task RegisterUser(IWebSocketConnection socket, string userId)
        {
            await socket.Send("You are now registered.");
            userLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        }

        public void UnregisterUser(string userId)
        {
            if (userLocks.TryRemove(userId, out var userLock))
            {
                userLock.Release();
            }
        }

        public async Task ProcessFunction(IWebSocketConnection socket, string userId, string message)
        {
            var parts = message.Split("||");
            if (parts.Length < 4)
            {
                await socket.Send($"Invalid message format {parts.Length}.");
                return;
            }

            var functionName = parts[0];
            var folderName = parts[1];
            var containerName = parts[2];

            var lockKey = GetLockKey(folderName, containerName);

            if (!userLocks.TryGetValue(userId, out var userLock))
            {
                await socket.Send("You are not registered or do not have access to this function.");
                return;
            }

            await userLock.WaitAsync();

            if (folderAndContainerLocks.TryGetValue(lockKey, out var folderAndContainerLock))
            {
                await socket.Send("Resource is currently locked.");
                userLock.Release();
                return;
            }

            folderAndContainerLock = folderAndContainerLocks.GetOrAdd(lockKey, new SemaphoreSlim(1, 1));
            await folderAndContainerLock.WaitAsync();

            try
            {
                Tuple<string, int> res = JsonIO.GetResources(parts).ToTuple();
                await socket.Send(res.Item2.ToString()+"||"+res.Item1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                folderAndContainerLocks.TryRemove(lockKey, out _);
                folderAndContainerLock.Release();

                userLock.Release();
            }
        }

        private string GetLockKey(string folderName, string containerName)
        {
            return $"{folderName}:{containerName}";
        }
    }
}
