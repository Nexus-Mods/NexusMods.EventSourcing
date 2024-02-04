﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using DuckDB.NET;
using NexusMods.EventSourcing.Abstractions;
using static DuckDB.NET.NativeMethods.DataChunks;
using static DuckDB.NET.NativeMethods.Types;


namespace NexusMods.EventSourcing;

/// <summary>
/// Contains all the pointers for a datom result set from DuckDB and all the way of accessing the data
/// </summary>
unsafe struct DuckDBResultSet : IResultSet
{
    private const int NumChildren = 8;

    public DuckDBResult Result;

    // This is a fantastic hack. Kids don't try this at home
    private fixed ulong _childData[NumChildren];
    private fixed ulong _childValidity[NumChildren];

    private long _rowCount;
    private long _chunkCount;
    private DuckDBDataChunk _currentChunk;
    private long _chunkSize;
    private int _chunkIndex;
    private IntPtr _eDataVector;
    private ulong* _eData;
    private int _chunkRow;
    private IntPtr _aDataVector;
    private ulong* _aData;

    private IntPtr _txDataVector;
    private ulong* _txData;

    public DuckDBResultSet()
    {
        _currentChunk = new DuckDBDataChunk();
        _chunkIndex = -1;
    }

    /// <summary>
    /// Initializes the result set after Result has been set externally.
    /// </summary>
    public void Init()
    {
        _chunkCount = DuckDBResultChunkCount(Result);

        _currentChunk = DuckDBResultGetChunk(Result, 0);
        _chunkSize = DuckDBDataChunkGetSize(_currentChunk);
        _chunkIndex = 0;
        _chunkRow = 0;
        InitChunk();
    }

    private void InitChunk()
    {
        _eDataVector = DuckDBDataChunkGetVector(_currentChunk, 0);
        _eData = (ulong*)DuckDBVectorGetData(_eDataVector);

        _aDataVector = DuckDBDataChunkGetVector(_currentChunk, 1);
        _aData = (ulong*)DuckDBVectorGetData(_aDataVector);

        _txDataVector = DuckDBDataChunkGetVector(_currentChunk, 3);
        _txData = (ulong*)DuckDBVectorGetData(_txDataVector);

        // Value Struct Reading

        var vData = DuckDBDataChunkGetVector(_currentChunk, 2);
        #if DEBUG
        var vType = DuckDBVectorGetColumnType(vData);
        Debug.Assert(NativeMethods.LogicalType.DuckDBStructTypeChildCount(vType) == (int)Enum.GetValues<ValueTypes>().Max() + 1);
        #endif

        for (var childId = 1; childId < NumChildren; childId++)
        {
            var child = DuckDBStructVectorGetChild(vData, childId);
            _childData[childId] = (ulong)DuckDBVectorGetData(child);
            _childValidity[childId] = (ulong)DuckDBVectorGetValidity(child);
        }
    }

    // Ratchets to the next row in the result set. Returns false if there are no more rows.
    public bool Next()
    {
        if (_chunkRow < _chunkSize - 1)
        {
            _chunkRow++;
            return true;
        }
        else if (_chunkIndex < _chunkCount - 1)
        {
            _chunkIndex++;
            _currentChunk = DuckDBResultGetChunk(Result, _chunkIndex);
            _chunkSize = DuckDBDataChunkGetSize(_currentChunk);
            _chunkRow = 0;
            InitChunk();
            return true;
        }
        else
        {
            return false;
        }
    }


    public ulong EntityId => _eData[_chunkRow];
    public ulong Attribute => _aData[_chunkRow];
    public ulong Tx => _txData[_chunkRow];

    public ValueTypes ValueType => (ValueTypes)ValidChild();

    public long ValueInt64 => ((long*)_childData[(int)ValueTypes.Int64])[_chunkRow];
    public ulong ValueUInt64 => ((ulong*)_childData[(int)ValueTypes.UInt64])[_chunkRow];
    public string ValueString
    {
        get
        {
            var ptr = (void*)_childData[(int)ValueTypes.String];
            var data = (DuckDBString2*)ptr + _chunkRow;
            return new string(data->Data, 0, data->Length, Encoding.UTF8);
        }
    }
    public bool ValueBoolean => ((byte*)_childData[(int)ValueTypes.Boolean])[_chunkRow] != 0;
    public double ValueDouble => ((double*)_childData[(int)ValueTypes.Double])[_chunkRow];
    public float ValueFloat => ((float*)_childData[(int)ValueTypes.Float])[_chunkRow];

    public ReadOnlySpan<byte> ValueBlob
    {
        get
        {
            var ptr = (void*)_childData[(int)ValueTypes.Bytes];
            var data = (DuckDBBlob*)ptr + _chunkRow;
            return data->AsSpan();
        }
    }

    /// <summary>
    /// Returns the value of the current row and boxes it into an object.
    /// </summary>
    /// <exception cref="InvalidDataException"></exception>
    public object Value =>
        ValueType switch
        {
            ValueTypes.Int64 => ValueInt64,
            ValueTypes.UInt64 => ValueUInt64,
            ValueTypes.String => ValueString,
            ValueTypes.Boolean => ValueBoolean,
            ValueTypes.Double => ValueDouble,
            ValueTypes.Float => ValueFloat,
            ValueTypes.Bytes => ValueBlob.ToArray(),
            _ => throw new InvalidDataException($"Unknown value type ({ValueType}) in result set.")
        };


    /// <summary>
    /// Gets the index of the child struct member that is valid for the current row.
    /// </summary>
    /// <returns></returns>
    private int ValidChild()
    {
        for (var i = 1; i < NumChildren; i++)
        {
            var entryIdx = _chunkRow / 64;
            var idxInEntry = _chunkRow % 64;
            var isValid = (((ulong*)_childValidity[i])[entryIdx] & (1UL << idxInEntry)) != 0;
            if (isValid) return i;
        }
        throw new InvalidOperationException("No valid child found");
    }


    /// <summary>
    /// Returns the index of the child struct member that is valid for the current row.
    /// </summary>
    /// <returns></returns>
    private unsafe int ValidChildSSE()
    {
        // TODO: This loading of the vector need not happen every row, only when we run out of space
        // in the vector. Further optimization could be done by using a larger vector of 8 elements.
        // With SSE we only need to load it once every 16 rows

        var idx = _chunkRow >> 16;
        var vector = Vector128.Create(0,
            ((ushort*)_childValidity[1])[idx],
            ((ushort*)_childValidity[2])[idx],
            ((ushort*)_childValidity[3])[idx],
            ((ushort*)_childValidity[4])[idx],
            ((ushort*)_childValidity[5])[idx],
            ((ushort*)_childValidity[6])[idx],
            ((ushort*)_childValidity[7])[idx]);

        var shifted = Sse2.ShiftRightLogical(vector, (byte)(_chunkRow % 16));
        shifted &= Vector128<ushort>.One;

        var mask = Sse2.CompareEqual(shifted, Vector128<ushort>.One).AsByte();
        var maskInt = Sse2.MoveMask(mask);

        var index = BitOperations.TrailingZeroCount(maskInt) >> 1;

        Debug.Assert(index is > 0 and < 8, "Invalid type index");

        return index;
    }


}
