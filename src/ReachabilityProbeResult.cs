using System;
using System.Collections.Generic;

namespace ConnStringDoctor;

/// <summary>
/// Represents the result of a reachability probe operation.
/// </summary>
public sealed class ReachabilityProbeResult : IEquatable<ReachabilityProbeResult>
{
    /// <summary>
    /// Gets the status of the reachability probe.
    /// </summary>
    public ProbeStatus Status { get; }

    /// <summary>
    /// Gets the elapsed time for the entire probe operation.
    /// </summary>
    public TimeSpan TotalElapsedTime { get; }

    /// <summary>
    /// Gets the elapsed time for each individual attempt.
    /// </summary>
    public IReadOnlyList<TimeSpan> AttemptElapsedTimes { get; }

    /// <summary>
    /// Gets the exception that occurred, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets additional details about the probe result.
    /// </summary>
    public string? Details { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReachabilityProbeResult"/> class.
    /// </summary>
    /// <param name="status">The probe status.</param>
    /// <param name="totalElapsedTime">The total elapsed time.</param>
    /// <param name="attemptElapsedTimes">The elapsed time for each attempt.</param>
    /// <param name="exception">The exception that occurred, if any.</param>
    /// <param name="details">Additional details.</param>
    public ReachabilityProbeResult(
        ProbeStatus status,
        TimeSpan totalElapsedTime,
        IReadOnlyList<TimeSpan> attemptElapsedTimes,
        Exception? exception = null,
        string? details = null)
    {
        Status = status;
        TotalElapsedTime = totalElapsedTime;
        AttemptElapsedTimes = attemptElapsedTimes ?? throw new ArgumentNullException(nameof(attemptElapsedTimes));
        Exception = exception;
        Details = details;
    }

    /// <summary>
    /// Gets whether the probe was successful.
    /// </summary>
    public bool IsSuccess => Status == ProbeStatus.Reachable;

    /// <summary>
    /// Gets whether the probe failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Determines whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">The object to compare with the current object.</param>
    /// <returns>true if the objects are equal; otherwise, false.</returns>
    public bool Equals(ReachabilityProbeResult? other)
    {
        if (other is null)
        {
            return false;
        }

        return Status == other.Status
            && TotalElapsedTime.Equals(other.TotalElapsedTime)
            && AttemptElapsedTimes.Count == other.AttemptElapsedTimes.Count
            && Exception?.GetType() == other.Exception?.GetType()
            && Details == other.Details;
    }

    /// <summary>
    /// Determines whether the current object is equal to another object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the objects are equal; otherwise, false.</returns>
    public override bool Equals(object? obj) => Equals(obj as ReachabilityProbeResult);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Status);
        hashCode.Add(TotalElapsedTime);
        hashCode.Add(AttemptElapsedTimes.Count);
        hashCode.Add(Exception?.GetType());
        hashCode.Add(Details);
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Returns a string representation of the probe result.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString() => Status switch
    {
        ProbeStatus.Reachable => "Reachable",
        ProbeStatus.Timeout => "Timeout",
        ProbeStatus.DnsFailure => "DNS Failure",
        ProbeStatus.ConnectionRefused => "Connection Refused",
        ProbeStatus.OtherFailure => "Other Failure",
        _ => "Unknown Status"
    };
}

/// <summary>
/// Represents the status of a reachability probe.
/// </summary>
public enum ProbeStatus
{
    /// <summary>
    /// The host is reachable and the connection was successful.
    /// </summary>
    Reachable,

    /// <summary>
    /// The connection attempt timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// DNS resolution failed.
    /// </summary>
    DnsFailure,

    /// <summary>
    /// The connection was refused by the remote host.
    /// </summary>
    ConnectionRefused,

    /// <summary>
    /// Another type of failure occurred.
    /// </summary>
    OtherFailure
}