﻿using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using NexusMods.EventSourcing.Storage.Columns.PackedColumns;
using TransparentValueObjects;

namespace NexusMods.EventSourcing.Storage;

public unsafe struct FourCC : IEquatable<FourCC>
{
    fixed byte _value[4];

    public static FourCC From(string s)
    {
        if (s.Length != 4)
        {
            throw new ArgumentException("FourCC must be 4 characters long");
        }
        var result = new FourCC();
        Encoding.ASCII.GetBytes(s, new Span<byte>(result._value, 4));
        return result;
    }

    public static FourCC From(ReadOnlySpan<byte> s)
    {
        var result = new FourCC();
        s.CopyTo(new Span<byte>(result._value, 4));
        return result;
    }

    #region Chunk Types

    public static readonly FourCC PackedData = From("PDAT");
    public static readonly FourCC PackedIndex = From("PIDX");

    public static readonly FourCC ReferenceData = From("RDAT");
    public static readonly FourCC ReferenceIndex = From("RIDX");


    public static readonly FourCC DatomStoreStateRoot = From("DSSR");

    #endregion

    #region ColumnTypes

    /// <summary>
    /// A constant byte column
    /// </summary>
    public static readonly FourCC ConstByte = From("CSTB");

    /// <summary>
    /// A constant unsigned short column
    /// </summary>
    public static readonly FourCC ConstUShort = From("CSTS");

    /// <summary>
    /// A constant unsigned int column
    /// </summary>
    public static readonly FourCC ConstUInt = From("CSTI");

    /// <summary>
    /// A constant unsigned long column
    /// </summary>
    public static readonly FourCC ConstULong = From("CSTL");


    /// <summary>
    /// A ulong column with a byte data
    /// </summary>
    public static readonly FourCC OffsetULongAsByte = From("OULB");

    /// <summary>
    /// A ulong column with a ushort data
    /// </summary>
    public static readonly FourCC OffsetULongAsUShort = From("OULS");

    /// <summary>
    /// A ulong column with a uint data
    /// </summary>
    public static readonly FourCC OffsetULongAsUInt = From("OULI");

    /// <summary>
    /// A ulong column with a ulong data
    /// </summary>
    public static readonly FourCC OffsetULongAsULong = From("OULL");

    /// <summary>
    /// A uint column with a uint data
    /// </summary>
    public static readonly FourCC OffsetUIntAsUInt = From("OUII");

    /// <summary>
    /// A packed UInt column with a ushort data
    /// </summary>
    public static readonly FourCC OffsetUIntAsUShort = From("OUIS");

    /// <summary>
    /// A packed UInt column with a byte data
    /// </summary>
    public static readonly FourCC OffsetUIntAsByte = From("OUIB");

    /// <summary>
    /// A packed byte column with a byte data
    /// </summary>
    public static FourCC OffsetByteAsByte = From("OBYB");



    /// <summary>
    /// A packed blob column
    /// </summary>
    public static readonly FourCC PackedBlob = From("PBLO");


    #endregion

    public override string ToString()
    {
        fixed(byte* ptr = _value)
            return Encoding.ASCII.GetString(ptr, 4);
    }

    public bool Equals(FourCC other)
    {
        return other.GetHashCode() == GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is FourCC other && Equals(other);
    }

    public static bool operator ==(FourCC left, FourCC right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FourCC left, FourCC right)
    {
        return !left.Equals(right);
    }

    public override int GetHashCode()
    {
        fixed(byte* ptr = _value)
            return BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(ptr, 4));
    }

    public void WriteTo<TWriter>(TWriter writer) where TWriter : IBufferWriter<byte>
    {
        fixed(byte* ptr = _value)
            writer.Write(new ReadOnlySpan<byte>(ptr, 4));
    }
}
