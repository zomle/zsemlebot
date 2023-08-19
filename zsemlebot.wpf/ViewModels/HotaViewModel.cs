﻿using System;
using System.Windows.Input;
using zsemlebot.core;
using zsemlebot.core.Enums;
using zsemlebot.core.EventArgs;
using zsemlebot.services;

namespace zsemlebot.wpf.ViewModels
{
    public class HotaViewModel : ViewModelBase
    {
        private HotaStatus status;
        public HotaStatus Status
        {
            get { return status; }
            set
            {
                if (status != value)
                {
                    status = value;
                    OnPropertyChanged();
                }
            }
        }

        private string lastMessageReceivedAt;
        public string LastMessageReceivedAt
        {
            get { return lastMessageReceivedAt; }
            set
            {
                if (lastMessageReceivedAt != value)
                {
                    lastMessageReceivedAt = value;
                    OnPropertyChanged();
                }
            }
        }

        public int onlineUserCount;
        public int OnlineUserCount
        {
            get { return onlineUserCount; }
            set
            {
                if (onlineUserCount != value)
                {
                    onlineUserCount = value;
                    OnPropertyChanged();
                }
            }
        }

        private uint clientVersion;
        public uint ClientVersion
        {
            get { return clientVersion; }
            set
            {
                if (clientVersion != value)
                {
                    clientVersion = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ConnectCommand { get; }
        public ICommand ReconnectCommand { get; }
        public ICommand TestCommand { get; }

        private HotaService HotaService { get; }

        public HotaViewModel(HotaService hotaService)
        {
            lastMessageReceivedAt = "-";
            clientVersion = Config.Instance.Hota.ClientVersion;

            HotaService = hotaService;

            HotaService.MessageReceived += HotaService_MessageReceived;
            HotaService.StatusChanged += HotaService_StatusChanged;
            HotaService.UserListChanged += HotaService_UserListChanged;

            ConnectCommand = new CommandHandler(
                () => HotaService.Connect(),
                () => Status == HotaStatus.Initialized);

            ReconnectCommand = new CommandHandler(
                () => HotaService.Reconnect(),
                () => Status != HotaStatus.Initialized);

            TestCommand = new CommandHandler(
                () => HotaService.Test());
        }

        private void HotaService_UserListChanged(object? sender, HotaUserListChangedArgs e)
        {
            OnlineUserCount = e.OnlineUserCount;
        }

        private void HotaService_StatusChanged(object? sender, HotaStatusChangedArgs e)
        {
            Status = e.NewStatus;
        }

        private void HotaService_MessageReceived(object? sender, MessageReceivedArgs e)
        {
            LastMessageReceivedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
