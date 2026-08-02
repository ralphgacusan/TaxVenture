/// <summary>
/// PURPOSE:
/// Marks an object as capable of PRODUCING a selectable DataValue.
/// Supporting Documents and Tax Code Book implement this only (never
/// IDataValueDestination). Case Folder and Computer implement both this
/// and IDataValueDestination.
/// </summary>
public interface IDataValueSource
{
    DataValue GetDataValue();
}