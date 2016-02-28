using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkypeNet.Lib;
using SkypeNet.Lib.Core.Objects;

namespace SkypeNet.App
{
    public partial class Form1 : Form
    {
        private TaskScheduler _uiScheduler;
        private Lib.SkypeNetClient _skype;
        private string active_call;
        private bool trusted;
        private string partner_handle;

        public SkypeNetClient Skype
        {
            get { return _skype; }
            set
            {
                if (_skype == value)
                    return;

                if (_skype != null )
                {
                    _skype.StatusChanged -= SkypeOnStatusChanged;
                    _skype.MessageReceived -= SkypeOnMessageReceived;

                    _skype.CallReceived -= SkypeOnCallReceived;
                    _skype.CallUpdated -= SkypeOnCallUpdated;

                    _skype.Dispose();
                    _skype = null;
                }

                _skype = value;

                if (_skype != null )
                {
                    _skype.StatusChanged += SkypeOnStatusChanged;
                    _skype.MessageReceived += SkypeOnMessageReceived;

                    _skype.CallReceived += SkypeOnCallReceived;
                    _skype.CallUpdated += SkypeOnCallUpdated;
                }

            }
        }

        public Form1()
        {
            InitializeComponent();

            this.Load += OnLoad_ToInitializeTaskScheduler_Correctly;

            // Hook the output textbox into the programs debug output listeners
        }

        private void OnLoad_ToInitializeTaskScheduler_Correctly(object sender, EventArgs eventArgs)
        {
            // Need to do this here as the SyncContext needs to be correctly set up
            // and you can only gurarantee that when the window message loop is running
            _uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        private void SkypeOnMessageReceived(object sender, string skypeMessage)
        {

            //rtbMessages.AppendText(skypeMessage +"\n");
            if(skypeMessage.Contains("CALLS") && skypeMessage != "CALLS")
            {
                active_call = skypeMessage.Split(' ').Last();
                // rtbMessages.AppendText(active_call +"\n");
                Skype.SendMessage("GET CALL " + active_call + " PARTNER_HANDLE");
            } else if(skypeMessage.Contains("PARTNER_HANDLE"))
            {
                partner_handle = skypeMessage.Split(' ').Last();
                trusted = false;
                if (listView1.FindItemWithText(partner_handle) == null)
                {
                    trusted = false;
                } else
                {
                    trusted = true;
                }
            } else if (skypeMessage.Contains("DURATION"))
            {
                if (listView1.FindItemWithText(partner_handle) == null)
                {
                    trusted = false;
                }
                else
                {
                    trusted = true;
                }
                if (trusted == false)
                {
                    if(checkBox3.Checked == true)  deny_remote_video(active_call);
                    if(checkBox2.Checked == true) deny_local_video(active_call);
                }
            }


        }

        private void deny_remote_video(string call_id)
        {
            // if(receiving_video())
            
            Skype.SendMessage("ALTER CALL " + call_id + " STOP_VIDEO_RECEIVE");
        }

        private void deny_local_video(string call_id)
        {
            Skype.SendMessage("ALTER CALL " + call_id + " STOP_VIDEO_SEND");
        }

        private void SkypeOnStatusChanged(object sender, SkypeStatus skypeStatus)
        {

        }

        private void SkypeOnCallUpdated(object sender, SkypeCall skypeCall)
        {
            //rtbOutput.AppendText("Call Updated: " + skypeCall.Id +" [" + skypeCall.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss") + "] > " + skypeCall.Status + " > " + skypeCall.Duration + "\n");
            // rtbOutput.AppendText("Call Updated: " + skypeCall.Id +" [" + skypeCall.TimeStamp + "] > " + skypeCall.Status + " > " + skypeCall.Duration + "\n");
            block_unauthorized_video();
        }

        private void SkypeOnCallReceived(object sender, SkypeCall skypeCall)
        {
            // rtbOutput.AppendText("Call Received: " + skypeCall.Id + " [" + skypeCall.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss") + "] > " + skypeCall.Status + " > " + skypeCall.Duration + "\n");
            //rtbOutput.AppendText("Call Received: " + skypeCall.Id + " [" + skypeCall.TimeStamp + "] > " + skypeCall.Status + " > " + skypeCall.Duration + "\n");

            block_unauthorized_video();
        }

        private void connect()
        {

            Skype = new Lib.SkypeNetClient();

            var task = Skype.ConnectAsync();

            task.ContinueWith(ret =>
            {
                if (ret.IsCompleted)
                {
                    rtbOutput.AppendText("SkypeGuard active.\n");
                }
                else
                {
                    rtbOutput.AppendText(ret.Exception != null ? ret.Exception.ToString() : "Couldn't connect to Skype. Is it open?");
                }
            }, _uiScheduler);

        }

        private void block_unauthorized_video()
        {
            Skype.SendMessage("SEARCH ACTIVECALLS");
        }

        // not supported in Skype Windows. (OS X supported)
        //private void block_unauthorized_links()
        //{
        //    if(checkBox1.Checked != true) return;
        //    Skype.SendMessage("SEARCH RECENTCHATS");
        //}

        private void Form1_Load(object sender, EventArgs e)
        {
           // timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
           // timer1.Stop();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string[] row = { textBox1.Text };
            var listViewItem = new ListViewItem(row);
            if (listView1.FindItemWithText(textBox1.Text) == null)
            {
                listView1.Items.Add(listViewItem);
            }
            
        }

        private void button2_Click(object sender, EventArgs e)
        {

            foreach (ListViewItem item in listView1.Items)
                if (item.Selected)
                    listView1.Items.Remove(item);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
                notifyIcon1.Visible = true;
                this.Hide();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            connect();
        }

    }
}
