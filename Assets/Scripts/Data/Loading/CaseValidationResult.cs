/// <summary>
/// PURPOSE:
/// Generic result wrapper for any load-and-validate operation. Forces
/// every call site to explicitly check IsSuccess before touching Data,
/// rather than risking a null-reference on a failed/malformed load —
/// this is the "never guess information" principle applied to error
/// handling itself.
/// </summary>
public class CaseValidationResult<T>
{
    public bool IsSuccess;
    public T Data;
    public string ErrorMessage;

    public static CaseValidationResult<T> Success(T data)
    {
        return new CaseValidationResult<T> { IsSuccess = true, Data = data, ErrorMessage = null };
    }

    public static CaseValidationResult<T> Fail(string error)
    {
        return new CaseValidationResult<T> { IsSuccess = false, Data = default, ErrorMessage = error };
    }
}