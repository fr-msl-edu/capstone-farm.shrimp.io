using System.Collections.Generic;
using System.Linq;

namespace Capstone.Production;

public static class PeopleText
{
    public static string Join(IEnumerable<string> names)
    {
        var list = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        return list.Count switch
        {
            0 => null,
            1 => list[0],
            _ => string.Join(", ", list.Take(list.Count - 1)) + " and " + list[^1],
        };
    }
}
