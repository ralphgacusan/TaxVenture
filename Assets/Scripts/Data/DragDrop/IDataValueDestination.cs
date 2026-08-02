/// <summary>
/// PURPOSE:
/// Marks an object as capable of RECEIVING a placed DataValue.
/// </summary>
public interface IDataValueDestination
{
    /// <summary>True if this destination will accept the given value's type/key.</summary>
    bool CanAccept(DataValue value);

    /// <summary>Attempts to actually receive the value. Returns true if accepted.</summary>
    bool TryReceiveValue(DataValue value);
}