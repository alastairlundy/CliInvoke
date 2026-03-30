namespace CliInvoke.Core;

public class ProcessCancellationPolicy : IEquatable<ProcessCancellationPolicy>
{
    public ProcessCancellationPolicy()
    {
        CancellationMode = ProcessCancellationMode.Graceful;
        CancellationExceptionBehaviour = ProcessExceptionBehaviour.AllowExceptionsIfUnexpected;
    }

    public ProcessCancellationPolicy(ProcessCancellationMode cancellationMode, 
        ProcessExceptionBehaviour exceptionBehaviour = ProcessExceptionBehaviour.AllowExceptionsIfUnexpected)
    {
        CancellationMode = cancellationMode;
        CancellationExceptionBehaviour = exceptionBehaviour;
    }
    
    /// <summary>
    /// The mode to use for cancelling the Process if cancellation is requested.
    /// </summary>
    public ProcessCancellationMode CancellationMode { get; }
    
    /// <summary>
    /// 
    /// </summary>
    public ProcessExceptionBehaviour CancellationExceptionBehaviour { get; }

    /// <summary>
    /// Represents the default process cancellation policy, which uses a graceful cancellation mode.
    /// This value serves as a convenient baseline configuration for process handling.
    /// </summary>
    public static ProcessCancellationPolicy Default =>
        new(ProcessCancellationMode.Graceful);

    /// <summary>
    /// Provides a default process cancellation policy that employs a graceful cancellation mode
    /// while suppressing cancellation-related exceptions that may occur during the cancellation process.
    /// </summary>
    public static ProcessCancellationPolicy DefaultNoException =>
        new(ProcessCancellationMode.Graceful, ProcessExceptionBehaviour.SuppressExceptions);

    /// <summary>
    /// 
    /// </summary>
    public static ProcessCancellationPolicy Forceful =>
        new(ProcessCancellationMode.Forceful);
    
    /// <summary>
    /// 
    /// </summary>
    public static ProcessCancellationPolicy None =>
        new(ProcessCancellationMode.None, ProcessExceptionBehaviour.SuppressExceptions);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="mode"></param>
    /// <returns></returns>
    public static ProcessCancellationPolicy FromCancellationMode(ProcessCancellationMode mode)
        => new(mode);

    public bool Equals(ProcessCancellationPolicy? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        
        return CancellationMode == other.CancellationMode &&
               CancellationExceptionBehaviour == other.CancellationExceptionBehaviour;
    }
    
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is ProcessCancellationPolicy other && Equals(other));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)CancellationMode, (int)CancellationExceptionBehaviour);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(ProcessCancellationPolicy? left, ProcessCancellationPolicy? right) 
        => Equals(left, right);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(ProcessCancellationPolicy? left, ProcessCancellationPolicy? right) 
        => !Equals(left, right);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool Equals(ProcessCancellationPolicy? left, ProcessCancellationPolicy? right)
    {
        if (left is null || right is null)
            return false;
        
        return left.Equals(right);
    }
}