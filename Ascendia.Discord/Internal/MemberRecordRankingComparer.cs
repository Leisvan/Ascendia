using Ascendia.Core.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ascendia.Discord.Internal
{
    internal class MemberRecordRankingComparer : IComparer<MemberRecord>
    {
        public int Compare(MemberRecord? x, MemberRecord? y)
        {
            if (x?.RankTier == null && y?.RankTier == null) return 0;
            if (x?.RankTier == null) return 1;
            if (y?.RankTier == null) return -1;

            int rankPointsComparison = y.RankTier!.Value.CompareTo(x.RankTier);
            if (rankPointsComparison != 0)
            {
                return rankPointsComparison;
            }

            if (x.LeaderboardRank == null && y.LeaderboardRank == null) return 0;
            if (x.LeaderboardRank == null) return 1;
            if (y.LeaderboardRank == null) return -1;

            return x.LeaderboardRank.Value.CompareTo(y.LeaderboardRank);
        }
    }
}