using TongaKids.Models;

namespace TongaKids.Services;

public sealed class PhonicsEngine : IPhonicsEngine
{
    public IReadOnlyList<PhonicsQuestion> BuildQuiz(
        IReadOnlyList<PhonicsItem> lessonItems,
        IReadOnlyList<PhonicsItem> pool,
        int optionCount,
        int seed)
    {
        if (lessonItems.Count == 0 || optionCount < 2)
        {
            return [];
        }

        var rng = new Random(seed);
        var questions = new List<PhonicsQuestion>(lessonItems.Count);

        foreach (var target in lessonItems)
        {
            var distractors = pool
                .Where(p => p.Id != target.Id)
                .OrderByDescending(p => MinimalPairScore(target.Grapheme, p.Grapheme))
                .ThenBy(_ => rng.Next())
                .Take(optionCount - 1)
                .ToList();

            var options = distractors
                .Append(target)
                .OrderBy(_ => rng.Next())
                .ToList();

            questions.Add(new PhonicsQuestion(target, options));
        }

        return questions;
    }

    /// <summary>
    /// 2 = a true minimal pair (same length, one differing position).
    /// 1 = same length but further apart. 0 = different length.
    /// </summary>
    internal static int MinimalPairScore(string a, string b)
    {
        if (a.Length != b.Length || a.Length == 0)
        {
            return 0;
        }

        var differences = 0;
        for (var i = 0; i < a.Length; i++)
        {
            if (char.ToUpperInvariant(a[i]) != char.ToUpperInvariant(b[i]))
            {
                differences++;
            }
        }

        return differences == 1 ? 2 : 1;
    }
}
