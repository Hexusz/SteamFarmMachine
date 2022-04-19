using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;

namespace AccountCreator
{
    public partial class Form1 : Form
    {
        private string savePath = "";

        private static byte[] _key;

        private static byte[] _iv;

        public Form1()
        {
            InitializeComponent();
        }

        private void pathButton_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            if (FBD.ShowDialog() == DialogResult.OK)
            {
                savePath = FBD.SelectedPath;
                savePathLB.Text = "Selected";
            }

            CheckFields();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (savePath == "" || !textBox3.Text.Contains("=") || textBox1.Text == "" || textBox2.Text == "" || textBox4.Text == "")
            {
                MessageBox.Show("Try again");
            }

            var acc = new Account()
            {
                Name = textBox1.Text,
                Password = textBox2.Text,
                SharedSecret = textBox3.Text,
                SteamId = textBox4.Text
            };

            FileStream fsWrite = new FileStream(Path.Combine(savePath, acc.Name + ".acc"), FileMode.Create, FileAccess.Write);
            string serializeProfile = Newtonsoft.Json.JsonConvert.SerializeObject(acc);
            fsWrite.Write(EncryptStringToBytes(serializeProfile));
            fsWrite.Close();
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
        }

        private void CheckFields()
        {
            if (_key != null && _iv != null && savePath.Length > 0)
            {
                panel1.Enabled = true;
            }
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
                keyLabel.Text = "Key loaded";
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
                keyLabel.Text = "Key loaded";
            }

            CheckFields();
        }
    }
}
