using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

class Program
{
    static ConcurrentDictionary<long, List<string>> userTransactions = new();
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var botClient = new TelegramBotClient(config["botToken"]);

        using var cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { }
        };

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token
        );

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Bot {me.Username} bot is start.");
        Console.ReadLine();
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message || message.Text is not { } text)
            return;

        var chatId = message.Chat.Id;

        if (!userTransactions.ContainsKey(chatId))
            userTransactions[chatId] = new List<string>();

        if (text.ToLower().StartsWith("/start"))
        {
            await botClient.SendTextMessageAsync(chatId,
                "Hello! Write your daily income and output.\n\nExample:\n`+10000` - income\n`-5000` - output\n`/report` - daily report",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }
        else if (text.ToLower() == "/report")
        {
            var list = userTransactions[chatId];
            if (!list.Any())
            {
                await botClient.SendTextMessageAsync(chatId, "Not found anything information.");
                return;
            }

            int jami = 0;
            string report = "Today report:\n\n";
            foreach (var entry in list)
            {
                report += $"{entry}\n";
                jami += int.Parse(entry);
            }

            report += $"\nTotal: {jami} dollor";
            await botClient.SendTextMessageAsync(chatId, report);
        }
        else if (text.StartsWith("+") || text.StartsWith("-"))
        {
            if (int.TryParse(text, out int summa))
            {
                userTransactions[chatId].Add(text);
                await botClient.SendTextMessageAsync(chatId, $"Accepted: {text} dollor");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Please, Send a correct number. Example: +10000 yoki `-5000`");
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId, "Uncorrect commond. Please send a number or write report.");
        }
    }

    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }
}