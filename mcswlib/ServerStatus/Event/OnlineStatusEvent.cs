﻿namespace mcswlib.ServerStatus.Event;

public class OnlineStatusEvent : EventBase
{
	/// <summary>
	///     Online Status of a  Server, given parameters are online bool & statusText msg if offline
	/// </summary>
	/// <param name="msg"></param>
	/// <param name="stat"></param>
	/// <param name="statusText"></param>
	internal OnlineStatusEvent(EventMessages msg, bool stat, string statusText = "") : base(msg)
	{
		ServerStatus = stat;
		StatusText = statusText;
	}

	public bool ServerStatus { get; }

	public string StatusText { get; }

	public override string ToString()
	{
		return (ServerStatus ? Messages.ServerOnline : Messages.ServerOffline).Replace("<text>", StatusText);
	}
}