using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BankClient
{
    public partial class main : Form {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
        [DllImport("User32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public main()
        {
            InitializeComponent();
            this.CenterToScreen();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form temp = new translation();
            temp.ShowDialog();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form temp = new show();
            temp.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e) {
            MessageBox.Show("Сделано Молчановым Андреем, Хомяковым Дмитрием, Буравкиным Матвеем, Флусовым Алексеем","О программе");
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void pictureBox4_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void pictureBox6_Click(object sender, EventArgs e) {
            this.WindowState = FormWindowState.Minimized;
        }
    }
}
