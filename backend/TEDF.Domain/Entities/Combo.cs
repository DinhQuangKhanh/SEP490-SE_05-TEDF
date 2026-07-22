using TEDF.Domain.Common.Primitives;

namespace TEDF.Domain.Entities
{
    /// <summary>
    /// Chuyên ngành hẹp (specialization track) — e.g. SE_COM3.3: .NET Programming.
    /// Abbr is appended to a Program code to form display strings like "BIT_SE_18C_.NET".
    /// </summary>
    public class Combo : Entity<int>
    {
        public string Name { get; private set; } = string.Empty;

        /// <summary>Short abbreviation used to build the display specialization string.</summary>
        public string Abbr { get; private set; } = string.Empty;

        private Combo() { }

        public static Combo Create(int id, string name, string abbr)
        {
            return new Combo { Id = id, Name = name, Abbr = abbr };
        }
    }
}
