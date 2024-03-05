using System;
using System.Windows.Input;
using zsemlebot.core;
using zsemlebot.core.Enums;
using zsemlebot.core.EventArgs;
using zsemlebot.services;

namespace zsemlebot.wpf.ViewModels
{
	public class MaintenanceViewModel : ViewModelBase
	{
		private string zsemlebot1DatabasePath;
		public string Zsemlebot1DatabasePath
		{
			get { return zsemlebot1DatabasePath; }
			set
			{
				if (zsemlebot1DatabasePath != value)
				{
					zsemlebot1DatabasePath = value;
					OnPropertyChanged();
				}
			}
		}

		public ICommand MigrateUsersCommand { get; }

		private BotService BotService { get; }

		public MaintenanceViewModel(BotService botService)
		{
			BotService = botService;

			MigrateUsersCommand = new CommandHandler(
				() =>
				{
					BotService.LoadUserData(Zsemlebot1DatabasePath);
				});
		}
	}
}
