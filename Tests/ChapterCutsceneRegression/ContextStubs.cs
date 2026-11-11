// Attribute/base-type shells only. Execute the production context resolver in
// the pure-C# suite without starting Unity or substituting any resolver logic.
namespace UnityEngine
{
    public class MonoBehaviour { }
    public sealed class DefaultExecutionOrder : System.Attribute
    {
        public DefaultExecutionOrder(int order) { }
    }
    public sealed class SerializeField : System.Attribute { }
    public sealed class Tooltip : System.Attribute
    {
        public Tooltip(string text) { }
    }
}
public static class ChapterCinematicPlayer
{
    public static ChapterCinematicContext PendingContext { get; set; }
}
