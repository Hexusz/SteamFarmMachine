
namespace AccountCreator
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.pathButton = new System.Windows.Forms.Button();
            this.savePathLB = new System.Windows.Forms.Label();
            this.saveButton = new System.Windows.Forms.Button();
            this.loadKeyButton = new System.Windows.Forms.Button();
            this.generateKeyButton = new System.Windows.Forms.Button();
            this.keyLabel = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.mafileFolderButton = new System.Windows.Forms.Button();
            this.mafileLabel = new System.Windows.Forms.Label();
            this.skipButton = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(70, 7);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(122, 23);
            this.textBox1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Login";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "Password";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(70, 36);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(122, 23);
            this.textBox2.TabIndex = 2;
            // 
            // pathButton
            // 
            this.pathButton.Location = new System.Drawing.Point(220, 11);
            this.pathButton.Name = "pathButton";
            this.pathButton.Size = new System.Drawing.Size(94, 23);
            this.pathButton.TabIndex = 10;
            this.pathButton.Text = "Account folder";
            this.pathButton.UseVisualStyleBackColor = true;
            this.pathButton.Click += new System.EventHandler(this.pathButton_Click);
            // 
            // savePathLB
            // 
            this.savePathLB.AutoSize = true;
            this.savePathLB.Location = new System.Drawing.Point(320, 15);
            this.savePathLB.Name = "savePathLB";
            this.savePathLB.Size = new System.Drawing.Size(36, 15);
            this.savePathLB.TabIndex = 11;
            this.savePathLB.Text = "None";
            // 
            // saveButton
            // 
            this.saveButton.Location = new System.Drawing.Point(7, 65);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(112, 36);
            this.saveButton.TabIndex = 12;
            this.saveButton.Text = "Save account";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // loadKeyButton
            // 
            this.loadKeyButton.Location = new System.Drawing.Point(222, 69);
            this.loadKeyButton.Name = "loadKeyButton";
            this.loadKeyButton.Size = new System.Drawing.Size(94, 23);
            this.loadKeyButton.TabIndex = 13;
            this.loadKeyButton.Text = "Load Key";
            this.loadKeyButton.UseVisualStyleBackColor = true;
            this.loadKeyButton.Click += new System.EventHandler(this.loadKeyButton_Click);
            // 
            // generateKeyButton
            // 
            this.generateKeyButton.Location = new System.Drawing.Point(222, 98);
            this.generateKeyButton.Name = "generateKeyButton";
            this.generateKeyButton.Size = new System.Drawing.Size(143, 23);
            this.generateKeyButton.TabIndex = 14;
            this.generateKeyButton.Text = "Generate Key";
            this.generateKeyButton.UseVisualStyleBackColor = true;
            this.generateKeyButton.Click += new System.EventHandler(this.generateKeyButton_Click);
            // 
            // keyLabel
            // 
            this.keyLabel.AutoSize = true;
            this.keyLabel.Location = new System.Drawing.Point(320, 73);
            this.keyLabel.Name = "keyLabel";
            this.keyLabel.Size = new System.Drawing.Size(45, 15);
            this.keyLabel.TabIndex = 15;
            this.keyLabel.Text = "No Key";
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.skipButton);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.textBox1);
            this.panel1.Controls.Add(this.textBox2);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.saveButton);
            this.panel1.Enabled = false;
            this.panel1.Location = new System.Drawing.Point(12, 11);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(202, 110);
            this.panel1.TabIndex = 16;
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 15;
            this.listBox1.Location = new System.Drawing.Point(7, 133);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(360, 169);
            this.listBox1.TabIndex = 13;
            // 
            // mafileFolderButton
            // 
            this.mafileFolderButton.Location = new System.Drawing.Point(222, 40);
            this.mafileFolderButton.Name = "mafileFolderButton";
            this.mafileFolderButton.Size = new System.Drawing.Size(94, 23);
            this.mafileFolderButton.TabIndex = 17;
            this.mafileFolderButton.Text = "Mafiles folder";
            this.mafileFolderButton.UseVisualStyleBackColor = true;
            this.mafileFolderButton.Click += new System.EventHandler(this.mafileFolderButton_Click);
            // 
            // mafileLabel
            // 
            this.mafileLabel.AutoSize = true;
            this.mafileLabel.Location = new System.Drawing.Point(320, 44);
            this.mafileLabel.Name = "mafileLabel";
            this.mafileLabel.Size = new System.Drawing.Size(36, 15);
            this.mafileLabel.TabIndex = 18;
            this.mafileLabel.Text = "None";
            // 
            // skipButton
            // 
            this.skipButton.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.skipButton.Location = new System.Drawing.Point(125, 65);
            this.skipButton.Name = "skipButton";
            this.skipButton.Size = new System.Drawing.Size(67, 36);
            this.skipButton.TabIndex = 13;
            this.skipButton.Text = "Skip";
            this.skipButton.UseVisualStyleBackColor = true;
            this.skipButton.Click += new System.EventHandler(this.skipButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(377, 312);
            this.Controls.Add(this.mafileLabel);
            this.Controls.Add(this.mafileFolderButton);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.keyLabel);
            this.Controls.Add(this.generateKeyButton);
            this.Controls.Add(this.loadKeyButton);
            this.Controls.Add(this.savePathLB);
            this.Controls.Add(this.pathButton);
            this.Name = "Form1";
            this.Text = "Account Creator";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button pathButton;
        private System.Windows.Forms.Label savePathLB;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button loadKeyButton;
        private System.Windows.Forms.Button generateKeyButton;
        private System.Windows.Forms.Label keyLabel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button mafileFolderButton;
        private System.Windows.Forms.Label mafileLabel;
        private System.Windows.Forms.Button skipButton;
    }
}

