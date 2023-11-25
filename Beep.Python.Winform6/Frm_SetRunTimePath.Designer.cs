namespace Beep.Python.Winform
{
    partial class Frm_SetRunTimePath
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ShowFileDialogbutton = new System.Windows.Forms.Button();
            this.RunTimePathtextBox = new System.Windows.Forms.TextBox();
            this.Savebutton = new System.Windows.Forms.Button();
            this.Resetbutton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ShowFileDialogbutton
            // 
            this.ShowFileDialogbutton.Location = new System.Drawing.Point(444, 10);
            this.ShowFileDialogbutton.Name = "ShowFileDialogbutton";
            this.ShowFileDialogbutton.Size = new System.Drawing.Size(75, 23);
            this.ShowFileDialogbutton.TabIndex = 0;
            this.ShowFileDialogbutton.Text = "Show  Dialog";
            this.ShowFileDialogbutton.UseVisualStyleBackColor = true;
            // 
            // RunTimePathtextBox
            // 
            this.RunTimePathtextBox.Location = new System.Drawing.Point(13, 12);
            this.RunTimePathtextBox.Name = "RunTimePathtextBox";
            this.RunTimePathtextBox.Size = new System.Drawing.Size(425, 20);
            this.RunTimePathtextBox.TabIndex = 1;
            // 
            // Savebutton
            // 
            this.Savebutton.Location = new System.Drawing.Point(201, 38);
            this.Savebutton.Name = "Savebutton";
            this.Savebutton.Size = new System.Drawing.Size(75, 23);
            this.Savebutton.TabIndex = 2;
            this.Savebutton.Text = "Save";
            this.Savebutton.UseVisualStyleBackColor = true;
            // 
            // Resetbutton
            // 
            this.Resetbutton.Location = new System.Drawing.Point(12, 38);
            this.Resetbutton.Name = "Resetbutton";
            this.Resetbutton.Size = new System.Drawing.Size(75, 23);
            this.Resetbutton.TabIndex = 3;
            this.Resetbutton.Text = "Reset";
            this.Resetbutton.UseVisualStyleBackColor = true;
            // 
            // Frm_SetRunTimePath
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(530, 69);
            this.Controls.Add(this.Resetbutton);
            this.Controls.Add(this.Savebutton);
            this.Controls.Add(this.RunTimePathtextBox);
            this.Controls.Add(this.ShowFileDialogbutton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Frm_SetRunTimePath";
            this.Text = "Set Python Runtime Path";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button ShowFileDialogbutton;
        private System.Windows.Forms.TextBox RunTimePathtextBox;
        private System.Windows.Forms.Button Savebutton;
        private System.Windows.Forms.Button Resetbutton;
    }
}