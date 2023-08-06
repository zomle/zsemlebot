﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using zsemlebot.networklib;
using zsemlebot.core;
using zsemlebot.core.EventArgs;
using System.Linq;

namespace zsemlebot.twitch
{

    public class IrcClient : IDisposable
    {
        private readonly TimeSpan PingFrequency = TimeSpan.FromSeconds(30);
        private readonly TimeSpan RateLimitWindowSize = TimeSpan.FromSeconds(30);
        private readonly int RateLimitMaxMessageCount = 20;

        private EventHandler<MessageReceivedArgs>? messageReceived;
        public event EventHandler<MessageReceivedArgs> MessageReceived
        {
            add { messageReceived += value; }
            remove { messageReceived -= value; }
        }

        private EventHandler<StatusChangedArgs>? statusChanged;
        public event EventHandler<StatusChangedArgs> StatusChanged
        {
            add { statusChanged += value; }
            remove { statusChanged -= value; }
        }

        private TwitchStatus status;
        public TwitchStatus Status
        {
            get { return status; }
            private set
            {
                if (status != value)
                {
                    status = value;
                    statusChanged?.Invoke(this, new StatusChangedArgs(value.ToString()));
                }
            }
        }

        private Socket Socket { get; set; }
        private Thread ReadThread { get; set; }
        private Thread PingThread { get; set; }
        private Thread SendThread { get; set; }

        private Queue<Message> IncomingMessageQueue { get; set; }
        private TwitchRawLogger RawLogger { get; set; }
        private TwitchEventLogger EventLogger { get; set; }

        private DateTime lastMessageReceivedAt;
        private DateTime LastMessageReceivedAt
        {
            get { return lastMessageReceivedAt; }
            set
            {
                if (lastMessageReceivedAt != value)
                {
                    lastMessageReceivedAt = value;
                    messageReceived?.Invoke(this, new MessageReceivedArgs());
                }
            }
        }

        private DateTime LastPingSentAt { get; set; }

        private Queue<OutgoingMessage> OutgoingMessageQueue { get; set; }
        private DateTime RateLimitWindowStart { get; set; }
        private int MessagesSentInRateLimitWindow { get; set; }

        private static readonly object padlock = new object();

        public IrcClient()
        {
            IncomingMessageQueue = new Queue<Message>();
            OutgoingMessageQueue = new Queue<OutgoingMessage>();
            Status = TwitchStatus.Initialized;
        }

        public bool Connect()
        {
            var now = DateTime.Now;

            Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Status = TwitchStatus.Connecting;

            try
            {
                Socket.Connect("irc.chat.twitch.tv", 6667);

                RawLogger = new TwitchRawLogger(now);
                EventLogger = new TwitchEventLogger(now);

                SendThread = new Thread(SendThreadWorker);
                SendThread.Start();

                SendMessage("CAP REQ :twitch.tv/tags twitch.tv/commands");
                SendMessage($"PASS {Configuration.Instance.Twitch.OAuthToken}", "PASS ***");
                SendMessage($"NICK {Configuration.Instance.Twitch.User}");

                lastMessageReceivedAt = DateTime.Now;

                ReadThread = new Thread(ReadThreadWorker);
                ReadThread.Start();

                Status = TwitchStatus.Connected;
                return true;
            }
            catch (Exception ex)
            {
                Status = TwitchStatus.Disconnected;
                return false;
            }
        }

        public void SendRawCommand(string rawCommandText)
        {
            SendMessage(rawCommandText);
        }

        public void ReplayFile(string filePath)
        {
            var reader = new StringReader(File.ReadAllText(filePath));
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var message = ParseMessage(line);

                if (!HandleLowLevelMessage(message))
                {
                    lock (padlock)
                    {
                        IncomingMessageQueue.Enqueue(message);
                    }
                }
            }
        }

        public bool HasNewMessage()
        {
            lock (padlock)
            {
                return IncomingMessageQueue.Count > 0;
            }
        }

        public Message GetNextMessage()
        {
            lock (padlock)
            {
                if (IncomingMessageQueue.Count == 0)
                {
                    return null;
                }

                return IncomingMessageQueue.Dequeue();
            }
        }

        public void JoinChannel(string channel)
        {
            SendMessage($"JOIN {channel}");
            EventLogger.LogJoinChannel(channel);
        }

        public void PartChannel(string channel)
        {
            SendMessage($"PART {channel}");
            EventLogger.LogPartChannel(channel);
        }

        public void SendPrivMsg(string channel, string message)
        {
            SendMessage($"PRIVMSG {channel} :{message}");
            EventLogger.LogSentMsg(channel, message);
        }

        private void SendPing()
        {
            SendMessage($"PING {Configuration.Instance.Twitch.User}");
            EventLogger.LogPing();
        }

        private void SendMessage(string message, string logMessageOverride = null)
        {
            OutgoingMessageQueue.Enqueue(new OutgoingMessage(message, logMessageOverride));
        }

        private bool CanSendMessage()
        {
            if (DateTime.Now - RateLimitWindowStart > RateLimitWindowSize)
            {
                return true;
            }

            if (MessagesSentInRateLimitWindow < RateLimitMaxMessageCount)
            {
                return true;
            }

            return false;
        }

