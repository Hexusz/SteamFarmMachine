using System;
using System.Windows.Forms;

namespace AccountCreator
{
    public partial class InputForm : Form
    {
        public bool Canceled = false;
        private bool userClosed = true;

        public InputForm(string label, bool password = false)
        {
            InitializeComponent();
            this.labelText.Text = label;

            if (password)
            {
                this.txtBox.PasswordChar = '*';
            }
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.txtBox.Text))
            {
                this.Canceled = true;
                this.userClosed = false;
                this.Close();
            }
            else
            {
                this.Canceled = false;
                this.userClosed = false;
                this.Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Canceled = true;
            this.userClosed = false;
            this.Close();
        }

        private void InputForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.userClosed)
            {
                // Set Canceled = true when the user hits the X button.
                this.Canceled = true;
            }
        }
    }
}
