using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using zsemlebot.networklib;
using zsemlebot.core;
using zsemlebot.core.EventArgs;
using zsemlebot.core.Enums;
using zsemlebot.twitch.Log;

namespace zsemlebot.twitch
{
	public class IrcClient : IDisposable
	{
		private readonly TimeSpan PingFrequency = TimeSpan.FromSeconds(30);
		private readonly TimeSpan MessageRateLimitWindowSize = TimeSpan.FromSeconds(30);
		private readonly int MessageRateLimitMaxMessageCount = 20;
		private readonly TimeSpan JoinRateLimitWindowSize = TimeSpan.FromSeconds(10);
		private readonly int JoinRateLimitMaxMessageCount = 20;

		#region Events
		private EventHandler<MessageReceivedArgs>? messageReceived;
		public event EventHandler<MessageReceivedArgs> MessageReceived
		{
			add { messageReceived += value; }
			remove { messageReceived -= value; }
		}

		private EventHandler<TwitchStatusChangedArgs>? statusChanged;
		public event EventHandler<TwitchStatusChangedArgs> StatusChanged
		{
			add { statusChanged += value; }
			remove { statusChanged -= value; }
		}
		#endregion

		private TwitchStatus status;
		public TwitchStatus Status
		{
			get { return status; }
			private set
			{
				if (status != value)
				{
					status = value;
					statusChanged?.Invoke(this, new TwitchStatusChangedArgs(value));
				}
			}
		}

		private Socket? Socket { get; set; }
		private Thread? ReadThread { get; set; }
		private Thread? PingThread { get; set; }
		private Thread? SendThread { get; set; }

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
		private DateTime MessageRateLimitWindowStart { get; set; }
		private int MessagesSentInRateLimitWindow { get; set; }

		private Queue<OutgoingMessage> JoinChannelQueue { get; set; }
		private DateTime JoinRateLimitWindowStart { get; set; }
		private int JoinsSentInRateLimitWindow { get; set; }


		private static readonly object padlock = new object();

		public IrcClient()
		{
			IncomingMessageQueue = new Queue<Message>();
			OutgoingMessageQueue = new Queue<OutgoingMessage>();
			JoinChannelQueue =new Queue<OutgoingMessage>();

			Status = TwitchStatus.Initialized;

			RawLogger = TwitchRawLogger.Null;
			EventLogger = TwitchEventLogger.Null;
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

				lock (padlock)
				{
					IncomingMessageQueue.Clear();
				}

				OutgoingMessageQueue.Clear();
				JoinChannelQueue.Clear();

				SendThread = new Thread(SendThreadWorker);
				SendThread.Start();

				Status = TwitchStatus.Connected;

				SendMessage("CAP REQ :twitch.tv/tags twitch.tv/commands");
				SendMessage($"PASS {Config.Instance.Twitch.OAuthToken}", "PASS ***");
				SendMessage($"NICK {Config.Instance.Twitch.User}");

				lastMessageReceivedAt = DateTime.Now;

				ReadThread = new Thread(ReadThreadWorker);
				ReadThread.Start();

				return true;
			}
			catch (Exception)
			{
				Status = TwitchStatus.Disconnected;
				return false;
			}
		}

		public void SendRawCommand(string rawCommandText)
		{
			SendMessage(rawCommandText);
		}

		public bool HasNewMessage()
		{
			lock (padlock)
			{
				return IncomingMessageQueue.Count > 0;
			}
		}

		public Message? GetNextMessage()
		{
			lock (padlock)
			{
				IncomingMessageQueue.TryDequeue(out var result);
				return result;
			}
		}

		public void JoinChannel(string channel)
		{
			if (!channel.StartsWith('#'))
			{
				throw new ArgumentException("Channel must start with a '#'", nameof(channel));
			}

			lock (padlock)
			{
				JoinChannelQueue.Enqueue(new OutgoingMessage($"JOIN {channel}", null));
			}
			EventLogger.LogJoinChannel(channel);
		}

		public void PartChannel(string channel)
		{
			if (!channel.StartsWith('#'))
			{
				throw new ArgumentException("Channel must start with a '#'", nameof(channel));
			}

			SendMessage($"PART {channel}");
			EventLogger.LogPartChannel(channel);
		}

		public void SendPrivMsg(string channel, string message)
		{
			SendMessage($"PRIVMSG {channel} :{message}");
			EventLogger.LogSentMsg(channel, message);
		}

		public void SendPrivMsg(string parentMessageId, string channel, string message)
		{
			SendMessage($"@reply-parent-msg-id={parentMessageId} PRIVMSG {channel} :{message}");
			EventLogger.LogSentMsg(channel, message);
		}

		private void SendPing()
		{
			SendMessage($"PING {Config.Instance.Twitch.User}");
			EventLogger.LogPing();
		}

		private void SendMessage(string message, string? logMessageOverride = null)
		{
			lock (padlock)
			{
				OutgoingMessageQueue.Enqueue(new OutgoingMessage(message, logMessageOverride));
			}
		}

		private bool CanSendJoin()
		{
			if (DateTime.Now - JoinRateLimitWindowStart > JoinRateLimitWindowSize)
			{
				return true;
			}

			if (JoinsSentInRateLimitWindow < JoinRateLimitMaxMessageCount)
			{
				return true;
			}

			return false;
		}

		private bool CanSendMessage()
		{
			if (DateTime.Now - MessageRateLimitWindowStart > MessageRateLimitWindowSize)
			{
				return true;
			}

			if (MessagesSentInRateLimitWindow < MessageRateLimitMaxMessageCount)
			{
				return true;
			}

			return false;
		}

