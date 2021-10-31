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
			this.RecordingButton = new System.Windows.Forms.Button();
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
			this.FrameRate_Text = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.Frame_Monitor = new System.Windows.Forms.Label();
			this.AA_Combobox = new System.Windows.Forms.ComboBox();
			this.Interpolation_Combobox = new System.Windows.Forms.ComboBox();
			this.AAText = new System.Windows.Forms.Label();
			this.IntrText = new System.Windows.Forms.Label();
			this.RightVolumeSlider = new System.Windows.Forms.VScrollBar();
			this.BalanceSlider = new System.Windows.Forms.HScrollBar();
			this.LeftVolumeSlider = new System.Windows.Forms.VScrollBar();
			this.leftVolumeMeter = new NAudio.Gui.VolumeMeter();
			this.rightVolumeMeter = new NAudio.Gui.VolumeMeter();
			this.LeftBalanceText = new System.Windows.Forms.Label();
			this.RightBalanceText = new System.Windows.Forms.Label();
			this.LeftVolumeText = new System.Windows.Forms.Label();
			this.RightVolumeText = new System.Windows.Forms.Label();
			this.DitherOnBox = new System.Windows.Forms.CheckBox();
			this.AudioDeviceChanger = new System.Windows.Forms.ComboBox();
			this.AudioDevice_Text = new System.Windows.Forms.Label();
			this.MuteCheckbox = new System.Windows.Forms.CheckBox();
			this.FrameMonitorText = new System.Windows.Forms.Label();
			this.toFileCheckbox = new System.Windows.Forms.CheckBox();
			this.saveVideoRecording = new System.Windows.Forms.SaveFileDialog();
			this.Save = new System.Windows.Forms.Button();
			this.SavingProgress = new System.Windows.Forms.ProgressBar();
			this.label7 = new System.Windows.Forms.Label();
			this.SampleRateTextBox = new System.Windows.Forms.TextBox();
			this.FileSaverBackground = new System.ComponentModel.BackgroundWorker();
			this.CopyToClipboard = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.Preview)).BeginInit();
			this.SuspendLayout();
			// 
			// RecordingButton
			// 
			this.RecordingButton.Location = new System.Drawing.Point(47, 471);
			this.RecordingButton.Margin = new System.Windows.Forms.Padding(4);
			this.RecordingButton.Name = "RecordingButton";
			this.RecordingButton.Size = new System.Drawing.Size(100, 28);
			this.RecordingButton.TabIndex = 0;
			this.RecordingButton.Text = "Record";
			this.RecordingButton.UseVisualStyleBackColor = true;
			this.RecordingButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.RecordOnClick);
			// 
			// text_posY
			// 
			this.text_posY.Location = new System.Drawing.Point(47, 428);
			this.text_posY.Margin = new System.Windows.Forms.Padding(4);
			this.text_posY.Name = "text_posY";
			this.text_posY.Size = new System.Drawing.Size(132, 22);
			this.text_posY.TabIndex = 1;
			this.text_posY.TextChanged += new System.EventHandler(this.UpdatePosY);
			// 
			// text_SizeX
			// 
			this.text_SizeX.Location = new System.Drawing.Point(215, 396);
			this.text_SizeX.Margin = new System.Windows.Forms.Padding(4);
			this.text_SizeX.Name = "text_SizeX";
			this.text_SizeX.Size = new System.Drawing.Size(132, 22);
			this.text_SizeX.TabIndex = 3;
			this.text_SizeX.TextChanged += new System.EventHandler(this.UpdateSizeX);
			// 
			// text_SizeY
			// 
			this.text_SizeY.Location = new System.Drawing.Point(215, 428);
			this.text_SizeY.Margin = new System.Windows.Forms.Padding(4);
			this.text_SizeY.Name = "text_SizeY";
			this.text_SizeY.Size = new System.Drawing.Size(132, 22);
			this.text_SizeY.TabIndex = 4;
			this.text_SizeY.TextChanged += new System.EventHandler(this.UpdateSizeY);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(16, 405);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(17, 17);
			this.label1.TabIndex = 6;
			this.label1.Text = "X";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(16, 437);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(17, 17);
			this.label2.TabIndex = 7;
			this.label2.Text = "Y";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(44, 375);
			this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(58, 17);
			this.label3.TabIndex = 8;
			this.label3.Text = "Position";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(211, 374);
			this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(35, 17);
			this.label4.TabIndex = 9;
			this.label4.Text = "Size";
			// 
			// text_ResX
			// 
			this.text_ResX.Location = new System.Drawing.Point(389, 396);
			this.text_ResX.Margin = new System.Windows.Forms.Padding(4);
			this.text_ResX.Name = "text_ResX";
			this.text_ResX.Size = new System.Drawing.Size(132, 22);
			this.text_ResX.TabIndex = 10;
			this.text_ResX.TextChanged += new System.EventHandler(this.UpdateResX);
			// 
			// text_ResY
			// 
			this.text_ResY.Location = new System.Drawing.Point(389, 428);
			this.text_ResY.Margin = new System.Windows.Forms.Padding(4);
			this.text_ResY.Name = "text_ResY";
			this.text_ResY.Size = new System.Drawing.Size(132, 22);
			this.text_ResY.TabIndex = 11;
			this.text_ResY.TextChanged += new System.EventHandler(this.UpdateResY);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(388, 374);
			this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(75, 17);
			this.label5.TabIndex = 12;
			this.label5.Text = "Resolution";
			// 
			// text_posX
			// 
			this.text_posX.Location = new System.Drawing.Point(47, 396);
			this.text_posX.Margin = new System.Windows.Forms.Padding(4);
			this.text_posX.Name = "text_posX";
			this.text_posX.Size = new System.Drawing.Size(132, 22);
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
			this.text_encoding.Location = new System.Drawing.Point(515, 314);
			this.text_encoding.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.text_encoding.Name = "text_encoding";
			this.text_encoding.Size = new System.Drawing.Size(67, 17);
			this.text_encoding.TabIndex = 13;
			this.text_encoding.Text = "Encoding";
			// 
			// Preview
			// 
			this.Preview.Location = new System.Drawing.Point(16, 15);
			this.Preview.Margin = new System.Windows.Forms.Padding(4);
			this.Preview.Name = "Preview";
			this.Preview.Size = new System.Drawing.Size(568, 295);
			this.Preview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.Preview.TabIndex = 14;
			this.Preview.TabStop = false;
			// 
			// ImageUpdater
			// 
			this.ImageUpdater.DoWork += new System.ComponentModel.DoWorkEventHandler(this.UpdateImageDoWork);
			// 
			// FrameTimeMonitor
			// 
			this.FrameTimeMonitor.AutoSize = true;
			this.FrameTimeMonitor.ForeColor = System.Drawing.SystemColors.ControlText;
			this.FrameTimeMonitor.Location = new System.Drawing.Point(85, 314);
			this.FrameTimeMonitor.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.FrameTimeMonitor.Name = "FrameTimeMonitor";
			this.FrameTimeMonitor.Size = new System.Drawing.Size(28, 17);
			this.FrameTimeMonitor.TabIndex = 15;
			this.FrameTimeMonitor.Text = "rec";
			// 
			// AudioLength
			// 
			this.AudioLength.AutoSize = true;
			this.AudioLength.Location = new System.Drawing.Point(455, 314);
			this.AudioLength.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.AudioLength.Name = "AudioLength";
			this.AudioLength.Size = new System.Drawing.Size(51, 17);
			this.AudioLength.TabIndex = 16;
			this.AudioLength.Text = "audlen";
			// 
			// RecordingText
			// 
			this.RecordingText.AutoSize = true;
			this.RecordingText.Location = new System.Drawing.Point(184, 479);
			this.RecordingText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.RecordingText.Name = "RecordingText";
			this.RecordingText.Size = new System.Drawing.Size(130, 17);
			this.RecordingText.TabIndex = 17;
			this.RecordingText.Text = "Recording Stopped";
			// 
			// FrameRate_Text
			// 
			this.FrameRate_Text.Location = new System.Drawing.Point(389, 474);
			this.FrameRate_Text.Margin = new System.Windows.Forms.Padding(4);
			this.FrameRate_Text.Name = "FrameRate_Text";
			this.FrameRate_Text.Size = new System.Drawing.Size(132, 22);
			this.FrameRate_Text.TabIndex = 18;
			this.FrameRate_Text.TextChanged += new System.EventHandler(this.FrameRate_Text_TextChanged);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(344, 479);
			this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(27, 17);
			this.label6.TabIndex = 19;
			this.label6.Text = "fps";
			// 
			// Frame_Monitor
			// 
			this.Frame_Monitor.AutoSize = true;
			this.Frame_Monitor.ForeColor = System.Drawing.SystemColors.ControlText;
			this.Frame_Monitor.Location = new System.Drawing.Point(16, 314);
			this.Frame_Monitor.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.Frame_Monitor.Name = "Frame_Monitor";
			this.Frame_Monitor.Size = new System.Drawing.Size(44, 17);
			this.Frame_Monitor.TabIndex = 20;
			this.Frame_Monitor.Text = "frame";
			// 
			// AA_Combobox
			// 
			this.AA_Combobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.AA_Combobox.FormattingEnabled = true;
			this.AA_Combobox.Location = new System.Drawing.Point(579, 395);
			this.AA_Combobox.Margin = new System.Windows.Forms.Padding(4);
			this.AA_Combobox.Name = "AA_Combobox";
			this.AA_Combobox.Size = new System.Drawing.Size(160, 24);
			this.AA_Combobox.TabIndex = 21;
			this.AA_Combobox.SelectedIndexChanged += new System.EventHandler(this.SmoothingModeChanged);
			// 
			// Interpolation_Combobox
			// 
			this.Interpolation_Combobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.Interpolation_Combobox.FormattingEnabled = true;
			this.Interpolation_Combobox.Location = new System.Drawing.Point(579, 446);
			this.Interpolation_Combobox.Margin = new System.Windows.Forms.Padding(4);
			this.Interpolation_Combobox.Name = "Interpolation_Combobox";
			this.Interpolation_Combobox.Size = new System.Drawing.Size(160, 24);
			this.Interpolation_Combobox.TabIndex = 22;
			this.Interpolation_Combobox.SelectedIndexChanged += new System.EventHandler(this.InterpolationModeChanged);
			// 
			// AAText
			// 
			this.AAText.AutoSize = true;
			this.AAText.Location = new System.Drawing.Point(575, 375);
			this.AAText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.AAText.Name = "AAText";
			this.AAText.Size = new System.Drawing.Size(114, 17);
			this.AAText.TabIndex = 23;
			this.AAText.Text = "Smoothing Mode";
			// 
			// IntrText
			// 
			this.IntrText.AutoSize = true;
			this.IntrText.Location = new System.Drawing.Point(575, 426);
			this.IntrText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.IntrText.Name = "IntrText";
			this.IntrText.Size = new System.Drawing.Size(125, 17);
			this.IntrText.TabIndex = 24;
			this.IntrText.Text = "Interpolation Mode";
			// 
			// RightVolumeSlider
			// 
			this.RightVolumeSlider.Location = new System.Drawing.Point(713, 154);
			this.RightVolumeSlider.Name = "RightVolumeSlider";
			this.RightVolumeSlider.Size = new System.Drawing.Size(20, 148);
			this.RightVolumeSlider.TabIndex = 25;
			this.RightVolumeSlider.ValueChanged += new System.EventHandler(this.RightVolumeChanged);
			// 
			// BalanceSlider
			// 
			this.BalanceSlider.Location = new System.Drawing.Point(607, 129);
			this.BalanceSlider.Name = "BalanceSlider";
			this.BalanceSlider.Size = new System.Drawing.Size(133, 17);
			this.BalanceSlider.TabIndex = 26;
			this.BalanceSlider.Value = 50;
			this.BalanceSlider.ValueChanged += new System.EventHandler(this.BalanceChanged);
			// 
			// LeftVolumeSlider
			// 
			this.LeftVolumeSlider.Location = new System.Drawing.Point(607, 154);
			this.LeftVolumeSlider.Name = "LeftVolumeSlider";
			this.LeftVolumeSlider.Size = new System.Drawing.Size(20, 148);
			this.LeftVolumeSlider.TabIndex = 27;
			this.LeftVolumeSlider.ValueChanged += new System.EventHandler(this.LeftVolumeChanged);
			// 
			// leftVolumeMeter
			// 
			this.leftVolumeMeter.Amplitude = 0F;
			this.leftVolumeMeter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
			this.leftVolumeMeter.Location = new System.Drawing.Point(637, 154);
			this.leftVolumeMeter.Margin = new System.Windows.Forms.Padding(4);
			this.leftVolumeMeter.MaxDb = 18F;
			this.leftVolumeMeter.MinDb = -60F;
			this.leftVolumeMeter.Name = "leftVolumeMeter";
			this.leftVolumeMeter.Size = new System.Drawing.Size(27, 148);
			this.leftVolumeMeter.TabIndex = 28;
			// 
			// rightVolumeMeter
			// 
			this.rightVolumeMeter.Amplitude = 0F;
			this.rightVolumeMeter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
			this.rightVolumeMeter.Location = new System.Drawing.Point(683, 154);
			this.rightVolumeMeter.Margin = new System.Windows.Forms.Padding(4);
			this.rightVolumeMeter.MaxDb = 18F;
			this.rightVolumeMeter.MinDb = -60F;
			this.rightVolumeMeter.Name = "rightVolumeMeter";
			this.rightVolumeMeter.Size = new System.Drawing.Size(27, 148);
			this.rightVolumeMeter.TabIndex = 29;
			this.rightVolumeMeter.Text = "volumeMeter2";
			// 
			// LeftBalanceText
			// 
			this.LeftBalanceText.AutoSize = true;
			this.LeftBalanceText.Location = new System.Drawing.Point(621, 98);
			this.LeftBalanceText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.LeftBalanceText.Name = "LeftBalanceText";
			this.LeftBalanceText.Size = new System.Drawing.Size(24, 17);
			this.LeftBalanceText.TabIndex = 30;
			this.LeftBalanceText.Text = "50";
			// 
			// RightBalanceText
			// 
			this.RightBalanceText.AutoSize = true;
			this.RightBalanceText.Location = new System.Drawing.Point(707, 98);
			this.RightBalanceText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.RightBalanceText.Name = "RightBalanceText";
			this.RightBalanceText.Size = new System.Drawing.Size(24, 17);
			this.RightBalanceText.TabIndex = 31;
			this.RightBalanceText.Text = "50";
			// 
			// LeftVolumeText
			// 
			this.LeftVolumeText.AutoSize = true;
			this.LeftVolumeText.Location = new System.Drawing.Point(600, 314);
			this.LeftVolumeText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.LeftVolumeText.Name = "LeftVolumeText";
			this.LeftVolumeText.Size = new System.Drawing.Size(32, 17);
			this.LeftVolumeText.TabIndex = 32;
			this.LeftVolumeText.Text = "100";
			this.LeftVolumeText.Click += new System.EventHandler(this.LeftVolumeText_Click);
			// 
			// RightVolumeText
			// 
			this.RightVolumeText.AutoSize = true;
			this.RightVolumeText.Location = new System.Drawing.Point(709, 314);
			this.RightVolumeText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.RightVolumeText.Name = "RightVolumeText";
			this.RightVolumeText.Size = new System.Drawing.Size(32, 17);
			this.RightVolumeText.TabIndex = 33;
			this.RightVolumeText.Text = "100";
			// 
			// DitherOnBox
			// 
			this.DitherOnBox.AutoSize = true;
			this.DitherOnBox.Location = new System.Drawing.Point(579, 478);
			this.DitherOnBox.Margin = new System.Windows.Forms.Padding(4);
			this.DitherOnBox.Name = "DitherOnBox";
			this.DitherOnBox.Size = new System.Drawing.Size(68, 21);
			this.DitherOnBox.TabIndex = 34;
			this.DitherOnBox.Text = "Dither";
			this.DitherOnBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.DitherOnBox.UseVisualStyleBackColor = true;
			this.DitherOnBox.CheckedChanged += new System.EventHandler(this.EnableDither);
			// 
			// AudioDeviceChanger
			// 
			this.AudioDeviceChanger.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.AudioDeviceChanger.FormattingEnabled = true;
			this.AudioDeviceChanger.Location = new System.Drawing.Point(592, 55);
			this.AudioDeviceChanger.Margin = new System.Windows.Forms.Padding(4);
			this.AudioDeviceChanger.Name = "AudioDeviceChanger";
			this.AudioDeviceChanger.Size = new System.Drawing.Size(160, 24);
			this.AudioDeviceChanger.TabIndex = 35;
			this.AudioDeviceChanger.SelectedIndexChanged += new System.EventHandler(this.AudioDeviceCombobox_SelectedIndexChanged);
			// 
			// AudioDevice_Text
			// 
			this.AudioDevice_Text.AutoSize = true;
			this.AudioDevice_Text.ForeColor = System.Drawing.Color.Black;
			this.AudioDevice_Text.Location = new System.Drawing.Point(592, 36);
			this.AudioDevice_Text.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.AudioDevice_Text.Name = "AudioDevice_Text";
			this.AudioDevice_Text.Size = new System.Drawing.Size(91, 17);
			this.AudioDevice_Text.TabIndex = 36;
			this.AudioDevice_Text.Text = "Audio Device";
			// 
			// MuteCheckbox
			// 
			this.MuteCheckbox.AutoSize = true;
			this.MuteCheckbox.Checked = true;
			this.MuteCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.MuteCheckbox.Location = new System.Drawing.Point(579, 345);
			this.MuteCheckbox.Margin = new System.Windows.Forms.Padding(4);
			this.MuteCheckbox.Name = "MuteCheckbox";
			this.MuteCheckbox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.MuteCheckbox.Size = new System.Drawing.Size(160, 21);
			this.MuteCheckbox.TabIndex = 37;
			this.MuteCheckbox.Text = "Mute Local Playback";
			this.MuteCheckbox.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.MuteCheckbox.UseVisualStyleBackColor = true;
			this.MuteCheckbox.CheckedChanged += new System.EventHandler(this.MuteAudio_Changed);
			// 
			// FrameMonitorText
			// 
			this.FrameMonitorText.AutoSize = true;
			this.FrameMonitorText.Location = new System.Drawing.Point(129, 314);
			this.FrameMonitorText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.FrameMonitorText.Name = "FrameMonitorText";
			this.FrameMonitorText.Size = new System.Drawing.Size(55, 17);
			this.FrameMonitorText.TabIndex = 38;
			this.FrameMonitorText.Text = "frameln";
			// 
			// toFileCheckbox
			// 
			this.toFileCheckbox.AutoSize = true;
			this.toFileCheckbox.Checked = true;
			this.toFileCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.toFileCheckbox.Location = new System.Drawing.Point(47, 506);
			this.toFileCheckbox.Margin = new System.Windows.Forms.Padding(4);
			this.toFileCheckbox.Name = "toFileCheckbox";
			this.toFileCheckbox.Size = new System.Drawing.Size(72, 21);
			this.toFileCheckbox.TabIndex = 39;
			this.toFileCheckbox.Text = "to File:";
			this.toFileCheckbox.UseVisualStyleBackColor = true;
			this.toFileCheckbox.CheckedChanged += new System.EventHandler(this.CheckedtoFile);
			// 
			// saveVideoRecording
			// 
			this.saveVideoRecording.FileName = "video.sevm";
			this.saveVideoRecording.InitialDirectory = "%appdata%\\SpaceEngineers";
			this.saveVideoRecording.FileOk += new System.ComponentModel.CancelEventHandler(this.saveFileDialog_FileOk);
			// 
			// Save
			// 
			this.Save.Location = new System.Drawing.Point(116, 506);
			this.Save.Margin = new System.Windows.Forms.Padding(4);
			this.Save.Name = "Save";
			this.Save.Size = new System.Drawing.Size(100, 28);
			this.Save.TabIndex = 40;
			this.Save.Text = "Save";
			this.Save.UseVisualStyleBackColor = true;
			this.Save.Click += new System.EventHandler(this.SaveFile);
			// 
			// SavingProgress
			// 
			this.SavingProgress.Location = new System.Drawing.Point(238, 506);
			this.SavingProgress.Margin = new System.Windows.Forms.Padding(4);
			this.SavingProgress.Name = "SavingProgress";
			this.SavingProgress.Size = new System.Drawing.Size(133, 28);
			this.SavingProgress.TabIndex = 41;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Enabled = false;
			this.label7.Location = new System.Drawing.Point(442, 510);
			this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(129, 17);
			this.label7.TabIndex = 42;
			this.label7.Text = "Audio Sample Rate";
			this.label7.Visible = false;
			// 
			// SampleRateTextBox
			// 
			this.SampleRateTextBox.Enabled = false;
			this.SampleRateTextBox.Location = new System.Drawing.Point(579, 510);
			this.SampleRateTextBox.Margin = new System.Windows.Forms.Padding(4);
			this.SampleRateTextBox.MaxLength = 6;
			this.SampleRateTextBox.Name = "SampleRateTextBox";
			this.SampleRateTextBox.Size = new System.Drawing.Size(132, 22);
			this.SampleRateTextBox.TabIndex = 43;
			this.SampleRateTextBox.Text = "0";
			this.SampleRateTextBox.Visible = false;
			this.SampleRateTextBox.TextChanged += new System.EventHandler(this.UpdateSampleRate);
			// 
			// FileSaverBackground
			// 
			this.FileSaverBackground.DoWork += new System.ComponentModel.DoWorkEventHandler(this.SaveFileWork);
			this.FileSaverBackground.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.FileSaverBackground_ProgressChanged);
			this.FileSaverBackground.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.FileSaverBackground_RunWorkerCompleted);
			// 
			// CopyToClipboard
			// 
			this.CopyToClipboard.Location = new System.Drawing.Point(215, 318);
			this.CopyToClipboard.Margin = new System.Windows.Forms.Padding(4);
			this.CopyToClipboard.Name = "CopyToClipboard";
			this.CopyToClipboard.Size = new System.Drawing.Size(205, 28);
			this.CopyToClipboard.TabIndex = 44;
			this.CopyToClipboard.Text = "Export Frame to Clipboard";
			this.CopyToClipboard.UseVisualStyleBackColor = true;
			this.CopyToClipboard.Click += new System.EventHandler(this.CopyToClipboard_Click);
			// 
			// CaptureStreamForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
			this.ClientSize = new System.Drawing.Size(756, 554);
			this.Controls.Add(this.CopyToClipboard);
			this.Controls.Add(this.SampleRateTextBox);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.SavingProgress);
			this.Controls.Add(this.Save);
			this.Controls.Add(this.toFileCheckbox);
			this.Controls.Add(this.FrameMonitorText);
			this.Controls.Add(this.MuteCheckbox);
			this.Controls.Add(this.AudioDevice_Text);
			this.Controls.Add(this.AudioDeviceChanger);
			this.Controls.Add(this.DitherOnBox);
			this.Controls.Add(this.RightVolumeText);
			this.Controls.Add(this.LeftVolumeText);
			this.Controls.Add(this.RightBalanceText);
			this.Controls.Add(this.LeftBalanceText);
			this.Controls.Add(this.rightVolumeMeter);
			this.Controls.Add(this.leftVolumeMeter);
			this.Controls.Add(this.LeftVolumeSlider);
			this.Controls.Add(this.BalanceSlider);
			this.Controls.Add(this.RightVolumeSlider);
			this.Controls.Add(this.IntrText);
			this.Controls.Add(this.AAText);
			this.Controls.Add(this.Interpolation_Combobox);
			this.Controls.Add(this.AA_Combobox);
			this.Controls.Add(this.Frame_Monitor);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.FrameRate_Text);
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
			this.Controls.Add(this.RecordingButton);
			this.Margin = new System.Windows.Forms.Padding(4);
			this.Name = "CaptureStreamForm";
			this.Text = "Capture Stream";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.onClosing);
			((System.ComponentModel.ISupportInitialize)(this.Preview)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button RecordingButton;
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
		private System.Windows.Forms.TextBox FrameRate_Text;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label Frame_Monitor;
		private System.Windows.Forms.ComboBox AA_Combobox;
		private System.Windows.Forms.ComboBox Interpolation_Combobox;
		private System.Windows.Forms.Label AAText;
		private System.Windows.Forms.Label IntrText;
		private System.Windows.Forms.VScrollBar RightVolumeSlider;
		private System.Windows.Forms.VScrollBar LeftVolumeSlider;
		private NAudio.Gui.VolumeMeter leftVolumeMeter;
		private NAudio.Gui.VolumeMeter rightVolumeMeter;
		private System.Windows.Forms.HScrollBar BalanceSlider;
		private System.Windows.Forms.Label LeftBalanceText;
		private System.Windows.Forms.Label RightBalanceText;
		private System.Windows.Forms.Label LeftVolumeText;
		private System.Windows.Forms.Label RightVolumeText;
		private System.Windows.Forms.CheckBox DitherOnBox;
		private System.Windows.Forms.ComboBox AudioDeviceChanger;
		private System.Windows.Forms.Label AudioDevice_Text;
		private System.Windows.Forms.CheckBox MuteCheckbox;
		private System.Windows.Forms.Label FrameMonitorText;
		private System.Windows.Forms.CheckBox toFileCheckbox;
		private System.Windows.Forms.Button Save;
		private System.Windows.Forms.ProgressBar SavingProgress;
		internal System.Windows.Forms.SaveFileDialog saveVideoRecording;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox SampleRateTextBox;
		private System.ComponentModel.BackgroundWorker FileSaverBackground;
		private System.Windows.Forms.Button CopyToClipboard;
	}
}

