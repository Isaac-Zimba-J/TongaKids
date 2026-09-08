using TongaKids.Models;

namespace TongaKids.Services;

/// <summary>One quiz question: the item being tested, and the cards to show.</summary>
public sealed record PhonicsQuestion(PhonicsItem Target, IReadOnlyList<PhonicsItem> Options);

public interface IPhonicsEngine
{
    /// <summary>
    /// Builds one question per lesson item. Distractors are drawn from the pool,
    /// preferring minimal pairs of the target.
    /// </summary>
    /// <param name="seed">Fixed seed makes a quiz reproducible, which is what
    /// lets the self-check assert on the result.</param>
    IReadOnlyList<PhonicsQuestion> BuildQuiz(
        IReadOnlyList<PhonicsItem> lessonItems,
        IReadOnlyList<PhonicsItem> pool,
        int optionCount,
        int seed);
}
