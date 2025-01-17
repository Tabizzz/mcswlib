﻿using System;
using static mcswlib.Types;

namespace mcswlib;

public static class Logger
{
	public static LogLevel LogLevel = LogLevel.Normal;

	/// <summary>
	///     DateTime Wrapper for Console WriteLine
	/// </summary>
	/// <param name="l"></param>
	/// <param name="lv"></param>
	public static void WriteLine(string l, LogLevel lv = LogLevel.Normal)
	{
		if(LogLevel >= lv)  Console.WriteLine($"[{DateTime.Now:yyyy-MM-ss HH:mm:ss}] {l}");
	}
}