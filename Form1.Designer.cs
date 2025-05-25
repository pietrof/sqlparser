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
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // richTextBox1
            // 
            richTextBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBox1.Location = new Point(12, 149);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(866, 289);
            richTextBox1.TabIndex = 0;
            richTextBox1.Text = "";
            // 
            // button1
            // 
            button1.Location = new Point(12, 13);
            button1.Name = "button1";
            button1.Size = new Size(112, 34);
            button1.TabIndex = 1;
            button1.Text = "Analyse";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // cmbInputSourceField
            // 
            cmbInputSourceField.FormattingEnabled = true;
            cmbInputSourceField.Location = new Point(10, 14);
            cmbInputSourceField.Name = "cmbInputSourceField";
            cmbInputSourceField.Size = new Size(182, 33);
            cmbInputSourceField.TabIndex = 2;
            // 
            // cmbInputSourceTable
            // 
            cmbInputSourceTable.FormattingEnabled = true;
            cmbInputSourceTable.Location = new Point(219, 14);
            cmbInputSourceTable.Name = "cmbInputSourceTable";
            cmbInputSourceTable.Size = new Size(182, 33);
            cmbInputSourceTable.TabIndex = 3;
            // 
            // cmbInputDestField
            // 
            cmbInputDestField.FormattingEnabled = true;
            cmbInputDestField.Location = new Point(14, 14);
            cmbInputDestField.Name = "cmbInputDestField";
            cmbInputDestField.Size = new Size(182, 33);
            cmbInputDestField.TabIndex = 4;
            // 
            // cmbInputDestTable
            // 
            cmbInputDestTable.FormattingEnabled = true;
            cmbInputDestTable.Location = new Point(211, 14);
            cmbInputDestTable.Name = "cmbInputDestTable";
            cmbInputDestTable.Size = new Size(182, 33);
            cmbInputDestTable.TabIndex = 5;
            // 
            // panel1
            // 
            panel1.Controls.Add(cmbInputSourceField);
            panel1.Controls.Add(cmbInputSourceTable);
            panel1.Location = new Point(12, 65);
            panel1.Name = "panel1";
            panel1.Size = new Size(417, 59);
            panel1.TabIndex = 6;
            // 
            // panel2
            // 
            panel2.Controls.Add(cmbInputDestField);
            panel2.Controls.Add(cmbInputDestTable);
            panel2.Location = new Point(476, 65);
            panel2.Name = "panel2";
            panel2.Size = new Size(407, 59);
            panel2.TabIndex = 7;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(432, 82);
            label1.Name = "label1";
            label1.Size = new Size(38, 25);
            label1.TabIndex = 8;
            label1.Text = "-->";
            // 
            // button2
            // 
            button2.Location = new Point(144, 12);
            button2.Name = "button2";
            button2.Size = new Size(112, 34);
            button2.TabIndex = 9;
            button2.Text = "Find query";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(891, 450);
            Controls.Add(button2);
            Controls.Add(label1);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(button1);
            Controls.Add(richTextBox1);
            Name = "Form1";
            Text = "Form1";
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
    }
}
