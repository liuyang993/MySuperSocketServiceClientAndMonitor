using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RouterClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            
        }

        RClient client1 = new RClient(1);
        RClient client2 = new RClient(2);
        RClient client3 = new RClient(3);
        RClient client4 = new RClient(4);

        private void btnStart_Click(object sender, EventArgs e)
        {
            client1.Start();
            client2.Start();
            client3.Start();
            client4.Start();

            timer1.Interval = 1000;
            timer1.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            client1.Stop();
            client2.Stop();
            client3.Stop();
            client4.Stop();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
                lbl_Clien1HOurCOunter.Text =            client1.HourCounter
        }
    }
}