        private Message ParseMessage(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            string[] tokens;

            IEnumerable<Tag> tags = null;
            if (line[0] == '@')
            {
                tokens = line.Split(' ', 2);
                tags = ProcessTags(tokens[0]);
                line = tokens[1];
            }

            string source = null;
            if (line[0] == ':')
            {
                tokens = line.Split(' ', 2);
                source = tokens[0][1..];
                line = tokens[1];
            }

            tokens = line.Split(' ', 2);
            var command = tokens[0];
            var parameters = tokens.Length > 1 ? tokens[1] : string.Empty;

            var result = new Message(tags)
            {
                Source = source,
                Command = command,
                Params = parameters
            };
            return result;
        }

        private bool HandleLowLevelMessage(Message message)
        {
            switch (message.Command)
            {
                case "PING":
                    SendMessage($"PONG {message.Params}");
                    return true;
                case "PONG":
                    EventLogger.LogPong();
                    return true;

                case "CAP":
                case "001": //server join message
                case "002": //server join message
                case "003": //server join message
                case "004": //server join message
                case "375": //server join message
                case "372": //server join message
                    return true;

                case "376": //server join message
                    EventLogger.Connected();
                    StartPingThread();
                    return true;

                case "353": //names reply for channel
                case "366": //end of /names 
                    return true;

                case "PRIVMSG":
                    EventLogger.LogPrivMsg(message);
                    return false;

                default:
                    return false;
            }
        }

        private void StartPingThread()
        {
            PingThread = new Thread(PingThreadWorker);
            PingThread.Start();
        }

        private void PingThreadWorker()
        {
            try
            {
                while (true)
                {
                    var timeSinceLastMessage = DateTime.Now - LastMessageReceivedAt;
                    bool noMessagesForAWhile = timeSinceLastMessage > PingFrequency;
                    bool shouldPingAgain = LastMessageReceivedAt > LastPingSentAt || (DateTime.Now - LastPingSentAt > PingFrequency);

                    if (Status == TwitchStatus.Connected && noMessagesForAWhile && shouldPingAgain)
                    {
                        LastPingSentAt = DateTime.Now;
                        SendPing();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                Status = TwitchStatus.Disconnected;
            }
        }

        private void ReadThreadWorker()
        {
            try
            {
                var buffer = new CircularCharBuffer(1024 * 1024);

                while (true)
                {
                    if (Socket == null)
                    {
                        Status = TwitchStatus.Disconnected;
                        return;
                    }

                    if (!Socket.Connected)
                    {
                        Status = TwitchStatus.Disconnected;
                        return;
                    }

                    int availableData = Socket.Available;
                    if (availableData == 0)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    var tmpBuffer = new byte[65535];
                    while (Socket.Available > 0)
                    {
                        int read = Socket.Receive(tmpBuffer);
                        if (read > 0)
                        {
                            LastMessageReceivedAt = DateTime.Now;

                            var stringMessage = Encoding.UTF8.GetString(tmpBuffer, 0, read);
                            buffer.PushData(stringMessage);
                        }
                    }

                    while (buffer.TryReadLine(out var line))
                    {
                        RawLogger.WriteIncomingMessage(line);

                        var message = ParseMessage(line);

                        if (!HandleLowLevelMessage(message))
                        {
                            lock (padlock)
                            {
                                IncomingMessageQueue.Enqueue(message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Status = TwitchStatus.Disconnected;
            }
        }

        private void SendThreadWorker()
        {
            try
            {
                while (true)
                {
                    if (!CanSendMessage())
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    OutgoingMessage outgoingMessage;
                    bool gotNewMessage = false;
                    lock (padlock)
                    {
                        gotNewMessage = OutgoingMessageQueue.TryDequeue(out outgoingMessage);
                    }

                    if (!gotNewMessage)
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    try
                    {
                        var bytes = Encoding.UTF8.GetBytes($"{outgoingMessage.Message}\r\n");
                        int sent = 0;
                        while (sent != bytes.Length)
                        {
                            var tmp = Socket.Send(bytes, sent, bytes.Length - sent, SocketFlags.None);
                            sent += tmp;
                        }

                        if (DateTime.Now - RateLimitWindowStart > RateLimitWindowSize)
                        {
                            RateLimitWindowStart = DateTime.Now;
                            MessagesSentInRateLimitWindow = 0;
                        }

                        MessagesSentInRateLimitWindow++;
                    }
                    catch (Exception e)
                    {
                        Status = TwitchStatus.Disconnected;
                    }
                    RawLogger.WriteOutgoingMessage(outgoingMessage.LogMessageOverride ?? outgoingMessage.Message);
                }
            }             
            catch (Exception ex)
            {
                Status = TwitchStatus.Disconnected;
            }            
        }

        private IEnumerable<Tag> ProcessTags(string tags)
        {
            var tokens = tags[1..].Split(';');
            foreach (var token in tokens)
            {
                var keyValue = token.Split('=', 2);
                yield return new Tag
                {
                    Key = keyValue[0],
                    Value = keyValue[1]
                };
            }
        }

        #region IDisposable implementation
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SafeDispose(Socket);
                    Socket = null;

                    SafeDispose(RawLogger);
                    RawLogger = null;

                    SafeDispose(EventLogger);
                    EventLogger = null;

                    SafeAbort(SendThread); 
                    SendThread = null;

                    SafeAbort(PingThread); 
                    PingThread = null;

                    SafeAbort(ReadThread); 
                    ReadThread = null;
                }

                disposedValue = true;
            }
        }

        private void SafeDispose(IDisposable disposable)
        {
            try
            {
                disposable?.Dispose();
            }
            catch { }
        }

        private void SafeAbort(Thread thread)
        {
            try
            {
                thread?.Abort();
            }
            catch { }            
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
