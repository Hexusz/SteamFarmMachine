using System;

namespace BananaShooterFarm.Entities
{
    public class LastAccountStatus
    {
        public string Account { get; set; }
        public AccountStatus LastStatus { get; set; }
    }
}