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
        private static byte[] _key;

        private static byte[] _iv;

        public static void LoadCryptKey()
        {
            var keyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "AccKey.key");
            if (File.Exists(keyPath))
            {
                using FileStream fs = new FileStream(keyPath, FileMode.OpenOrCreate);
                var accKey = JsonSerializer.DeserializeAsync<AccKey>(fs);
                _key = accKey.Result.Key;
                _iv = accKey.Result.IV;
                NLogger.Log.Info("Файл ключа загружен");
            }
            else
            {
                NLogger.Log.Fatal("Файл ключа не обнаружен, завершение программы");
                Environment.Exit(0);
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
            NLogger.Log.Info("Загрузка аккаунтов завершена");
            return Task.FromResult(acc);
        }

        private static string DecryptStringFromBytes(byte[] profileText)
        {
            string decryptText;

            using (Aes newAes = Aes.Create())
            {
                newAes.Key = _key;
                newAes.IV = _iv;

                ICryptoTransform decryptor = newAes.CreateDecryptor(_key, _iv);

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