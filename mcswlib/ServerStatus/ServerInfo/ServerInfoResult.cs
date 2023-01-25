using System;
using System.Collections.Generic;
using System.Numerics;
using mcswlib.ServerStatus.Event;
namespace mcswlib.ServerStatus.ServerInfo;

/*
 * Store the result of a ping in both the oldd andd the new methods.
 */
public class ServerInfoResult
{
	public required ServerInfoBase Old { get; init; }
	
	public required ServerInfoBase New { get; init; }

	/// <summary>
	///     TimeStamp when the request was done
	/// </summary>
	public DateTime RequestDate => New.HadSuccess ? New.RequestDate : Old.RequestDate;

	/// <summary>
	///     How long did the request take to complete in MS?
	/// </summary>
	public long RequestTime => New.HadSuccess ? New.RequestTime : Old.RequestTime;

	/// <summary>
	///		Ping to the server.
	/// </summary>
	public long Ping => Math.Min(New.RequestTime, Old.RequestTime);

	/// <summary>
	///     Determines if the request was successfull
	/// </summary>
	public bool HadSuccess => New.HadSuccess || Old.HadSuccess;

	/// <summary>
	///     Returns the last occured runtime error
	/// </summary>
	public Exception? LastError => New.HadSuccess ? New.LastError : Old.LastError;

	/// <summary>
	///     Get the raw Message of the day including formatting's and color codes.
	/// </summary>
	public string RawMotd => New.HadSuccess ? New.RawMotd : Old.RawMotd;

	/// <summary>
	///     Gets the server's MOTD as Text
	/// </summary>
	public string ServerMotd => Types.FixMcChat(RawMotd);

	/// <summary>
	///     Gets the server's max player count
	/// </summary>
	public int MaxPlayerCount => New.HadSuccess ? New.MaxPlayerCount : Old.MaxPlayerCount;

	/// <summary>
	///     Gets the server's current player count
	/// </summary>
	public int CurrentPlayerCount => New.HadSuccess ? New.CurrentPlayerCount : Old.CurrentPlayerCount;

	/// <summary>
	///     Gets the server's Minecraft version
	/// </summary>
	public string MinecraftVersion => New.HadSuccess ? New.MinecraftVersion : Old.MinecraftVersion;

	/// <summary>
	///     Gets the server's Online Players as object List
	/// </summary>
	public List<PlayerPayLoad>? OnlinePlayers => New.HadSuccess ? New.OnlinePlayers : Old.OnlinePlayers;

	/// <summary>
	///     The Icon for the Server
	/// </summary>
	public string? FavIcon => New.HadSuccess ? New.FavIcon : Old.FavIcon;
	
	public override string ToString()
	{
		return string.Format($"[Success:{HadSuccess}, Ping:{Ping}ms, LasError:{New.LastError}, Motd:{Old.RawMotd}, MaxPlayers:{New.MaxPlayerCount}, CurrentPlayers:{New.CurrentPlayerCount}, Version:{Old.MinecraftVersion}]");
	}
}