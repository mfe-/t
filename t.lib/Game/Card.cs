using System.Diagnostics;

namespace t.lib.Game
{
    [DebuggerDisplay("Value={Value}")]
    public record Card
    {
        public Card(int value)
        {
            Value = value;
        }
        public int Value { get; set; }
    }
}