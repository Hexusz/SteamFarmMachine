using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using DSTvmFarm.Entities;
using NLog.Fluent;

namespace DSTvmFarm.Core
{
    public static class AppFunc
    {
        private readonly static byte[] Key = Convert.FromBase64String("q9OwdZag1163OJqwjVAsIovXfSWG98m+sPSxwJecfe4=");

        private readonly static byte[] IV = Convert.FromBase64String("htJhjtmZs1Aq0UTbuyWXrw==");

        public static async Task<AppConfig> LoadConfig()
        {
            var cfgPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DSTvmFarm.conf");
            if (File.Exists(cfgPath))
            {
                using (FileStream fs = new FileStream(cfgPath, FileMode.OpenOrCreate))
                {
                    AppConfig? conf = await JsonSerializer.DeserializeAsync<AppConfig>(fs);
                    return conf;
                }
            }
            else
            {
                using (FileStream fs = new FileStream("DSTvmFarm.conf", FileMode.OpenOrCreate))
                {
                    var defaulConfig = new AppConfig()
                    {
                        SteamPath = @"C:\Program Files (x86)\Steam",
                        VirtualInputMethod = 0
                    };

                    await JsonSerializer.SerializeAsync<AppConfig>(fs, defaulConfig);

                    return defaulConfig;
                }
            }
        }

        public static async Task<List<Account>> LoadAccounts()
        {
            var acc = new List<Account>();
            try
            {
                var accPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Accounts");
                var accFiles = Directory.GetFiles(accPath, "*.acc");
                
                foreach (var accFile in accFiles)
                {
                    FileStream fsRead = new FileStream(accFile, FileMode.Open, FileAccess.Read);
                    BinaryReader br = new BinaryReader(fsRead);
                    long numBytes = new FileInfo(accFile).Length;
                    string decryptedText = DecryptStringFromBytes(br.ReadBytes((int)numBytes));
                    Account deserializeAccount = Newtonsoft.Json.JsonConvert.DeserializeObject<Account>(decryptedText);
                    acc.Add(deserializeAccount);
                    fsRead.Close();
                }
            }
            catch (Exception ex)
            {
                NLogger.Log.Fatal("Ошибка загрузки аккаунта из файла");
            }
            return acc;
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

    }
}