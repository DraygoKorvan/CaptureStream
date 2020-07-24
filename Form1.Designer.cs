namespace CaptureStream
{
	partial class CaptureStreamForm
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
			this.button1 = new System.Windows.Forms.Button();
			this.text_posY = new System.Windows.Forms.TextBox();
			this.text_SizeX = new System.Windows.Forms.TextBox();
			this.text_SizeY = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.text_ResX = new System.Windows.Forms.TextBox();
			this.text_ResY = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.text_posX = new System.Windows.Forms.TextBox();
			this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
			this.text_encoding = new System.Windows.Forms.Label();
			this.Preview = new System.Windows.Forms.PictureBox();
			this.ImageUpdater = new System.ComponentModel.BackgroundWorker();
			this.FrameTimeMonitor = new System.Windows.Forms.Label();
			this.AudioLength = new System.Windows.Forms.Label();
			this.RecordingText = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.Preview)).BeginInit();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(35, 389);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 0;
			this.button1.Text = "Record";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.RecordOnClick);
			// 
			// text_posY
			// 
			this.text_posY.Location = new System.Drawing.Point(35, 348);
			this.text_posY.Name = "text_posY";
			this.text_posY.Size = new System.Drawing.Size(100, 20);
			this.text_posY.TabIndex = 1;
			this.text_posY.TextChanged += new System.EventHandler(this.UpdatePosY);
			// 
			// text_SizeX
			// 
			this.text_SizeX.Location = new System.Drawing.Point(161, 322);
			this.text_SizeX.Name = "text_SizeX";
			this.text_SizeX.Size = new System.Drawing.Size(100, 20);
			this.text_SizeX.TabIndex = 3;
			this.text_SizeX.TextChanged += new System.EventHandler(this.UpdateSizeX);
			// 
			// text_SizeY
			// 
			this.text_SizeY.Location = new System.Drawing.Point(161, 348);
			this.text_SizeY.Name = "text_SizeY";
			this.text_SizeY.Size = new System.Drawing.Size(100, 20);
			this.text_SizeY.TabIndex = 4;
			this.text_SizeY.TextChanged += new System.EventHandler(this.UpdateSizeY);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 329);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(14, 13);
			this.label1.TabIndex = 6;
			this.label1.Text = "X";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 355);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(14, 13);
			this.label2.TabIndex = 7;
			this.label2.Text = "Y";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(74, 303);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(44, 13);
			this.label3.TabIndex = 8;
			this.label3.Text = "Position";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(205, 302);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(27, 13);
			this.label4.TabIndex = 9;
			this.label4.Text = "Size";
			// 
			// text_ResX
			// 
			this.text_ResX.Location = new System.Drawing.Point(292, 322);
			this.text_ResX.Name = "text_ResX";
			this.text_ResX.Size = new System.Drawing.Size(100, 20);
			this.text_ResX.TabIndex = 10;
			this.text_ResX.TextChanged += new System.EventHandler(this.UpdateResX);
			// 
			// text_ResY
			// 
			this.text_ResY.Location = new System.Drawing.Point(292, 348);
			this.text_ResY.Name = "text_ResY";
			this.text_ResY.Size = new System.Drawing.Size(100, 20);
			this.text_ResY.TabIndex = 11;
			this.text_ResY.TextChanged += new System.EventHandler(this.UpdateResY);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(328, 301);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(57, 13);
			this.label5.TabIndex = 12;
			this.label5.Text = "Resolution";
			// 
			// text_posX
			// 
			this.text_posX.Location = new System.Drawing.Point(35, 322);
			this.text_posX.Name = "text_posX";
			this.text_posX.Size = new System.Drawing.Size(100, 20);
			this.text_posX.TabIndex = 2;
			this.text_posX.Text = "0";
			this.text_posX.TextChanged += new System.EventHandler(this.UpdatePosX);
			// 
			// backgroundWorker
			// 
			this.backgroundWorker.WorkerReportsProgress = true;
			this.backgroundWorker.WorkerSupportsCancellation = true;
			this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackGroundWorkerDoWork);
			// 
			// text_encoding
			// 
			this.text_encoding.AutoSize = true;
			this.text_encoding.Location = new System.Drawing.Point(538, 301);
			this.text_encoding.Name = "text_encoding";
			this.text_encoding.Size = new System.Drawing.Size(52, 13);
			this.text_encoding.TabIndex = 13;
			this.text_encoding.Text = "Encoding";
			// 
			// Preview
			// 
			this.Preview.Location = new System.Drawing.Point(362, 23);
			this.Preview.Name = "Preview";
			this.Preview.Size = new System.Drawing.Size(426, 240);
			this.Preview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.Preview.TabIndex = 14;
			this.Preview.TabStop = false;
			this.Preview.Click += new System.EventHandler(this.pictureBox1_Click);
			// 
			// ImageUpdater
			// 
			this.ImageUpdater.DoWork += new System.ComponentModel.DoWorkEventHandler(this.UpdateImageDoWork);
			// 
			// FrameTimeMonitor
			// 
			this.FrameTimeMonitor.AutoSize = true;
			this.FrameTimeMonitor.Location = new System.Drawing.Point(754, 270);
			this.FrameTimeMonitor.Name = "FrameTimeMonitor";
			this.FrameTimeMonitor.Size = new System.Drawing.Size(22, 13);
			this.FrameTimeMonitor.TabIndex = 15;
			this.FrameTimeMonitor.Text = "rec";
			// 
			// AudioLength
			// 
			this.AudioLength.AutoSize = true;
			this.AudioLength.Location = new System.Drawing.Point(681, 270);
			this.AudioLength.Name = "AudioLength";
			this.AudioLength.Size = new System.Drawing.Size(39, 13);
			this.AudioLength.TabIndex = 16;
			this.AudioLength.Text = "audlen";
			// 
			// RecordingText
			// 
			this.RecordingText.AutoSize = true;
			this.RecordingText.Location = new System.Drawing.Point(132, 394);
			this.RecordingText.Name = "RecordingText";
			this.RecordingText.Size = new System.Drawing.Size(99, 13);
			this.RecordingText.TabIndex = 17;
			this.RecordingText.Text = "Recording Stopped";
			// 
			// CaptureStreamForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.RecordingText);
			this.Controls.Add(this.AudioLength);
			this.Controls.Add(this.FrameTimeMonitor);
			this.Controls.Add(this.Preview);
			this.Controls.Add(this.text_encoding);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.text_ResY);
			this.Controls.Add(this.text_ResX);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.text_SizeY);
			this.Controls.Add(this.text_SizeX);
			this.Controls.Add(this.text_posX);
			this.Controls.Add(this.text_posY);
			this.Controls.Add(this.button1);
			this.Name = "CaptureStreamForm";
			this.Text = "Capture Stream";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.onClosing);
			this.Load += new System.EventHandler(this.CaptureStreamForm_Load);
			((System.ComponentModel.ISupportInitialize)(this.Preview)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.TextBox text_posY;
		private System.Windows.Forms.TextBox text_posX;
		private System.Windows.Forms.TextBox text_SizeX;
		private System.Windows.Forms.TextBox text_SizeY;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox text_ResX;
		private System.Windows.Forms.TextBox text_ResY;
		private System.Windows.Forms.Label label5;
		internal System.ComponentModel.BackgroundWorker backgroundWorker;
		private System.Windows.Forms.Label text_encoding;
		private System.Windows.Forms.PictureBox Preview;
		private System.ComponentModel.BackgroundWorker ImageUpdater;
		private System.Windows.Forms.Label FrameTimeMonitor;
		private System.Windows.Forms.Label AudioLength;
		private System.Windows.Forms.Label RecordingText;
	}
}

