﻿using System.Collections.Concurrent;

namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// A string that is interned behind a class so we can do reference equality
/// instead of value equality
/// </summary>
public class Symbol
{
    /// <summary>
    /// The constructor, which is private to ensure that all symbols are interned
    /// </summary>
    /// <param name="nsAndName"></param>
    private Symbol(string nsAndName)
    {
        nsAndName = nsAndName.Replace("+", ".");
        Id = nsAndName;
        var splitOn = nsAndName.LastIndexOf('.');
        Name = nsAndName[(splitOn + 1)..];
        Namespace = nsAndName[..splitOn];
    }

    private static ConcurrentDictionary<string, Symbol> _internedSymbols = new();

    /// <summary>
    /// Construct a new symbol, or return an existing one that matches the given name
    /// </summary>
    /// <param name="nsAndName"></param>
    /// <returns></returns>
    public static Symbol Intern(string nsAndName)
    {
        if (_internedSymbols.TryGetValue(nsAndName, out var symbol))
            return symbol;

        symbol = new Symbol(nsAndName);
        return !_internedSymbols.TryAdd(nsAndName, symbol) ? _internedSymbols[nsAndName] : symbol;
    }

    public string Namespace { get; }

    public string Name { get; }

    public string Id { get; }
}