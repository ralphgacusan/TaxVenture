/// <summary>
/// PURPOSE:
/// Broad category of a transferable value's underlying data, used to
/// validate whether a destination will accept it.
/// </summary>
public enum DataValueType
{
    Number,
    Text,
    Enum // for values like TaxpayerType, TaxOption — text-displayed, category-validated
}