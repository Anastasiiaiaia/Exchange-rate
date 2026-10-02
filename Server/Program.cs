using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class Program
    {
        static async Task Main()
        {
            int maxRequests = 4;
            Console.WriteLine("Max requests: " + maxRequests);
            var blockedClients = new Dictionary<string, DateTime>();

            var tcpListener = new TcpListener(IPAddress.Any, 8888);

            try
            {
                tcpListener.Start();
                Console.WriteLine("Сервер запущено. Очiкування пiдключень... ");

                while (true)
                {

                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    string client = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();

                    if (blockedClients.ContainsKey(client))
                    {
                        if (DateTime.Now < blockedClients[client])
                        {
                            var stream = tcpClient.GetStream();

                            await stream.WriteAsync(Encoding.UTF8.GetBytes("Ви заблоковані. Спробуйте підключитися через 1 хвилину.\n"));

                            tcpClient.Close();

                            continue;
                        }
                        else
                        {
                            blockedClients.Remove(client);
                        }
                    }

                    Task.Run(async () => await ProcessClientAsync(tcpClient));

                }
            }
            finally
            {
                tcpListener.Stop();
            }

            async Task ProcessClientAsync(TcpClient tcpClient)
            {

                DateTime startTime = DateTime.Now;
                string time = startTime.ToString("HH:mm:ss");

                var currencies = new Dictionary<string, double>()
                {
                    {"USD", 44.99 },
                    {"EUR", 50.55 },
                    {"GBP", 59.41 },
                };

                var stream = tcpClient.GetStream();

                var response = new List<byte>();
                int bytesRead = 10;
                int requestCount = 0;
                while (true)
                {
                    while (((bytesRead = stream.ReadByte()) != '\n'))
                    {
                        response.Add(((byte)bytesRead));
                    }
                    var currency = Encoding.UTF8.GetString(response.ToArray());

                    if (currency == "END")
                    {
                            break;
                    }
                    requestCount++;

                    if(requestCount > maxRequests)
                    {
                        Console.Write($"Клiєнт {tcpClient.Client.RemoteEndPoint} досяг максимальну кiлькiсть запитiв({maxRequests}). Спробуйте через 1 хвилину");
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("Досягнуто максимальну кiлькiсть запитiв. Спробуйте через 1 хвилину\n"));

                        string client = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
                        blockedClients[client] = DateTime.Now.AddMinutes(1);

                        response.Clear();

                        return;
                    }

                    var parts = currency.Split(' ');

                    if (parts.Length != 2)
                    {
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("Неправильний формат\n"));
                        response.Clear();
                        continue;
                    }

                    string currencyFrom = parts[0].ToUpper();
                    string currencyTo = parts[1].ToUpper();


                    Console.Write($"Клiєнт {tcpClient.Client.RemoteEndPoint} запитав курс {currencyFrom} по вiдношенню до {currencyTo} о {time} ");

                    if (currencies.ContainsKey(currencyFrom) && currencies.ContainsKey(currencyTo))
                    {
                        double r = currencies[currencyFrom] / currencies[currencyTo];

                        string result = $"1 {currencyFrom} = {r:F4} {currencyTo}";

                        result += '\n';

                        await stream.WriteAsync(Encoding.UTF8.GetBytes(result));
                    }
                    else
                    {
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("Валюта не знайдена\n"));
                    }
                    response.Clear();

                    DateTime endTime = DateTime.Now;
                    Console.WriteLine("завершив о " + endTime.ToString("HH:mm:ss"));
                }


                tcpClient.Close();
            }
        }
    }
}
