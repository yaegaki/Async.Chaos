# Async.Chaos

Async.Chaos provide chaos for you by async/await.

## Requirements

* C# 7.2
* .NET Framework 4.7.1

## Example

### Recursive

Calc gcd.

```csharp
static async ChaosTask<int> gcd(int _a, int _b)
{
    var (a, b, continuation) = await ChaosTask.Continuation<int, int, int>(_a, _b);

    if (a % b == 0)
    {
        return b;
    }

    return await continuation(b, a % b);
}

// execute
Console.WriteLine(await gcd(824, 128)); // => 8
```

### Checkpoint

Get numbers in random order (not duplicated).

```csharp
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

// execute
var list = await GetNumbersInRandomOrder(100);
Console.WriteLine(string.Join(",", list));  // => 27,54,70,4,7,28,21,93,32,69,83,45,22,...

```

### Concurrent(Simple)

Execute concurrent task (like fork).

```csharp
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

// execute
await SimpleConcurrentTest();
/* =>
Parent.
Child.
*/
```

### Concurrent(EchoServer)

Start echo server.

```csharp
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

// execute
await StartEchoServer();
/* =>
Start echo server.
if you want to exit, Type 'end'.
hoge
your message:hoge
fuga
your message:fuga
piyo
your message:piyo
end
byebye
*/
```
