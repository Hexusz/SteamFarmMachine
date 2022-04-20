using SteamAuth;

namespace SteamLibrary.Entities
{
    public class Account
    {
        public SteamGuardAccount SteamGuardAccount { get; set; }

        public string Password { get; set; }
    }
}