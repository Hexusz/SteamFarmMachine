using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DSTvmFarm.Entities;
using Newtonsoft.Json;
using NLog.Fluent;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DSTvmFarm.Core
{
    public static class AppFunc
    {
        private static readonly byte[] Key = Convert.FromBase64String("q9OwdZag1163OJqwjVAsIovXfSWG98m+sPSxwJecfe4=");

        private static readonly byte[] IV = Convert.FromBase64String("htJhjtmZs1Aq0UTbuyWXrw==");

        public static async Task<AppConfig> LoadConfig()
        {
            var cfgPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DSTvmFarm.conf");
            if (File.Exists(cfgPath))
            {
                await using FileStream fs = new FileStream(cfgPath, FileMode.OpenOrCreate);
                var conf = await JsonSerializer.DeserializeAsync<AppConfig>(fs);
                return conf;
            }
            else
            {
                await using FileStream fs = new FileStream("DSTvmFarm.conf", FileMode.OpenOrCreate);
                var defaultConfig = new AppConfig()
                {
                    SteamPath = @"C:\Program Files (x86)\Steam",
                    VirtualInputMethod = 0
                };

                await JsonSerializer.SerializeAsync<AppConfig>(fs, defaultConfig);

                return defaultConfig;
            }
        }

        public static Task<List<Account>> LoadAccounts()
        {
            var acc = new List<Account>();
            try
            {
                var accPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Accounts");
                var accFiles = Directory.GetFiles(accPath, "*.acc");

                foreach (var accFile in accFiles)
                {
                    var fsRead = new FileStream(accFile, FileMode.Open, FileAccess.Read);
                    var br = new BinaryReader(fsRead);
                    var numBytes = new FileInfo(accFile).Length;
                    var decryptedText = DecryptStringFromBytes(br.ReadBytes((int)numBytes));
                    var deserializeAccount = JsonConvert.DeserializeObject<Account>(decryptedText);
                    acc.Add(deserializeAccount);
                    fsRead.Close();
                }
            }
            catch (Exception ex)
            {
                NLogger.Log.Fatal("Ошибка загрузки аккаунта из файла " + ex.Message);
            }
            return Task.FromResult(acc);
        }

        private static string DecryptStringFromBytes(byte[] profileText)
        {
            string decryptText;

            using (Aes newAes = Aes.Create())
            {
                newAes.Key = Key;
                newAes.IV = IV;

                ICryptoTransform decryptor = newAes.CreateDecryptor(Key, IV);

                using (MemoryStream msDecrypt = new MemoryStream(profileText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            decryptText = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return decryptText;
        }

        public static long GetSystemUnixTime()
        {
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}