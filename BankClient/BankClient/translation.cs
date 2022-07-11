using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace BankClient {
    public partial class translation : Form {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
        [DllImport("User32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private double Comission { get; set; }
        Regex regexResponse = new Regex(@"HTTP\s\d\d\d\s([\s\w!]*)\n\n([\w\W]*)$");
        Regex regexResponse1 = new Regex(@"HTTP\s\d\d\d\s([\s\w!]*)");

        public translation() {
            InitializeComponent();
            this.CenterToScreen();
        }

        private void button1_Click(object sender, EventArgs e) {
            if (maskedTextBox6.Text != "" && maskedTextBox6.Text == maskedTextBox7.Text) {
                MessageBox.Show("Указана одна и та же карта", "Ошибка");
            }
            else
            if (maskedTextBox1.Text == "" || maskedTextBox2.Text == "" || maskedTextBox3.Text == "" || maskedTextBox6.Text == "" || maskedTextBox7.Text == "") {
                MessageBox.Show("Введите все данные", "Ошибка");
            }
            else if(!(int.TryParse(maskedTextBox3.Text,out _) && int.TryParse(maskedTextBox7.Text, out _))) {
                MessageBox.Show("Некорректные данные", "Ошибка");
            }
            else {
                try {
                    //string NumberCardFrom, NumberCardTo;
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect("127.0.0.1", 1500);
                    byte[] buffer = new byte[8192];
                    string message = "";
                    StringBuilder sb = new StringBuilder();

                    message = $"PUT /translation {@"http://localhost:1500"}\nContentType:application/json\n\n" +
                        $"{{\n\"Number\":\"{maskedTextBox1.Text}\",\n" +
                        $"\"Month\":\"{maskedTextBox2.Text.Split('/')[0]}\",\n" +
                        $"\"Year\":\"{maskedTextBox2.Text.Split('/')[1]}\",\n" +
                        $"\"CVV\":{maskedTextBox3.Text},\n" +
                        $"\"NumberTo\":\"{maskedTextBox6.Text}\",\n" +
                        $"\"Cash\":{label9.Text}\n}}";

                    buffer = Encoding.UTF8.GetBytes(message);
                    socket.Send(buffer);
                    byte[] buffer2 = new byte[8192];
                    int countBytes = socket.Receive(buffer2);
                    sb.Append(Encoding.UTF8.GetString(buffer2, 0, countBytes));
                    string str = sb.ToString();
                    string mes = "";
                    Match match = regexResponse.Match(str);
                    if (match.Value != "") {

                        var anonimObj = new { Number = "", Month = 0, Year = 0, CVV = 0, NumberTo = "", Cash = 0 };
                        anonimObj = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(match.Groups[2].Value, anonimObj);
                        if (anonimObj == null) {
                            mes = match.Groups[1].Value;
                        }
                        else {
                            mes = $"Сумма {anonimObj.Cash} переведена с карты {anonimObj.Number} на карту {anonimObj.NumberTo}";
                        }

                    }
                    else {
                        match = regexResponse1.Match(str);
                        if (match.Value != "") {
                            mes = match.Groups[1].Value;
                        }
                    }
                    MessageBox.Show(mes);
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception ex) {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void pictureBox4_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void maskedTextBox7_TextChanged(object sender, EventArgs e) {
            int cash = 0;
            if (int.TryParse(maskedTextBox7.Text, out cash)) {
                if (cash >= 1000) {
                    label6.Visible = true;
                    if (cash >= 1000 && cash <= 10000) {
                        Comission = 0.01;
                        label6.Text = "Комиссия: 1%";
                        label9.Text = ((int)(cash * (1 - Comission))).ToString();
                    }
                    else if (cash > 10000 && cash <= 100000) {
                        Comission = 0.02;
                        label6.Text = "Комиссия: 2%";
                        label9.Text = ((int)(cash * (1 - Comission))).ToString();
                    }
                    else if (cash > 100000) {
                        Comission = 0.03;
                        label6.Text = "Комиссия: 3%";
                        label9.Text = ((int)(cash * (1 - Comission))).ToString();
                    }
                }
                else {
                    label6.Visible = false;
                    label9.Text = ((int)cash).ToString();
                }
            }
            else {
                label6.Visible = false;
                label9.Text = "-";
            }
        }

        private void maskedTextBox7_MaskInputRejected(object sender, MaskInputRejectedEventArgs e) {

        }
    }
}
