using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Async.Chaos.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine(string.Join(",", (await GetNumbersInRandomOrder(100))));
            Console.WriteLine("gcd(824, 128) == 8 : " + (await gcd(824, 128) == 8));

            var list = await GetNumbersInRandomOrder(100);
            Console.WriteLine("GetNumbersInRandomOrder(100) contains 0 ~ 99 : " + Enumerable.Range(0, 100).All(i => list.Contains(i)));

            await SimpleConcurrentTest();

            var lastMessage = await StartEchoServer();
            Console.WriteLine("StartEchoServer() return 'end' : " + (lastMessage == "end"));

        }

        // recursive test
        static async ChaosTask<int> gcd(int _a, int _b)
        {
            var (a, b, continuation) = await ChaosTask.Continuation<int, int, int>(_a, _b);

            if (a % b == 0)
            {
                return b;
            }

            return await continuation(b, a % b);
        }

        // checkpoint test
        static async ChaosTask<List<int>> GetNumbersInRandomOrder(int len)
        {
            var r = new Random();
            var result = new List<int>(len);
            var checkpoint = await ChaosTask.Checkpoint<List<int>>();

            if (result.Count < len)
            {
                var value = r.Next(0, len);
                if (!result.Contains(value))
                {
                    result.Add(value);
                }

                await checkpoint();
            }

            return result;
        }

        // concurrent test
        static async ChaosTask<ChaosUnit> SimpleConcurrentTest()
        {
            if (await ChaosTask.Concurrent<ChaosUnit>())
            {
                Console.WriteLine("Parent.");
            }
            else
            {
                Console.WriteLine("Child.");
            }
            return default(ChaosUnit);
        }

        // concurrent test
        static async ChaosTask<string> StartEchoServer()
        {
            Console.WriteLine("Start echo server.");
            Console.WriteLine("if you want to exit, Type 'end'.");
            var finished = ChaosBox.Create(false);
            var message = string.Empty;
            var isParent = await ChaosTask.Concurrent<string>();

            if (isParent)
            {
                while (!finished.Value)
                {
                    await ChaosTask.WaitNext<string>();
                    if (!string.IsNullOrEmpty(message))
                    {
                        if (message == "end")
                        {
                            Console.WriteLine("byebye");
                            finished.Value = true;
                        }
                        else
                        {
                            Console.WriteLine($"your message:{message}");
                        }
                    }
                }

                await ChaosTask.Yield<string>();
                return message;
            }
            else
            {
                while (!finished.Value)
                {
                    async ChaosTask<ChaosUnit> sendMessage()
                    {
                        await Task.Run(() =>
                        {
                            message = Console.ReadLine();
                        });
                        return default(ChaosUnit);
                    }

                    await ChaosTask.WaitTask<string>(sendMessage());
                }
                return string.Empty;
            }
        }
    }
}
