using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BananaShooterFarm.Entities
{
    public class AccountStats : INotifyPropertyChanged
    {
        private string account;
        public string Account
        {
            get => account;
            set { account = value; NotifyPropertyChanged(); }
        }
        private AccountStatus status;
        public AccountStatus Status
        {
            get => status;
            set { status = value; NotifyPropertyChanged(); }
        }
        private int items;
        public int Items
        {
            get => items;
            set { items = value; NotifyPropertyChanged(); }
        }
        private int pid;
        public int PID
        {
            get => pid;
            set { pid = value; NotifyPropertyChanged(); }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}