using System;
using System.Threading;
using mcswlib;
using mcswlib.ServerStatus;

namespace example;

internal class Program
{
	const string TestServer = "Monarquicraft.com";

	static void Main()
	{
		Logger.LogLevel = Types.LogLevel.Debug;

		SinglePing();

		MultiPing();

		AsyncPing();
	}

	static void SinglePing()
	{
		Console.WriteLine("\r\nSinglePing");

		var factory = new ServerStatusFactory();

		var inst = factory.Make(TestServer);

		 inst.Updater.Ping().Wait();
		 var res = inst.Updater.GetLatestServerInfo(true);
		 
		Console.WriteLine("Result: " + res);

		factory.Dispose();
	}

	static void MultiPing()
	{
		Console.WriteLine("\r\nMultiPing");

		var factory = new ServerStatusFactory();
		// create first instance
		factory.Make(TestServer, 25565, false, "One");
		// create second instance (force new)
		factory.Make(TestServer, 25565, true, "Two");

		// create two wrong instances with the same base
		var a = factory.Make(TestServer, 25566, false, "Three");
		var b = factory.Make(TestServer, 25566, false, "Four");

		Console.WriteLine("Compare: " + a.Updater.Equals(b.Updater));

		factory.PingAll(5).Wait();

		foreach (var srv in factory.Entries)
		{
			var events = srv.Update();
			foreach (var evt in events)
			{
				Console.WriteLine($"Server {srv.Label} Event:\r\n{evt}");
			}
		}

		factory.Dispose();
	}

	static void AsyncPing()
	{
		Console.WriteLine("\r\nAsyncPing");

		var factory = new ServerStatusFactory();

		factory.ServerChanged += (sender, e) => {
			var srv = (ServerStatus)sender;
			Console.WriteLine("Got new Events for server: " + srv?.Label);
			foreach (var evt in e)
				Console.WriteLine(evt);
		};

		factory.Make(TestServer, 25565, false, "One");

		factory.StartAutoUpdate(10);

		var cntDwn = 5;
		while(cntDwn-- >= 0)
		{
			Console.WriteLine("Waiting " + (cntDwn * 10) + " seconds for something to happen ...");
			Thread.Sleep(10000);
		}

		factory.Dispose();
	}
}