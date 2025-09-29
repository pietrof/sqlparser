namespace sqlparsergui
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
            richTextBox1 = new RichTextBox();
            button1 = new Button();
            cmbInputSourceField = new ComboBox();
            cmbInputSourceTable = new ComboBox();
            cmbInputDestField = new ComboBox();
            cmbInputDestTable = new ComboBox();
            panel1 = new Panel();
            panel2 = new Panel();
            label1 = new Label();
            button2 = new Button();
            button3 = new Button();
            textBox1 = new TextBox();
            label2 = new Label();
            folderBrowserDialog1 = new FolderBrowserDialog();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // richTextBox1
            // 
            richTextBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBox1.Location = new Point(20, 244);
            richTextBox1.Margin = new Padding(5);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(1957, 471);
            richTextBox1.TabIndex = 0;
            richTextBox1.Text = "";
            richTextBox1.TextChanged += richTextBox1_TextChanged;
            // 
            // button1
            // 
            button1.Location = new Point(20, 21);
            button1.Margin = new Padding(5);
            button1.Name = "button1";
            button1.Size = new Size(190, 56);
            button1.TabIndex = 1;
            button1.Text = "Analyse";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // cmbInputSourceField
            // 
            cmbInputSourceField.FormattingEnabled = true;
            cmbInputSourceField.Location = new Point(17, 23);
            cmbInputSourceField.Margin = new Padding(5);
            cmbInputSourceField.Name = "cmbInputSourceField";
            cmbInputSourceField.Size = new Size(307, 49);
            cmbInputSourceField.TabIndex = 2;
            // 
            // cmbInputSourceTable
            // 
            cmbInputSourceTable.FormattingEnabled = true;
            cmbInputSourceTable.Location = new Point(372, 23);
            cmbInputSourceTable.Margin = new Padding(5);
            cmbInputSourceTable.Name = "cmbInputSourceTable";
            cmbInputSourceTable.Size = new Size(307, 49);
            cmbInputSourceTable.TabIndex = 3;
            // 
            // cmbInputDestField
            // 
            cmbInputDestField.FormattingEnabled = true;
            cmbInputDestField.Location = new Point(24, 23);
            cmbInputDestField.Margin = new Padding(5);
            cmbInputDestField.Name = "cmbInputDestField";
            cmbInputDestField.Size = new Size(762, 49);
            cmbInputDestField.TabIndex = 4;
            cmbInputDestField.SelectedIndexChanged += cmbInputDestField_SelectedIndexChanged_1;
            // 
            // cmbInputDestTable
            // 
            cmbInputDestTable.FormattingEnabled = true;
            cmbInputDestTable.Location = new Point(869, 23);
            cmbInputDestTable.Margin = new Padding(5);
            cmbInputDestTable.Name = "cmbInputDestTable";
            cmbInputDestTable.Size = new Size(307, 49);
            cmbInputDestTable.TabIndex = 5;
            // 
            // panel1
            // 
            panel1.Controls.Add(cmbInputSourceField);
            panel1.Controls.Add(cmbInputSourceTable);
            panel1.Location = new Point(20, 107);
            panel1.Margin = new Padding(5);
            panel1.Name = "panel1";
            panel1.Size = new Size(709, 97);
            panel1.TabIndex = 6;
            // 
            // panel2
            // 
            panel2.Controls.Add(cmbInputDestField);
            panel2.Controls.Add(cmbInputDestTable);
            panel2.Location = new Point(809, 107);
            panel2.Margin = new Padding(5);
            panel2.Name = "panel2";
            panel2.Size = new Size(1190, 112);
            panel2.TabIndex = 7;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(734, 134);
            label1.Margin = new Padding(5, 0, 5, 0);
            label1.Name = "label1";
            label1.Size = new Size(63, 41);
            label1.TabIndex = 8;
            label1.Text = "-->";
            // 
            // button2
            // 
            button2.Location = new Point(245, 20);
            button2.Margin = new Padding(5);
            button2.Name = "button2";
            button2.Size = new Size(190, 56);
            button2.TabIndex = 9;
            button2.Text = "Find query";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.Location = new Point(483, 21);
            button3.Margin = new Padding(5);
            button3.Name = "button3";
            button3.Size = new Size(233, 61);
            button3.TabIndex = 10;
            button3.Text = "Jump to file";
            button3.UseVisualStyleBackColor = true;
            button3.MouseClick += button3_MouseClick;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(893, 35);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(739, 47);
            textBox1.TabIndex = 11;
            textBox1.Text = "@..\\..\\..\\..\\sqlparsergui\\sql\\";
            textBox1.Click += textBox1_Click;
            textBox1.TextChanged += textBox1_TextChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(811, 36);
            label2.Name = "label2";
            label2.Size = new Size(76, 41);
            label2.TabIndex = 12;
            label2.Text = "Path";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(17F, 41F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(2003, 738);
            Controls.Add(label2);
            Controls.Add(textBox1);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(label1);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(button1);
            Controls.Add(richTextBox1);
            Margin = new Padding(5);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            panel1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private RichTextBox richTextBox1;
        private Button button1;
        private ComboBox cmbInputSourceField;
        private ComboBox cmbInputSourceTable;
        private ComboBox cmbInputDestField;
        private ComboBox cmbInputDestTable;
        private Panel panel1;
        private Panel panel2;
        private Label label1;
        private Button button2;
        private Button button3;
        private TextBox textBox1;
        private Label label2;
        private FolderBrowserDialog folderBrowserDialog1;
    }
}
