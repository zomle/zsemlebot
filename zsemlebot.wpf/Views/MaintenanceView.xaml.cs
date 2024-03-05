using Microsoft.Win32;
using System;
using System.Windows.Controls;
using zsemlebot.wpf.ViewModels;

namespace zsemlebot.wpf.Views
{
	/// <summary>
	/// Interaction logic for MaintenanceView.xaml
	/// </summary>
	public partial class MaintenanceView : UserControl
    {
        public MaintenanceView()
        {
            InitializeComponent();
        }

		private void BrowseZsemlebotDatabase_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog()
			{
				DefaultExt = ".db"
			};
			var result = dialog.ShowDialog();
			if (result ?? false)
			{
				var vm = DataContext as MaintenanceViewModel;
				if (vm == null)
				{
					throw new InvalidOperationException("DataContext is not assigned");
				}

				vm.Zsemlebot1DatabasePath = dialog.FileName;
			}			
        }
    }
}
