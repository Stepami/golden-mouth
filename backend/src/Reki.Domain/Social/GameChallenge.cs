using Reki.Domain.Player;
using Reki.Domain.Posts;

namespace Reki.Domain.Social;

public sealed record GameChallenge(
    ChallengeId Id,
    IReadOnlyList<PlayerId> Participants,
    PlayerId InitiatorId,
    ChallengeStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    int Days,
    int MinimumPosts,
    ChallengePerformance InitiatorPerformance,
    ChallengePerformance OpponentPerformance,
    string BestPost,
    double BestPostScore,
    PlayerId? WinnerId,
    int Version)
{
    public PlayerId OpponentId => Participants.Single(playerId => playerId != InitiatorId);

    public static GameChallenge CreatePending(
        PlayerId initiatorId,
        PlayerId opponentId,
        DateTimeOffset createdAtUtc,
        int days,
        int minimumPosts)
    {
        if (initiatorId == opponentId)
        {
            throw new ArgumentException("A player cannot challenge their own channel.", nameof(opponentId));
        }

        return new GameChallenge(
            ChallengeId.New(),
            [initiatorId, opponentId],
            initiatorId,
            ChallengeStatus.Pending,
            createdAtUtc,
            null,
            days,
            minimumPosts,
            ChallengePerformance.Empty,
            ChallengePerformance.Empty,
            string.Empty,
            0,
            null,
            0);
    }

    public bool Includes(PlayerId playerId) => Participants.Contains(playerId);

    public PlayerId GetRival(PlayerId playerId)
    {
        if (!Includes(playerId))
        {
            throw new InvalidOperationException("The player does not participate in this challenge.");
        }

        return Participants.Single(participant => participant != playerId);
    }

    public GameChallenge Accept(PlayerId playerId, DateTimeOffset periodStartedAtUtc)
    {
        if (Status != ChallengeStatus.Pending || playerId != OpponentId)
        {
            throw new InvalidOperationException("Only the invited player can accept a pending challenge.");
        }

        return this with
        {
            Status = ChallengeStatus.Active,
            StartedAtUtc = periodStartedAtUtc,
            Version = Version + 1,
        };
    }

    public int GetDay(DateTimeOffset periodStartedAtUtc)
    {
        if (StartedAtUtc is null)
        {
            return 0;
        }

        return Math.Clamp((int)(periodStartedAtUtc - StartedAtUtc.Value).TotalDays + 1, 1, Days);
    }

    public bool IsFinished(DateTimeOffset periodStartedAtUtc)
    {
        return StartedAtUtc is not null && periodStartedAtUtc >= StartedAtUtc.Value.AddDays(Days);
    }

    public GameChallenge RecordPost(PlayerId playerId, GamePost post)
    {
        if (Status != ChallengeStatus.Active || !Includes(playerId))
        {
            return this;
        }

        ChallengePerformance initiator = InitiatorPerformance;
        ChallengePerformance opponent = OpponentPerformance;
        if (playerId == InitiatorId)
        {
            initiator = initiator.Add(post.Quality, post.ReachRatio);
        }
        else
        {
            opponent = opponent.Add(post.Quality, post.ReachRatio);
        }

        double postScore = ChallengePerformance.CalculatePostScore(post.Quality, post.ReachRatio);
        bool newBest = postScore > BestPostScore;
        return this with
        {
            InitiatorPerformance = initiator,
            OpponentPerformance = opponent,
            BestPost = newBest ? post.Text : BestPost,
            BestPostScore = newBest ? postScore : BestPostScore,
            Version = Version + 1,
        };
    }

    public GameChallenge Complete()
    {
        if (Status != ChallengeStatus.Active)
        {
            return this;
        }

        return this with
        {
            Status = ChallengeStatus.Completed,
            WinnerId = GetWinnerId(),
            Version = Version + 1,
        };
    }

    public ChallengePerformance GetPerformance(PlayerId playerId)
    {
        if (playerId == InitiatorId)
        {
            return InitiatorPerformance;
        }

        if (playerId == OpponentId)
        {
            return OpponentPerformance;
        }

        throw new InvalidOperationException("The player does not participate in this challenge.");
    }

    private PlayerId? GetWinnerId()
    {
        bool initiatorEligible = InitiatorPerformance.PostCount >= MinimumPosts;
        bool opponentEligible = OpponentPerformance.PostCount >= MinimumPosts;
        if (!initiatorEligible && !opponentEligible)
        {
            return null;
        }

        if (initiatorEligible != opponentEligible)
        {
            return initiatorEligible ? InitiatorId : OpponentId;
        }

        int comparison = InitiatorPerformance.Score.CompareTo(OpponentPerformance.Score);
        return comparison switch
        {
            > 0 => InitiatorId,
            < 0 => OpponentId,
            _ => null,
        };
    }
}

