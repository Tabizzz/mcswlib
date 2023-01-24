﻿using mcswlib.ServerStatus.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace mcswlib.ServerStatus;

public class ServerStatusFactory : IDisposable
{

	/// <summary>
	///     Constructor with optional
	/// </summary>
	/// <param name="msg"></param>
	public ServerStatusFactory(EventMessages? msg = null)
	{
		msg ??= new();
		Messages = msg;
	}

	readonly List<ServerStatusUpdater> _updaters = new();

	readonly List<ServerStatus> _states = new();

	CancellationTokenSource? _token;

	Task? _updateTask;
        
	public EventMessages Messages { get; }

	public bool AutoUpdating => _updateTask is { IsCompleted: false };

	public ServerStatus[] Entries => _states.ToArray();


	/// <summary>
	///     NOTE: This event may only be triggered when running in Auto-Update mode!
	/// </summary>
	public event EventHandler<EventBase[]>? ServerChanged;

	/// <summary>
	///     Start Auto-updating the servers with given Interval
	/// </summary>
	/// <param name="secInterval"></param>
	/// 
	public void StartAutoUpdate(int secInterval = 30)
	{
		if (secInterval < 0) throw new("Interval must be >= 0");
		if (ServerChanged?.Target == null) throw new("Event-listener must be registered before auto-updating!");
		StopAutoUpdate();
		_token = new();
		_updateTask = AutoUpdater(secInterval, _token.Token);
	}

	/// <summary>
	///     Stop Auto-Updating servers
	/// </summary>
	public void StopAutoUpdate()
	{
		if (AutoUpdating)
		{
			_token?.Cancel();
			try
			{
				_updateTask?.Wait();
			}
			catch (Exception)
			{
				// ignored
			}
		}
		_updateTask = null;
	}

	/// <summary>
	///     Ping & update all the servers and groups
	/// </summary>
	public Task PingAll(int timeOut = 30)
	{
		return Task.WhenAll(_updaters.Select(u => u.Ping(timeOut)));
	}

	/// <summary>
	///     Will either reuse a given ServerStatusBase with same address or create one
	/// </summary>
	/// <param name="forceNewBase"></param>
	/// <param name="label"></param>
	/// <param name="addr"></param>
	/// <param name="port"></param>
	/// <returns>a new ServerStatus object with given params</returns>
	public ServerStatus Make(string addr, int port = 25565, bool forceNewBase = false, string label = "")
	{
		// Get Serverstatusbase or make & add one
		var found = forceNewBase ? null : GetByAddr(addr, port);
		if (found == null)
			_updaters.Add(found = new() { Address = addr, Port = port });
		// Make & add new status
		var state = new ServerStatus(label, found, Messages);
		_states.Add(state);
		return state;
	}

	/// <summary>
	///     Destroy a created ServerStatus object.
	///     Will only destroy the underlying ServerStatusBase if its not used anymore.
	/// </summary>
	/// <param name="status"></param>
	/// <returns>Success indicator</returns>
	public bool Destroy(ServerStatus status)
	{
		if (!_states.Contains(status))
			return false;
		// away with it
		_states.Remove(status);
		// check if the base is still in use and if not remove it
		if (!IsBeingUsed(status.Updater))
		{
			_updaters.Remove(status.Updater);
		}
		// done
		return true;
	}

	/// <summary>
	///     Destroy multiple ServersStatus objects
	/// </summary>
	/// <param name="statuses"></param>
	/// <returns></returns>
	public bool Destroy(ServerStatus[] statuses)
	{
		var res = true;
		foreach (var srv in statuses) res &= Destroy(srv);
		return res;
	}

	/// <summary>
	///     Will try to find a ServerStatusBase with given constraints
	/// </summary>
	/// <param name="addr"></param>
	/// <param name="port"></param>
	/// <returns></returns>
	ServerStatusUpdater? GetByAddr(string addr, int port)
	{
		return _updaters.FirstOrDefault(s => s.Address.ToLower() == addr.ToLower() && s.Port == port);
	}

	/// <summary>
	///     Returns whether the ServerStatusBase is used more than once
	/// </summary>
	/// <param name="ssb"></param>
	/// <returns></returns>
	bool IsBeingUsed(ServerStatusUpdater ssb)
	{
		return _states.FindAll(s => ssb.Equals(s.Updater)).Count > 1;
	}

	/// <summary>
	///     Will run to repeatedly ping all servers in a seperate thread.
	/// </summary>
	/// <param name="secInterval"></param>
	/// <param name="token"></param>
	async Task AutoUpdater(int secInterval, CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			await PingAll();
			_states.ForEach(ss =>
			{
				var evts = ss.Update();
				if (evts.Length > 0 && ServerChanged is not null) ServerChanged(ss, evts);
			});
			await Task.Delay(secInterval * 1000, token);
		}
	}

	/// <summary>
	///     Dispose of all IDispose objects managed by this factory
	/// </summary>
	public void Dispose()
	{
		StopAutoUpdate();
	}
}