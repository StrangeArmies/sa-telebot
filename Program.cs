using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.InputFiles;

namespace SA.Telebot
{
    class Program
    {
        private const string TELEGRAM_ACCESS_TOKEN = "TELEGRAM_ACCESS_TOKEN";
        private const string SECTION_IMAGES = "images";
        private const string SECTION_TEXT = "text";
        private const string SECTION_REACTIONS = "reactions";

        private static readonly string _telegramAccessToken;
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private static readonly IConfigurationRoot _configuration;
        private static ITelegramBotClient _botClient;
        private static readonly Random _rnd = new Random();

        static Program()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            _telegramAccessToken = _configuration[TELEGRAM_ACCESS_TOKEN];
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        static void Main(string[] args)
        {
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                _cts.Cancel();
            };

            try
            {
                Start();

                Console.WriteLine("Press Ctrl+C to exit");
                while (!_cts.Token.IsCancellationRequested)
                    Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex);
            }
            finally
            {
                try { Cleanup(); }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception in Cleanup: {0}", ex);
                }
            }
        }

        private static void Start()
        {
            _botClient = new TelegramBotClient(_telegramAccessToken);
            var me = _botClient.GetMeAsync().Result;
            Console.WriteLine($"Hello, World! I am user {me.Id} and my name is {me.FirstName}.");

            _botClient.OnMessage += Bot_OnMessage;
            _botClient.StartReceiving();
        }

        private static void Cleanup()
        {
            _botClient.StopReceiving();
        }

        static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Message.Text))
                return;

            Console.WriteLine($"Received a text message in chat {e.Message.Chat.Id} from {e.Message.From.Username}.");

            switch (e.Message.Text)
            {
                case var cmd when cmd.StartsWith("/help"):
                    var text = "Список команд:"
                    + Environment.NewLine + "/img - присылает случайную картинку из списка."
                    + Environment.NewLine + "/text - присылает случайный текст."
                    + Environment.NewLine + "Реагирует на следующийе слова или фразы:"
                    + Environment.NewLine + "    "
                    + string.Join(Environment.NewLine + "    ", _configuration.GetSection(SECTION_REACTIONS).GetChildren().Select(i => i.Key));
                    await _botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: text,
                        replyToMessageId: e.Message.MessageId); ;
                    break;
                case var cmd when cmd.StartsWith("/img"):
                    var images = _configuration.GetSection(SECTION_IMAGES).GetChildren().SelectMany(i => PpopulatePath(i.Value)).ToArray();
                    var imagePath = images[_rnd.Next(0, images.Length)];
                    await _botClient.SendPhotoAsync(
                        chatId: e.Message.Chat,
                        new InputOnlineFile(File.OpenRead(imagePath)),
                        replyToMessageId: e.Message.MessageId);
                    break;
                case var cmd when cmd.StartsWith("/text"):
                    var texts = _configuration.GetSection(SECTION_TEXT).GetChildren().ToArray();
                    var msg = texts[_rnd.Next(0, texts.Length)].Value;
                    await _botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: msg,
                        replyToMessageId: e.Message.MessageId);
                    break;
                default:
                    var reaction = _configuration
                        .GetSection(SECTION_REACTIONS)
                        .GetChildren()
                        .FirstOrDefault(i => e.Message.Text.ToLower().StartsWith(i.Key))
                        ?.Value ?? "Unknown command";
                    await _botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: reaction,
                        replyToMessageId: e.Message.MessageId);
                    break;
            }
        }

        private static IEnumerable<string> PpopulatePath(string path)
        {
            if (path.Contains('*'))
                return Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path));

            return new[] { path };
        }
    }
}
