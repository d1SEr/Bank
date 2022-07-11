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
    public partial class show : Form {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
        [DllImport("User32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        Regex regexResponse = new Regex(@"HTTP\s\d\d\d\s([\s\w]*)\n\n([\w\W]*)$");
        Regex regexResponse1 = new Regex(@"HTTP\s\d\d\d\s([\s\w]*)");

        public show() {
            InitializeComponent();
            this.CenterToScreen();
        }

        private void button1_Click(object sender, EventArgs e) {
            if (maskedTextBox1.Text != "" && maskedTextBox2.Text != "" && maskedTextBox3.Text != "" && int.TryParse(maskedTextBox3.Text, out _)) {
                try {
                    //string NumberCardFrom, NumberCardTo;
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect("127.0.0.1", 1500);
                    byte[] buffer = new byte[8192];
                    string message = "";
                    StringBuilder sb = new StringBuilder();

                    message = $"GET /card {@"http://localhost:1500"}\nContentType:application/json\n\n" +
                        $"{{\n\"Number\":\"{maskedTextBox1.Text}\",\n" +
                        $"\"Month\":\"{maskedTextBox2.Text.Split('/')[0]}\",\n" +
                        $"\"Year\":\"{maskedTextBox2.Text.Split('/')[1]}\",\n" +
                        $"\"CVV\":\"{maskedTextBox3.Text}\"\n}}";

                    buffer = Encoding.UTF8.GetBytes(message);
                    socket.Send(buffer);
                    byte[] buffer2 = new byte[8192];
                    int countBytes = socket.Receive(buffer2);
                    sb.Append(Encoding.UTF8.GetString(buffer2, 0, countBytes));

                    string str = sb.ToString();
                    string mes = "";
                    Match match = regexResponse.Match(str);
                    if (match.Value != "") {
                        var anonimObj = new { Cash = 0 };
                        anonimObj = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(match.Groups[2].Value, anonimObj);
                        if (anonimObj == null) {
                            mes = match.Groups[1].Value;
                        }
                        else {
                            mes = $"Сумма на карте {maskedTextBox1.Text}: {anonimObj.Cash}";
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
            else if(maskedTextBox1.Text!="" || maskedTextBox2.Text != "" || maskedTextBox3.Text != "") {
                MessageBox.Show("Введите все данные", "Ошибка");
            }
            else if(!int.TryParse(maskedTextBox3.Text, out _)) {
                MessageBox.Show("Некорректные данные", "Ошибка");
            }
        }

        private void pictureBox4_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void pictureBox4_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }
    }
}