		private static Message? ParseMessage(string line)
		{
			if (string.IsNullOrEmpty(line))
			{
				return null;
			}

			string[] tokens;

			IReadOnlyDictionary<string, Tag>? tags = null;
			if (line[0] == '@')
			{
				tokens = line.Split(' ', 2);
				tags = ProcessTags(tokens[0]);
				line = tokens[1];
			}

			string? source = null;
			if (line[0] == ':')
			{
				tokens = line.Split(' ', 2);
				source = tokens[0][1..];
				line = tokens[1];
			}

			tokens = line.Split(' ', 2);
			var command = tokens[0];
			var parameters = tokens.Length > 1 ? tokens[1] : string.Empty;

			var result = new Message(source, command, parameters, tags ?? new Dictionary<string, Tag>());
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
					Status = TwitchStatus.Authenticated;
					EventLogger.Connected();
					StartPingThread();
					return true;

				case "353": //names reply for channel
				case "366": //end of /names 
					return true;

				case "PRIVMSG":
					EventLogger.LogPrivMsg(message);
					return false;

				case "RECONNECT":
					try
					{
						if (Socket != null && Socket.Connected)
						{
							Socket.Disconnect(true);
						}
					}
					finally
					{
						Status = TwitchStatus.Disconnected;
					}
					EventLogger.ReconnectRequested();
					return true;

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

					if (Status == TwitchStatus.Authenticated && noMessagesForAWhile && shouldPingAgain)
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
			catch (Exception)
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
						if (line == null)
						{
							continue;
						}

						RawLogger.WriteIncomingMessage(line);

						var message = ParseMessage(line);

						if (message == null)
						{
							continue;
						}

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
			catch (Exception)
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
					if (JoinChannelQueue.Count > 0)
					{
						if (!TrySendJoin())
						{
							continue;
						}
					}

					if (OutgoingMessageQueue.Count > 0)
					{
						if (!TrySendMessage())
						{
							continue;
						}
						
					}
				}
			}
			catch (Exception)
			{
				Status = TwitchStatus.Disconnected;
			}
		}

		private bool TrySendJoin()
		{
			if (!CanSendJoin())
			{
				Thread.Sleep(500);
				return false;
			}

			OutgoingMessage? outgoingMessage;
			bool gotNewMessage = false;
			lock (padlock)
			{
				gotNewMessage = JoinChannelQueue.TryDequeue(out outgoingMessage);
			}

			if (gotNewMessage && outgoingMessage != null)
			{
				try
				{
					SendSocketRaw(outgoingMessage);

					if (DateTime.Now - JoinRateLimitWindowStart > JoinRateLimitWindowSize)
					{
						JoinRateLimitWindowStart = DateTime.Now;
						JoinsSentInRateLimitWindow = 0;
					}

					JoinsSentInRateLimitWindow++;
				}
				catch (Exception)
				{
					Status = TwitchStatus.Disconnected;
				}

				RawLogger.WriteOutgoingMessage(outgoingMessage.LogMessageOverride ?? outgoingMessage.Message);
			}

			return true;
		}

		private bool TrySendMessage()
		{
			if (!CanSendMessage())
			{
				Thread.Sleep(500);
				return false;
			}

			OutgoingMessage? outgoingMessage;
			bool gotNewMessage = false;
			lock (padlock)
			{
				gotNewMessage = OutgoingMessageQueue.TryDequeue(out outgoingMessage);
			}

			if (!gotNewMessage || outgoingMessage == null)
			{
				Thread.Sleep(500);
				return false;
			}

			try
			{
				SendSocketRaw(outgoingMessage);

				if (DateTime.Now - MessageRateLimitWindowStart > MessageRateLimitWindowSize)
				{
					MessageRateLimitWindowStart = DateTime.Now;
					MessagesSentInRateLimitWindow = 0;
				}

				MessagesSentInRateLimitWindow++;
			}
			catch (Exception)
			{
				Status = TwitchStatus.Disconnected;
			}
			RawLogger.WriteOutgoingMessage(outgoingMessage.LogMessageOverride ?? outgoingMessage.Message);
			return true;
		}

		private void SendSocketRaw(OutgoingMessage? outgoingMessage)
		{
			if (Socket == null || outgoingMessage == null)
			{
				return;
			}

			var bytes = Encoding.UTF8.GetBytes($"{outgoingMessage.Message}\r\n");
			int sent = 0;
			while (sent != bytes.Length)
			{
				var tmp = Socket.Send(bytes, sent, bytes.Length - sent, SocketFlags.None);
				sent += tmp;
			}
		}

		private static IReadOnlyDictionary<string, Tag> ProcessTags(string tags)
		{
			var result = new Dictionary<string, Tag>(20);

			var tokens = tags[1..].Split(';');
			foreach (var token in tokens)
			{
				var keyValue = token.Split('=', 2);
				var tag = new Tag(keyValue[0], keyValue[1]);
				result.Add(keyValue[0], tag);
			}

			return result;
		}

		#region IDisposable implementation
		private bool disposedValue;
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					SafeAbort(SendThread);
					SendThread = null;

					SafeAbort(PingThread);
					PingThread = null;

					SafeAbort(ReadThread);
					ReadThread = null;

					SafeDispose(Socket);
					Socket = null;

					SafeDispose(RawLogger);
					RawLogger = TwitchRawLogger.Null;

					SafeDispose(EventLogger);
					EventLogger = TwitchEventLogger.Null;
				}

				disposedValue = true;
			}
		}

		private static void SafeDispose(IDisposable? disposable)
		{
			try
			{
				disposable?.Dispose();
			}
			catch { }
		}

		private static void SafeAbort(Thread? thread)
		{
			try
			{
				thread?.Interrupt();
			}
			catch { }
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
