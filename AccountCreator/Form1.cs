using Newtonsoft.Json;
using SteamAuth;
using SteamLibrary.Core;
using SteamLibrary.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Forms;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AccountCreator
{
    public partial class Form1 : Form
    {
        private int _idCurrentAccount;
        private string _saveFolder = "";
        private string _mafilesFolder = "";
        private int _maxTryCount=2;
        private static byte[] _key;

        private static byte[] _iv;

        public static LogWindow.ListBoxLog ListBoxLog;

        public List<SteamGuardAccount> Accounts = new List<SteamGuardAccount>();

        public Form1()
        {
            InitializeComponent();
            ListBoxLog = new LogWindow.ListBoxLog(listBox1);
        }

        private void pathButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            if (FBD.ShowDialog() == DialogResult.OK)
            {
                _saveFolder = FBD.SelectedPath;
                savePathLB.Text = new DirectoryInfo(FBD.SelectedPath).Name;
            }

            CheckFields();
        }

        private void SaveAccount()
        {
            string username = textBox1.Text;
            string password = textBox2.Text;

            var userLogin = new UserLogin(username, password);
            userLogin.TwoFactorCode =
                SteamFunc.GenerateSteamGuardCodeForTime(AppFunc.GetSystemUnixTime(), Accounts[_idCurrentAccount].SharedSecret);

            LoginResult response = LoginResult.BadCredentials;

            var tryCount = 0;

            while ((response = userLogin.DoLogin()) != LoginResult.LoginOkay)
            {
                tryCount++;
                if (tryCount > _maxTryCount)
                {
                    ListBoxLog.Log(LogWindow.Level.Warning, "Error: Failed logins, check account.");
                    return;
                }
                switch (response)
                {
                    case LoginResult.NeedEmail:
                        InputForm emailForm = new InputForm("Enter the code sent to your email:");
                        ListBoxLog.Log(LogWindow.Level.Warning, "Enter the code sent to your email:");
                        emailForm.ShowDialog();
                        if (emailForm.Canceled)
                        {
                            return;
                        }

                        userLogin.EmailCode = emailForm.txtBox.Text;
                        break;

                    case LoginResult.NeedCaptcha:
                        CaptchaForm captchaForm = new CaptchaForm(userLogin.CaptchaGID);
                        captchaForm.ShowDialog();
                        if (captchaForm.Canceled)
                        {
                            return;
                        }

                        userLogin.CaptchaText = captchaForm.CaptchaCode;
                        break;

                    case LoginResult.BadRSA:
                        ListBoxLog.Log(LogWindow.Level.Error, "Error: Steam returned \"BadRSA\"");
                        return;

                    case LoginResult.BadCredentials:
                        ListBoxLog.Log(LogWindow.Level.Error, "Error: Username or password was incorrect.");
                        return;

                    case LoginResult.TooManyFailedLogins:
                        ListBoxLog.Log(LogWindow.Level.Error, "Error: Too many failed logins, try again later.");
                        return;

                    case LoginResult.GeneralFailure:
                        ListBoxLog.Log(LogWindow.Level.Error, "Error: Steam returned \"GeneralFailure\".");
                        return;
                }
            }

            ListBoxLog.Log(LogWindow.Level.Success, "Login succeeded!");
            ListBoxLog.Log(LogWindow.Level.Success, $"Login: {username} SteamId: {userLogin.Session.SteamID}");

            FileStream fsWrite = new FileStream(Path.Combine(_saveFolder, Accounts[_idCurrentAccount].AccountName + ".acc"), FileMode.Create, FileAccess.Write);
            var serializeAccount = new Account();
            serializeAccount.Password = password;
            serializeAccount.SteamGuardAccount = Accounts[_idCurrentAccount];
            string serializeProfile = Newtonsoft.Json.JsonConvert.SerializeObject(serializeAccount);
            fsWrite.Write(EncryptStringToBytes(serializeProfile));
            fsWrite.Close();
            ListBoxLog.Log(LogWindow.Level.Success, $"Account {username} save to file");

            textBox1.Text = "";
            textBox2.Text = "";
            NextAccount();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" || textBox2.Text == "")
            {
                ListBoxLog.Log(LogWindow.Level.Warning, "Fill in the text fields");
                return;
            }

            SaveAccount();
        }

        private void CheckFields()
        {
            if (_key != null && _iv != null && _saveFolder.Length > 0 && _mafilesFolder.Length > 0)
            {
                panel1.Enabled = true;
            }
        }

        private void NextAccount()
        {
            if (Accounts.Count == 0) { return; }

            if (_idCurrentAccount < Accounts.Count - 1)
                _idCurrentAccount++;

            ListBoxLog.Log(LogWindow.Level.Info, $"Enter password for {Accounts[_idCurrentAccount].AccountName}");

            textBox1.Text = Accounts[_idCurrentAccount].AccountName;
        }

        private static byte[] EncryptStringToBytes(string profileText)
        {
            byte[] encryptedAuditTrail;

            using (Aes newAes = Aes.Create())
            {
                newAes.Key = _key;
                newAes.IV = _iv;

                ICryptoTransform encryptor = newAes.CreateEncryptor(_key, _iv);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(profileText);
                        }
                        encryptedAuditTrail = msEncrypt.ToArray();
                    }
                }
            }

            return encryptedAuditTrail;
        }

        private void loadKeyButton_Click(object sender, EventArgs e)
        {
            try
            {
                var opd = new OpenFileDialog();
                opd.Filter = "AccKey.key | AccKey.key";
                opd.ShowDialog();

                using FileStream fs = new FileStream(opd.FileName, FileMode.Open);
                var key = JsonSerializer.DeserializeAsync<AccKey>(fs);
                _key = key.Result.Key;
                _iv = key.Result.IV;
                keyLabel.Text = "Loaded";
                generateKeyButton.Enabled = false;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            CheckFields();
        }

        private void generateKeyButton_Click(object sender, EventArgs e)
        {
            using (RijndaelManaged myRijndael = new RijndaelManaged())
            {
                myRijndael.GenerateKey(); // this line generates key
                myRijndael.GenerateIV(); // this line generates initialization vektor

                var accKey = new AccKey()
                {
                    Key = myRijndael.Key,
                    IV = myRijndael.IV
                };
                var keyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "AccKey.key");

                string serializeProfile = Newtonsoft.Json.JsonConvert.SerializeObject(accKey);

                File.WriteAllText(keyPath, serializeProfile);

                _key = myRijndael.Key;
                _iv = myRijndael.IV;
                generateKeyButton.Enabled = false;
                keyLabel.Text = "Loaded";
                ListBoxLog.Log(LogWindow.Level.Warning, "Key in the program folder");
            }

            CheckFields();
        }

        private void mafileFolderButton_Click(object sender, EventArgs e)
        {
            var mafiles = new List<string>();
            using (var fldrDlg = new FolderBrowserDialog())
            {
                if (fldrDlg.ShowDialog() == DialogResult.OK)
                {
                    _mafilesFolder = fldrDlg.SelectedPath;
                    mafileLabel.Text = new DirectoryInfo(_mafilesFolder).Name;
                }
                else
                {
                    return;
                }
            }

            mafiles.AddRange(Directory.GetFiles(_mafilesFolder, "*.maFile", SearchOption.TopDirectoryOnly));

            foreach (var entry in mafiles)
            {
                string fileText = File.ReadAllText(entry);

                var account = JsonConvert.DeserializeObject<SteamAuth.SteamGuardAccount>(fileText);
                if (account == null) continue;
                Accounts.Add(account);
            }

            textBox1.Text = Accounts[_idCurrentAccount].AccountName;
            CheckFields();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ListBoxLog.Log(LogWindow.Level.Info, "Please select all folders");
        }

        private void skipButton_Click(object sender, EventArgs e)
        {
            ListBoxLog.Log(LogWindow.Level.Info, "Skip account");
            NextAccount();
        }
    }
}
