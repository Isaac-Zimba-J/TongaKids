using TongaKids.Models;

namespace TongaKids.Services.SelfCheck;

public sealed class PhonicsEngineSelfCheck(IPhonicsEngine engine) : ISelfCheck
{
    public string Area => "Phonics engine";

    private static PhonicsItem Item(int id, string grapheme) =>
        new() { Id = id, Grapheme = grapheme, LessonId = 1, SortOrder = id };

    public IReadOnlyList<SelfCheckResult> Run()
    {
        var results = new List<SelfCheckResult>();

        void Check(string name, object expected, object actual) =>
            results.Add(new SelfCheckResult(
                name,
                Equals(expected, actual),
                $"expected {expected}, got {actual}"));

        // Minimal-pair scoring
        Check("BA vs DA is a minimal pair", 2, PhonicsEngine.MinimalPairScore("BA", "DA"));
        Check("BA vs BE is a minimal pair", 2, PhonicsEngine.MinimalPairScore("BA", "BE"));
        Check("BA vs DE differs in two places", 1, PhonicsEngine.MinimalPairScore("BA", "DE"));
        Check("BA vs BAA has a different length", 0, PhonicsEngine.MinimalPairScore("BA", "BAA"));
        Check("BA vs BA is identical, not minimal", 1, PhonicsEngine.MinimalPairScore("BA", "BA"));

        // Quiz construction
        var target = Item(1, "BA");
        var lesson = new List<PhonicsItem> { target };
        var pool = new List<PhonicsItem>
        {
            target, Item(2, "DA"), Item(3, "MA"), Item(4, "PA"), Item(5, "TAMBO")
        };

        var quiz = engine.BuildQuiz(lesson, pool, optionCount: 4, seed: 42);

        Check("one question per lesson item", 1, quiz.Count);
        Check("four options are offered", 4, quiz[0].Options.Count);
        Check("the target is among the options", true, quiz[0].Options.Any(o => o.Id == target.Id));
        Check("no option appears twice", 4, quiz[0].Options.Select(o => o.Id).Distinct().Count());
        Check("the long word is not chosen over minimal pairs", false,
            quiz[0].Options.Any(o => o.Grapheme == "TAMBO"));

        // Reproducibility - the same seed must give the same quiz.
        var again = engine.BuildQuiz(lesson, pool, optionCount: 4, seed: 42);
        Check("the same seed gives the same order", true,
            quiz[0].Options.Select(o => o.Id).SequenceEqual(again[0].Options.Select(o => o.Id)));

        // Degenerate input must not throw.
        Check("an empty lesson gives no questions", 0,
            engine.BuildQuiz([], pool, 4, 1).Count);
        Check("a pool smaller than optionCount still works", 2,
            engine.BuildQuiz(lesson, [target, Item(2, "DA")], 4, 1)[0].Options.Count);

        return results;
    }
}
