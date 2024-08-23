namespace chat.rede.webapi.models
{
    public class ResponseMessage
    {
        public string Message { get; set; } = string.Empty;
        public int QueuePosition { get; set; } = 0;
        public int QueueSize { get; set; } = 0;
        public string userName { get; set; } = string.Empty;
        public bool exitQueue { get; set; } = false;
        public bool isConnected { get; set; } = false;
    }
}
