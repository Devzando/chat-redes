using chat.rede.webapi.models;
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
                var response = new ResponseMessage
                {
                    Message = "Você está na fila. Por favor espere...",
                    QueueSize = WaitingQueue.Count,
                    QueuePosition = GetQueuePosition(connectionId)
                };
                await Clients.Client(connectionId).SendAsync("QueueUpdate", response);
            }
            else
            {
                ConnectedUsers.Add(connectionId);
                var response = new ResponseMessage
                {
                    Message = "Você está conectado.",
                    QueueSize = WaitingQueue.Count,
                    QueuePosition = 0
                };
                await Clients.Client(connectionId).SendAsync("connected", response);
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
                    var response = new ResponseMessage
                    {
                        Message = "Você está conectado.",
                        QueueSize = WaitingQueue.Count,
                        QueuePosition = 0
                    };
                    await Clients.Client(nextConnectionId).SendAsync("QueueUpdate", response);
                }
            }
            else
            {
                WaitingQueue = new ConcurrentQueue<string>(WaitingQueue.Where(id => id != connectionId));
            }

            await NotifyQueueStatus();

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string user, string message)
        {
            if (ConnectedUsers.Contains(Context.ConnectionId))
            {
                var response = new ResponseMessage
                {
                    Message = message,
                    QueueSize = WaitingQueue.Count,
                    QueuePosition = 0,
                    userName = user
                };
                await Clients.Client(ConnectedUsers.First(id => id != Context.ConnectionId)).SendAsync("ReceiveMessage", response);
            }
            else
            {
                var response = new ResponseMessage
                {
                    Message = "Você está na fila. Por favor espere...",
                    QueueSize = WaitingQueue.Count,
                    QueuePosition = GetQueuePosition(Context.ConnectionId)
                };
                await Clients.Client(Context.ConnectionId).SendAsync("QueueUpdate", response);
            }
        }

        private async Task NotifyQueueStatus()
        {
            var response = new ResponseMessage
            {
                Message = "Você está na fila. Por favor espere...",
                QueueSize = WaitingQueue.Count,
                QueuePosition = 0,
                exitQueue = true
            };
            var queuePosition = 0;
            foreach (var connectionId in WaitingQueue)
            {
                response.QueuePosition = queuePosition;
                await Clients.Client(connectionId).SendAsync("QueueUpdate", response);
                queuePosition++;
            }
        }
        private int GetQueuePosition(string connectionId)
        {
            var position = 0;
            foreach (var id in WaitingQueue)
            {
                if (id == connectionId)
                {
                    return position;
                }
                position++;
            }
            return -1; // Retorna -1 se o connectionId não for encontrado, o que não deveria acontecer
        }
    }
}
