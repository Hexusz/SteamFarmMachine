using SteamAuth;

namespace AccountCreator
{
    public class Account
    {
        public SteamGuardAccount SteamGuardAccount { get; set; }

        public string Password { get; set; }
    }
}