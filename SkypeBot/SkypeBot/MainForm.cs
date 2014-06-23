using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SkypeBot
{
    public partial class MainForm : Form
    {
        private bool visible = false;

        public MainForm()
        {
            InitializeComponent();

            if (SkypeInteraction.FirstLaunch())
            {
                label1.Text += "OK";
                timer1.Interval = 2500;
                timer1.Start();
            }
            else
            {
                label1.Text += "failed to start";

                MessageBox.Show(@"Application was unable to initialize.
There may be a problem with Skype4COM.dll not registered on system (use reg.bat to register it) or application is requires to have Administrator permissions to access local files.

Please fix the problem and restart the application.", "Error", MessageBoxButtons.OK);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (visible)
                this.Hide();
            else
                this.Show();

            visible = !visible;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Hide();
            visible = false;
            timer1.Stop();
        }
    }
}
