using System.Text;

namespace SprutDcsModule.SampleConsole
{
    internal class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            var defaultForeground = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Starting SPRUT-chat...");
            Console.ForegroundColor = defaultForeground;

            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            // Запрашиваем имя пользователя
            Console.Write("Enter your username: ");
            Console.ForegroundColor = ConsoleColor.Green;
            var username = Console.ReadLine();
            Console.ForegroundColor = defaultForeground;
            // Создаем клиент для multicast
            using var sprut = new NotifyClient();

            // Подписываемся на событие получения данных
            sprut.ReceveDataHandler += (s, data) =>
            {
                var message = Encoding.UTF8.GetString(data);
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"{message}");
                Console.ForegroundColor = defaultForeground;
            };


            sprut.ReceivedExceptionHandler += (s, ex) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error received data: {ex.Message}");
                Console.ForegroundColor = default;
            };

            // Подписываемся на событие ошибок при отправке
            sprut.SendingExceptionHandler += (s, ex) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error sending data: {ex.Message}");
                Console.ForegroundColor = defaultForeground;
            };

            // Запускаем клиент
            sprut.Start(8888, "239.255.255.250"); //with multicast mode
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("SPRUT-chat started. Type a message and press Enter to send.");
            Console.ForegroundColor = defaultForeground;

            // Бесконечный цикл для отправки сообщений
            while (true)
            {
                Console.ForegroundColor = defaultForeground;
                var input = Console.ReadLine();

                if (input?.ToLower() == "exit") break;

                // Отправляем сообщение с именем пользователя
                var message = $"{username}: {input}";
                var data = Encoding.UTF8.GetBytes(message);
                sprut.SendDataAsync(data).GetAwaiter().GetResult();
            }

            // Останавливаем клиент
            sprut.StopAsync().GetAwaiter().GetResult();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("SPRUT-chat stopped.");
            Console.ForegroundColor = defaultForeground;
        }
    }
}
