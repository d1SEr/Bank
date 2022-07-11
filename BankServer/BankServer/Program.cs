using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Server {
    class Program {
        static void Main(string[] args) {
            const int serverPort = 1500;
            IPAddress serverIp = IPAddress.Parse("127.0.0.1");
            string message = "";
            Regex regexGetHttp = new Regex(@"GET\s/(\w*)\s([/:\w]*)\nContentType:application/json\n\n([\w\W]*)$");
            Regex regexPutHttp = new Regex(@"PUT\s/(\w*)\s([/:\w]*)\nContentType:application/json\n\n([\w\W]*)$");
            Match match;
            IPEndPoint ep = new IPEndPoint(serverIp, serverPort);
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try {
                Console.WriteLine("Запуск сервера...");
                serverSocket.Bind(ep);
                serverSocket.Listen(10);
                Console.WriteLine("Сервер запущен. Ожидание подключения...");

                while (true) {
                    Socket handler = serverSocket.Accept();
                    StringBuilder sb = new StringBuilder();
                    byte[] buffer = new byte[8192];
                    string[] str;
                    string[] date;
                    bool isValid = false;
                    if (handler.Available > 0) {
                        int countBytes = handler.Receive(buffer);
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, countBytes));
                        message = sb.ToString();
                        if (regexGetHttp.IsMatch(message)) {
                            match = regexGetHttp.Match(message);

                            if (match.Groups[1].Value == "card") {
                                var anonimObj = new { Number = "", Month = "", Year = "", CVV = 0 };
                                anonimObj = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(match.Groups[3].Value, anonimObj);
                                if (anonimObj != null) {
                                    using (var connection = new SqliteConnection("Data Source=bankdb.db;")) {
                                        connection.Open();
                                        SqliteCommand command = connection.CreateCommand();
                                        command.Connection = connection;
                                        command.CommandText = $"SELECT Cash FROM info WHERE Number='{anonimObj.Number}' AND Month='{anonimObj.Month}' AND Year='{anonimObj.Year}' AND CVV='{anonimObj.CVV}'";
                                        var cash = command.ExecuteScalar();
                                        if (cash == null) {
                                            message = "HTTP 404 Введены некорректные данные";
                                        }
                                        else {
                                            message = $"HTTP 200 OK\n\n{{\n\"Cash\":{cash}\n}}" ;
                                        }
                                        connection.Close();
                                    }
                                }
                            }
                        }
                        else if (regexPutHttp.IsMatch(message)) {
                            match = regexPutHttp.Match(message);
                            if (match.Groups[1].Value == "translation") {
                                var anonimObj = new { Number = "", Month = "", Year = "", CVV = 0, NumberTo = "", Cash = 0 };
                                anonimObj = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(match.Groups[3].Value, anonimObj);
                                if (anonimObj != null) {
                                    using (var connection = new SqliteConnection("Data Source=bankdb.db;")) {
                                        connection.Open();
                                        SqliteCommand command = connection.CreateCommand();
                                        command.Connection = connection;
                                        command.CommandText = $"SELECT id FROM info WHERE Number='{anonimObj.Number}' AND Month='{anonimObj.Month}' AND Year='{anonimObj.Year}' AND CVV='{anonimObj.CVV}'";
                                        var id = command.ExecuteScalar();
                                        if (id == null) {
                                            message = "HTTP 404 Введены некорректные данные";
                                        }
                                        else {
                                            command.CommandText = $"SELECT Cash FROM info WHERE id='{id}'";
                                            var cashVar = command.ExecuteScalar();
                                            if (Convert.ToInt32(cashVar) < Convert.ToInt32(anonimObj.Cash)) {
                                                message = "HTTP 404 На карте недостаточно средств";
                                            }
                                            else if (Convert.ToInt32(anonimObj.Cash) <= 0) {
                                                message = "HTTP 404 Введена некорректная сумма";
                                            }
                                            else {
                                                command.CommandText = $"SELECT id FROM info WHERE Number='{anonimObj.NumberTo}'";
                                                var secondId = command.ExecuteScalar();
                                                if (secondId == null) {
                                                    message = "HTTP 404 Счёт получателя не зарегистрирован";
                                                }
                                                else {
                                                    command.CommandText = $"SELECT VIP FROM info WHERE id='{secondId}'";
                                                    var Vip = command.ExecuteScalar();
                                                    command.CommandText = $"SELECT VIP FROM info WHERE id='{id}'";
                                                    var Vip1 = command.ExecuteScalar();

                                                    if (Convert.ToInt32(Vip) == 1) {
                                                        //command.CommandText = $"UPDATE info SET Cash = Cash + {Convert.ToInt32(anonimObj.Cash)} WHERE id='{secondId}'";
                                                        //command.ExecuteNonQuery();
                                                        if (Convert.ToInt32(Vip1) == 0) {
                                                            command.CommandText = $"UPDATE info SET Cash = Cash - {Convert.ToInt32(anonimObj.Cash)} WHERE id='{id}'";
                                                            command.ExecuteNonQuery();
                                                        }
                                                        message = "HTTP 200 Деньги успешно переведены!\n\n" +
                                                            $"{{\n\"Number\":\"{anonimObj.Number}\",\n" +
                                                            $"\"Month\":{anonimObj.Month},\n" +
                                                            $"\"Year\":{anonimObj.Year},\n" +
                                                            $"\"CVV\":{anonimObj.CVV},\n" +
                                                            $"\"NumberTo\":\"{anonimObj.NumberTo}\",\n" +
                                                            $"\"Cash\":{anonimObj.Cash}\n}}";
                                                    }
                                                    else {
                                                        command.CommandText = $"SELECT Cash FROM info WHERE id='{secondId}'";
                                                        var secondCash = command.ExecuteScalar();
                                                        command.CommandText = $"SELECT LimitCash FROM info WHERE id='{secondId}'";
                                                        var secondLimit = command.ExecuteScalar();
                                                        if (Convert.ToInt32(secondCash) + Convert.ToInt32(anonimObj.Cash) > Convert.ToInt32(secondLimit)) {
                                                            message = "HTTP 404 На счёте у получателя превышен лимит";
                                                        }
                                                        else {
                                                            command.CommandText = $"UPDATE info SET Cash = Cash + {Convert.ToInt32(anonimObj.Cash)} WHERE id = '{secondId}'";
                                                            command.ExecuteNonQuery();
                                                            if (Convert.ToInt32(Vip1) == 0) {
                                                                command.CommandText = $"UPDATE info SET Cash = Cash - {Convert.ToInt32(anonimObj.Cash)} WHERE id = '{id}'";
                                                                command.ExecuteNonQuery();
                                                            }
                                                            message = "HTTP 200 Деньги успешно переведены!\n\n" +
                                                            $"{{\n\"Number\":\"{anonimObj.Number}\",\n" +
                                                            $"\"Month\":{anonimObj.Month},\n" +
                                                            $"\"Year\":{anonimObj.Year},\n" +
                                                            $"\"CVV\":{anonimObj.CVV},\n" +
                                                            $"\"NumberTo\":\"{anonimObj.NumberTo}\",\n" +
                                                            $"\"Cash\":{anonimObj.Cash}\n}}";
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        connection.Close();
                                    }
                                }
                            }
                        }

                        buffer = Encoding.UTF8.GetBytes(message);
                        handler.Send(buffer);
                    }
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
                Console.WriteLine("Сервер отключается...");
                serverSocket.Close();
                Console.WriteLine("Сервер отключён.");
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
