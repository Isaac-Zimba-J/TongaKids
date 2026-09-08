using TongaKids.Models;

namespace TongaKids.Services;

/// <summary>Which learner is using the app right now. In-memory only.</summary>
public interface ILearnerSession
{
    Learner? Current { get; }

    void SetCurrent(Learner learner);

    void Clear();
}
