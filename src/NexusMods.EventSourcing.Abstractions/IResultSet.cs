﻿namespace NexusMods.EventSourcing.Abstractions;

/// <summary>
/// Defines a result set from a query
/// </summary>
public interface IResultSet
{
    /// <summary>
    /// Advance to the next row in the result set, returns false if there are no more rows, the first row is never
    /// pre-advanced. So an false return value means there are no rows in the result set.
    /// </summary>
    public bool Next();

    /// <summary>
    /// The entity id of the current row
    /// </summary>
    public ulong EntityId { get; }

    /// <summary>
    /// The attribute of the current row
    /// </summary>
    public ulong Attribute { get; }

    /// <summary>
    /// Gets the transaction id of the current row
    /// </summary>
    public ulong Tx { get; }

    /// <summary>
    /// The value type of the current row
    /// </summary>
    public ValueTypes ValueType { get; }

    /// <summary>
    /// Gets the value of the current row as an int64
    /// </summary>
    public long ValueInt64 { get; }

    /// <summary>
    /// Gets the value of the current row as an uint64
    /// </summary>
    public ulong ValueUInt64 { get; }

    /// <summary>
    /// Gets the value of the current row as a string
    /// </summary>
    public string ValueString { get; }

    /// <summary>
    /// Gets the value of the current row as a boolean
    /// </summary>
    public bool ValueBoolean { get; }

    /// <summary>
    /// Gets the value of the current row as a double
    /// </summary>
    public double ValueDouble { get; }

    /// <summary>
    /// Gets the value of the current row as a float
    /// </summary>
    public float ValueFloat { get; }

    /// <summary>
    /// Should only use this for testing, gets the value of the current row and boxes it
    /// returning the value as an object
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// All the value types that can be returned from a result set
    /// </summary>
    public enum ValueTypes : int
    {
        /// <summary>
        /// Internal value, should not be returned
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Int64 value
        /// </summary>
        Int64 = 1,
        /// <summary>
        /// UInt64 value
        /// </summary>
        UInt64 = 2,
        /// <summary>
        /// String value
        /// </summary>
        String = 3,
        /// <summary>
        /// Boolean value
        /// </summary>
        Boolean = 4,
        /// <summary>
        /// Double value
        /// </summary>
        Double = 5,
        /// <summary>
        /// Float value
        /// </summary>
        Float = 6,
        /// <summary>
        /// Byte blob value
        /// </summary>
        Bytes = 7
    }
}
