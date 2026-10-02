using System.Net.Sockets;
using System.Text;

namespace _26._09ExchangeRate
{
    internal class Program
    {
        static async Task Main()
        {
            var currencies = new string[]
            {
                "USD EUR",
                "USD GBP",
                "EUR USD",
                "EUR GBP",
                "GBP USD",
                "GBP EUR"
            };

            using (TcpClient tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync("127.0.0.1", 8888);
                var stream = tcpClient.GetStream();

                var response = new List<byte>();
                int bytesRead = 10;
                foreach (var currency in currencies)
                {
                    byte[] data = Encoding.UTF8.GetBytes(currency + '\n');

                    await stream.WriteAsync(data);

                    while (true)
                    {
                        bytesRead = stream.ReadByte();

                        if (bytesRead == -1)
                        {
                            return;
                        }

                        if (bytesRead == '\n')
                        {
                            break;
                        }

                        response.Add((byte)bytesRead);
                    }
                    var rate = Encoding.UTF8.GetString(response.ToArray());
                    Console.WriteLine($"{currency}: {rate}");
                    if (rate.Contains("Досягнуто максимальну кількість запитів"))
                    {
                        return;
                    }
                    response.Clear();
                    await Task.Delay(2000);
                }

                await stream.WriteAsync(Encoding.UTF8.GetBytes("END\n"));
                Console.ReadLine();
            }
        }
    }
}
