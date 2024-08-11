using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace chat.rede.webapi.Hubs
{
    public class ChatHub : Hub
    {
        private static ConcurrentQueue<string> WaitingQueue = new ConcurrentQueue<string>();
        private static HashSet<string> ConnectedUsers = new HashSet<string>();
        private const int MaxConnectedUsers = 2;

        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;

            if (ConnectedUsers.Count >= MaxConnectedUsers)
            {
                WaitingQueue.Enqueue(connectionId);
                await Clients.Client(connectionId).SendAsync("QueueUpdate", "Você está na fila. Por favor espere...", WaitingQueue.Count-1);
            }
            else
            {
                ConnectedUsers.Add(connectionId);
                await Clients.Client(connectionId).SendAsync("QueueUpdate", "Você está conectado.");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            if (ConnectedUsers.Contains(connectionId))
            {
                ConnectedUsers.Remove(connectionId);

                if (WaitingQueue.TryDequeue(out var nextConnectionId))
                {
                    ConnectedUsers.Add(nextConnectionId);
                    await Clients.Client(nextConnectionId).SendAsync("QueueUpdate", "Você está conectado.");
                }
            }
            else
            {
                WaitingQueue = new ConcurrentQueue<string>(WaitingQueue.Where(id => id != connectionId));
                await Clients.All.SendAsync("QueueUpdate", "Um usuário saiu da fila. Por favor espere...", WaitingQueue.Count-1);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string user, string message)
        {
            if (ConnectedUsers.Contains(Context.ConnectionId))
            {
                await Clients.All.SendAsync("ReceiveMessage", user, message);
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("QueueUpdate", "Você está na fila. Por favor espere...", WaitingQueue.Count-1);
            }
        }
    }
}
