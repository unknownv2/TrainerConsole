namespace Console
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO.Pipes;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Windows.Forms;

    public class Main : Form
    {
        private const string PipeName = "Infinity_Trainer_Debug";
        private const int TickInterval = 250;
        private string _filterText;
        private NamedPipeServerStream _pipe;
        private List<LogEntry> _entries;
        private ConcurrentQueue<QueuedMessage> _messages;
        private System.Timers.Timer _flushTimer;
        private IContainer components;
        private RichTextBox txtLog;
        private Button btnClose;
        private CheckBox ckVerbose;
        private CheckBox ckTimestamps;
        private Button btnCopy;
        private Button btnClear;
        private Label lblFilter;
        private TextBox txtFilter;
        private CheckBox ckMessageType;

        public Main()
        {
            this.InitializeComponent();
            base.Closing += new CancelEventHandler(this.OnWindowClosing);
        }

        private void Append(LogEntry entry)
        {
            if (entry.Type == MessageType.Waiting)
            {
                this.txtLog.Invoke((Action)(() => this.txtLog.AppendText("Waiting for connection...\r\n")));
            }
            else if (entry.Type == MessageType.Connected)
            {
                this.txtLog.Invoke((Action)(() => {
                    if (this.ckTimestamps.Checked)
                    {
                        this.txtLog.AppendText($"[{entry.Time:M/d h:mm:ss tt}] ");
                    }
                    this.txtLog.AppendText("Trainer connected.\r\n");
                }));
            }
            else if (entry.Type == MessageType.Disconnected)
            {
                this.txtLog.Invoke((Action)(() => {
                    if (this.ckTimestamps.Checked)
                    {
                        this.txtLog.AppendText($"[{entry.Time:M/d h:mm:ss tt}] ");
                    }
                    this.txtLog.AppendText("Trainer disconnected.\r\n\r\n");
                }));
            }
            else if ((this._filterText == null) || entry.LoweredMessage.Contains(this._filterText))
            {
                Color color;
                string type;
                switch (entry.Level)
                {
                    case LogLevel.Debug:
                        if (!this.ckVerbose.Checked)
                        {
                            return;
                        }
                        type = "D";
                        color = Color.DarkGray;
                        break;

                    case LogLevel.Info:
                        type = "I";
                        color = Color.Black;
                        break;

                    case LogLevel.Warning:
                        type = "W";
                        color = Color.DarkOrange;
                        break;

                    case LogLevel.Error:
                        type = "E";
                        color = Color.Red;
                        break;

                    case LogLevel.Critical:
                        type = "C";
                        color = Color.DarkRed;
                        break;

                    default:
                        type = "?";
                        color = Color.Black;
                        break;
                }
                this.txtLog.Invoke((Action)(() => {
                    if (this.ckTimestamps.Checked)
                    {
                        this.txtLog.AppendText($"[{entry.Time:M/d h:mm:ss tt}]");
                        if (!this.ckMessageType.Checked)
                        {
                            this.txtLog.AppendText(" ");
                        }
                    }
                    if (this.ckMessageType.Checked)
                    {
                        this.txtLog.AppendText($"[{type}] ");
                    }
                    this.txtLog.AppendText(entry.Message, color);
                    this.txtLog.AppendText(Environment.NewLine);
                }));
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            this._entries = new List<LogEntry>(500);
            this.txtLog.Invoke((Action)(() => this.txtLog.Clear()));
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            base.Close();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            string text = this.txtLog.Text;
            if (text != null)
            {
                Clipboard.SetText(text);
            }
        }

        private void ckTimestamps_CheckedChanged(object sender, EventArgs e)
        {
            this.RefreshLog();
        }

        private void ckVerbose_CheckedChanged(object sender, EventArgs e)
        {
            this.RefreshLog();
        }

        private static NamedPipeServerStream CreatePipe()
        {
            PipeSecurity pipeSecurity = new PipeSecurity();
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            pipeSecurity.AddAccessRule(new PipeAccessRule(current.User, PipeAccessRights.ReadWrite, AccessControlType.Allow));
            if (current.User != current.Owner)
            {
                pipeSecurity.AddAccessRule(new PipeAccessRule(current.Owner, PipeAccessRights.ReadWrite, AccessControlType.Allow));
            }
            pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
            pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier("S-1-15-2-1"), PipeAccessRights.ReadWrite, AccessControlType.Allow));
            pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
            pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.RemoteLogonIdSid, null), PipeAccessRights.ReadWrite, AccessControlType.Allow));
            return new NamedPipeServerStream("Infinity_Trainer_Debug", PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 0x10000, 0x10000, pipeSecurity);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            ComponentResourceManager manager = new ComponentResourceManager(typeof(Main));
            this.txtLog = new RichTextBox();
            this.btnClose = new Button();
            this.ckVerbose = new CheckBox();
            this.ckTimestamps = new CheckBox();
            this.btnCopy = new Button();
            this.btnClear = new Button();
            this.lblFilter = new Label();
            this.txtFilter = new TextBox();
            this.ckMessageType = new CheckBox();
            base.SuspendLayout();
            this.txtLog.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Top;
            this.txtLog.Font = new Font("Consolas", 8.5f);
            this.txtLog.HideSelection = false;
            this.txtLog.Location = new Point(13, 4);
            this.txtLog.Margin = new Padding(4);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = RichTextBoxScrollBars.Vertical;
            this.txtLog.Size = new Size(530, 0x194);
            this.txtLog.TabIndex = 1;
            this.txtLog.Text = "";
            this.btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.btnClose.Location = new Point(0x1bb, 0x1c4);
            this.btnClose.Margin = new Padding(4);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new Size(100, 0x1c);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new EventHandler(this.btnClose_Click);
            this.ckVerbose.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.ckVerbose.AutoSize = true;
            this.ckVerbose.Location = new Point(0xda, 0x1c9);
            this.ckVerbose.Name = "ckVerbose";
            this.ckVerbose.Size = new Size(0x56, 0x15);
            this.ckVerbose.TabIndex = 2;
            this.ckVerbose.Text = "Verbose";
            this.ckVerbose.UseVisualStyleBackColor = true;
            this.ckVerbose.CheckedChanged += new EventHandler(this.ckVerbose_CheckedChanged);
            this.ckTimestamps.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.ckTimestamps.AutoSize = true;
            this.ckTimestamps.Checked = true;
            this.ckTimestamps.CheckState = CheckState.Checked;
            this.ckTimestamps.Location = new Point(310, 0x1c9);
            this.ckTimestamps.Name = "ckTimestamps";
            this.ckTimestamps.Size = new Size(110, 0x15);
            this.ckTimestamps.TabIndex = 4;
            this.ckTimestamps.Text = "Timestamps";
            this.ckTimestamps.UseVisualStyleBackColor = true;
            this.ckTimestamps.CheckedChanged += new EventHandler(this.ckTimestamps_CheckedChanged);
            this.btnCopy.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.btnCopy.Location = new Point(0x4e, 0x1c4);
            this.btnCopy.Margin = new Padding(4);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new Size(100, 0x1c);
            this.btnCopy.TabIndex = 5;
            this.btnCopy.Text = "Copy All";
            this.btnCopy.UseVisualStyleBackColor = true;
            this.btnCopy.Click += new EventHandler(this.btnCopy_Click);
            this.btnClear.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.btnClear.Location = new Point(0x1bb, 0x1a0);
            this.btnClear.Margin = new Padding(4);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new Size(100, 0x1c);
            this.btnClear.TabIndex = 6;
            this.btnClear.Text = "Clear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new EventHandler(this.btnClear_Click);
            this.lblFilter.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            this.lblFilter.AutoSize = true;
            this.lblFilter.Location = new Point(8, 0x1a7);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new Size(0x40, 0x11);
            this.lblFilter.TabIndex = 7;
            this.lblFilter.Text = "Filter:";
            this.txtFilter.Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Bottom;
            this.txtFilter.Location = new Point(0x4e, 420);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new Size(0xe2, 0x17);
            this.txtFilter.TabIndex = 8;
            this.txtFilter.TextChanged += new EventHandler(this.txtFilter_TextChanged);
            this.ckMessageType.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            this.ckMessageType.AutoSize = true;
            this.ckMessageType.Checked = true;
            this.ckMessageType.CheckState = CheckState.Checked;
            this.ckMessageType.Location = new Point(310, 0x1a5);
            this.ckMessageType.Name = "ckMessageType";
            this.ckMessageType.Size = new Size(0x7e, 0x15);
            this.ckMessageType.TabIndex = 9;
            this.ckMessageType.Text = "Message Type";
            this.ckMessageType.UseVisualStyleBackColor = true;
            base.AutoScaleDimensions = new SizeF(7f, 15f);
            base.AutoScaleMode = AutoScaleMode.Font;
            base.ClientSize = new Size(0x22c, 0x1ed);
            base.Controls.Add(this.ckMessageType);
            base.Controls.Add(this.txtFilter);
            base.Controls.Add(this.lblFilter);
            base.Controls.Add(this.btnClear);
            base.Controls.Add(this.btnCopy);
            base.Controls.Add(this.ckTimestamps);
            base.Controls.Add(this.ckVerbose);
            base.Controls.Add(this.btnClose);
            base.Controls.Add(this.txtLog);
            this.Font = new Font("Consolas", 7.8f, FontStyle.Regular, GraphicsUnit.Point, 0);
            //base.Icon = (Icon) manager.GetObject("$this.Icon");
            base.Margin = new Padding(4);
            this.MinimumSize = new Size(0x23e, 450);
            base.Name = "Main";
            base.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Console";
            base.Load += new EventHandler(this.Main_Load);
            base.ResumeLayout(false);
            base.PerformLayout();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            this._entries = new List<LogEntry>(500);
            this._messages = new ConcurrentQueue<QueuedMessage>();
            this._flushTimer = new System.Timers.Timer(250.0);
            this._flushTimer.Elapsed += new ElapsedEventHandler(this.OnFlushTimerTick);
            this._flushTimer.Start();
            Task.Factory.StartNew(new Action(this.ReadLoop), TaskCreationOptions.LongRunning);
        }

        private void OnFlushTimerTick(object sender, ElapsedEventArgs e)
        {
            QueuedMessage message;
            this._flushTimer.Enabled = false;
            DateTime now = FastDateTime.Now;
            while (this._messages.TryDequeue(out message))
            {
                LogLevel level = (LogLevel) BitConverter.ToUInt32(message.Header, 0);
                string str = Encoding.Unicode.GetString(message.Text);
                LogEntry item = new LogEntry {
                    Type = MessageType.Message,
                    Level = level,
                    Message = str,
                    LoweredMessage = str.ToLowerInvariant(),
                    Time = now
                };
                this._entries.Add(item);
                this.Append(item);
                Thread.Sleep(0);
            }
            this._flushTimer.Enabled = true;
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (this._pipe == null)
            {
                NamedPipeServerStream local1 = this._pipe;
            }
            else
            {
                this._pipe.Close();
            }
            this._pipe = null;
        }

        private void ReadLoop()
        {
            while (true)
            {
                this._pipe = CreatePipe();
                if (this._pipe == null)
                {
                    base.Invoke((Action)(() => {
                        MessageBox.Show(this, "Failed to open the named pipe.\r\n\r\nIs the logger already running on your computer?", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                        base.Close();
                    }));
                    return;
                }
                LogEntry entry2 = new LogEntry {
                    Type = MessageType.Waiting,
                    Time = FastDateTime.Now
                };
                LogEntry item = entry2;
                this._entries.Add(item);
                this.Append(item);
                try
                {
                    this._pipe.WaitForConnection();
                    entry2 = new LogEntry {
                        Type = MessageType.Connected,
                        Time = FastDateTime.Now
                    };
                    item = entry2;
                    this._entries.Add(item);
                    this.Append(item);
                    this._messages = new ConcurrentQueue<QueuedMessage>();
                    while (true)
                    {
                        QueuedMessage message = new QueuedMessage {
                            Header = new byte[0x10]
                        };
                        try
                        {
                            if (this._pipe.Read(message.Header, 0, message.Header.Length) > 0)
                            {
                                message.Text = new byte[BitConverter.ToUInt32(message.Header, 8)];
                                if (this._pipe.Read(message.Text, 0, message.Text.Length) == message.Text.Length)
                                {
                                    this._messages.Enqueue(message);
                                    continue;
                                }
                            }
                        }
                        catch (Exception exception1)
                        {
                            this._pipe.Close();
                            base.Invoke((Action)(() => MessageBox.Show(this, @"An error occured while reading from the log pipe. Exception:\r\n\r\n" + exception1.Message, "Pipe Error")));
                        }
                        if (this._pipe == null)
                        {
                            break;
                        }
                        entry2 = new LogEntry {
                            Type = MessageType.Disconnected,
                            Time = FastDateTime.Now
                        };
                        item = entry2;
                        this._entries.Add(item);
                        while (true)
                        {
                            if (this._messages.Count == 0)
                            {
                                this.Append(item);
                                this._pipe.Close();
                                break;
                            }
                            Thread.Sleep(10);
                        }
                    }
                    return;
                }
                catch (Exception)
                {
                    if ((this._pipe != null) && this._pipe.IsConnected)
                    {
                        this._pipe.Close();
                    }
                }
                break;
            }
        }

        private void RefreshLog()
        {
            this._flushTimer.Enabled = false;
            this.txtLog.Visible = false;
            this.txtLog.Clear();
            foreach (LogEntry entry in this._entries)
            {
                this.Append(entry);
            }
            this.txtLog.Visible = true;
            this._flushTimer.Enabled = true;
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            this._filterText = string.IsNullOrWhiteSpace(this.txtFilter.Text) ? null : this.txtFilter.Text.ToLowerInvariant();
            this.RefreshLog();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct QueuedMessage
        {
            public byte[] Header;
            public byte[] Text;
        }
    }
}

