using mcswlib.ServerStatus.Event;
using mcswlib.ServerStatus.ServerInfo;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace mcswlib.ServerStatus;

public class ServerStatus
{
	readonly EventMessages _messages;

	// ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
	internal ServerStatus(string label, ServerStatusUpdater basis, EventMessages msg)
	{
		Label = label;
		Updater = basis;
		NotifyServer = true;
		NotifyCount = true;
		PlayerList = new();
		_messages = msg;
		ApplyServerInfo(null);
	}

	// Identity

	public string Label { get; }
	public ServerStatusUpdater Updater { get; }

	// Settings

	public bool NotifyServer { get; set; }
	public bool NotifyCount { get; set; }
	public bool NotifyNames { get; set; }

	// Current / Last Status
 
	// ReSharper disable once CollectionNeverQueried.Global
	public List<PlayerPayLoad> PlayerList { get; }
	public string LastStatusDate { get; private set; }
	public bool IsOnline { get; private set; }
	public int PlayerCount { get; private set; }
	public int MaxPlayerCount { get; private set; }
	public string Version { get; private set; }
	public string Motd { get; private set; }
	public string LastError { get; private set; }

	/// <summary>
	///     Include a list of Names or UID's of Minecraft-Users.
	///     When they join or leave, the PlayerStateChangedEvent will be triggerd.
	///     NOTE; Only new Servers(1.11+) support this feature! Also, large Servers
	///     don't usually return the actual/complete player list. Hence, this may
	///     not work for some cases.
	/// </summary>
	readonly Dictionary<string, string> _userNames = new();

	readonly Dictionary<string, bool> _userStates = new();

	ServerInfoBase? _last;

	/// <summary>
	///     Will compare the last status with the current one and return event updates.
	/// </summary>
	/// <returns></returns>
	public EventBase[] Update()
	{
		// event-queue
		var events = new List<EventBase>();
		var isFirst = _last == null;
		var current = Updater.GetLatestServerInfo();
		if (current != null)
		{
			// if first info, or last success was different from this (either went online or went offline) => invoke
			if (NotifyServer && (isFirst || _last.HadSuccess != current.HadSuccess))
			{
				Debug.WriteLine("Server '" + Updater.Address + ":" + Updater.Port + "' status change: " + current.HadSuccess);
				var errMsg = current.LastError != null ? "Connection Failed: " + current.LastError.GetType().Name : "";
				events.Add(new OnlineStatusEvent(_messages, current.HadSuccess, current.HadSuccess ? current.ServerMotd : errMsg));
			}

			// if first info, or last player count was different (player went online or offline) => invoke
			if (NotifyCount)
			{
				var diff = isFirst
					? current.CurrentPlayerCount
					: current.CurrentPlayerCount - _last.CurrentPlayerCount;
				if (diff != 0)
				{
					Debug.WriteLine("Server '" + Updater.Address + ":" + Updater.Port + "' count change: " + diff);
					events.Add(new PlayerChangeEvent(_messages, diff));
				}
			}

			// check current list for new players 
			var onlineIds = new List<string>();
			if (current.OnlinePlayers != null)
				foreach (var p in current.OnlinePlayers)
				{
					// save online user id temporarily
					if (!onlineIds.Contains(p.Id))
						onlineIds.Add(p.Id);
					// register name
					_userNames[p.Id] = p.Name;
					// if notify and user has state and last state was offline and user is watched, notify change
					if (NotifyNames && (!_userStates.ContainsKey(p.Id) || !_userStates[p.Id]))
						events.Add(new PlayerStateEvent(_messages, p, true));
					// register state or set to true
					_userStates[p.Id] = true;
				}

			// this needs to be done to avoid ElementChangedException
			var keys = _userStates.Keys.ToArray();
			// check all states for players who went offline
			foreach (var k in keys)
			{
				if (!_userStates[k] || onlineIds.Contains(k)) continue;
				// if user state still true, but he is not in online list => went offline
				_userStates[k] = false;
				// create payload
				var p = new PlayerPayLoad { Id = k, RawName = _userNames[k] };
				// notify => invoke
				if (NotifyNames)
					events.Add(new PlayerStateEvent(_messages, p, false));

			}
		}
		// set new last
		_last = current;
		ApplyServerInfo(current);
		return events.ToArray();
	}


	/// <summary>
	///     Will apply the current server-info to the public vars
	/// </summary>
	/// <param name="si"></param>
	void ApplyServerInfo(ServerInfoBase? si)
	{
		var nu = si == null;

		LastStatusDate = nu ? "-" : si.RequestDate.AddMilliseconds(si.RequestTime).ToString("HH:mm:ss");
		IsOnline = !nu && si.HadSuccess;
		PlayerCount = nu ? 0 : si.CurrentPlayerCount;
		MaxPlayerCount = nu ? 0 : si.MaxPlayerCount;
		Version = nu ? "0.0.0" : si.MinecraftVersion;
		Motd = nu || !si.HadSuccess ? "-" : si.ServerMotd;
		LastError = !nu && si.LastError != null ? si.LastError.GetType().Name : "-";

		PlayerList.Clear();
		if (!nu && si.OnlinePlayers != null) PlayerList.AddRange(si.OnlinePlayers);
	}
}