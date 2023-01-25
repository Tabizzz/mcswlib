using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mcswlib.ServerStatus.ServerInfo;

namespace mcswlib.ServerStatus;

public class ServerStatusUpdater
{
	internal ServerStatusUpdater()
	{
		Address = string.Empty;
	}

	// the time over which server infos are held in memory...
	public static TimeSpan ClearSpan = new(0, 1, 0);

	// contains the received Server-Infos.
	readonly List<ServerInfoResult> _history = new();

	public string Address { get; init; }
	public int Port { get; init; }

	public ServerInfoResult[] History => _history.ToArray();

	/// <summary>
	///     This method will ping the server to request infos.
	///     This is done in context of a task and 30 second timeout
	/// </summary>
	public async Task Ping(int timeOut = 30)
	{
		try
		{
			using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeOut));
			await Ping(tokenSource.Token);
		}
		catch (Exception)
		{
			Logger.WriteLine("Ping Timeout? [" + Address + ":" + Port + "]");
			var toAdd = new ServerInfoBase(DateTime.Now.Subtract(TimeSpan.FromSeconds(timeOut)), timeOut * 1000, new TimeoutException());
			_history.Add(new() { New = toAdd, Old = toAdd });
		}
	}
        
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	async Task Ping(CancellationToken ct)
	{
		var srv = "[" + Address + ":" + Port + "]";
		Logger.WriteLine("Pinging server " + srv);

		// safety-wrapper
		try
		{
			// current server-info object
			var OldTask = GetMethod(false, ct);
			var NewTask = GetMethod(true, ct);
			await Task.WhenAll(OldTask, NewTask);
			var Old = OldTask.Result;
			var New = NewTask.Result;
			
			// if the result is null, nothing to do here
			Logger.WriteLine("Ping result " + srv + " is " + (New.HadSuccess || Old.HadSuccess), Types.LogLevel.Debug);
			_history.Add(new(){New = New, Old = Old});
		}
		catch (Exception ex)
		{
			Logger.WriteLine("Fatal Error when Pinging... " + ex, Types.LogLevel.Error);
		}
		// cleanup, done
		ClearMem();
	}

	/// <summary>
	///     Returns the latest (successfull) ServerInfo
	/// </summary>
	/// <param name="successful">filter for the last successfull</param>
	/// <returns></returns>
	public ServerInfoResult? GetLatestServerInfo(bool successful = false)
	{
		var tmpList = new List<ServerInfoResult>();
		tmpList.AddRange(successful ? _history.FindAll(o => o.HadSuccess) : _history);
		return tmpList.Count > 0
			? tmpList.OrderByDescending(ob => ob.RequestDate.AddMilliseconds(ob.RequestTime)).First()
			: null;
	}

	/// <summary>
	///     This method will request the server infos for the given version/method.
	///     it is run as task to make it cancelable
	/// </summary>
	/// <param name="newmethod"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	async Task<ServerInfoBase> GetMethod(bool newmethod, CancellationToken ct)
	{
		return newmethod ? await new GetServerInfoNew(Address, Port).DoAsync(ct) : await new GetServerInfoOld(Address, Port).DoAsync(ct);
	}

	/// <summary>
	///     Remove all objects of which the Timestamp exceeds the Clearspan and run GC.
	/// </summary>
	void ClearMem()
	{
		// TODO TEST
		_history.FindAll(o => o.RequestDate < DateTime.Now.Subtract(ClearSpan)).ForEach(d =>
		{
			_history.Remove(d);
		});
		GC.Collect();
	}
}