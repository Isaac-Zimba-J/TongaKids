using TongaKids.Models;

namespace TongaKids.Services;

public sealed class LearnerSession : ILearnerSession
{
    public Learner? Current { get; private set; }

    public void SetCurrent(Learner learner) => Current = learner;

    public void Clear() => Current = null;
}
