using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace zsemlebot.wpf.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        protected void InvokeOnUI(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
