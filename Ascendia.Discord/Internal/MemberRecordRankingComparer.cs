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

            int rankPointsComparison = y.RankTier.Value.CompareTo(x.RankTier);
            if (rankPointsComparison != 0)
            {
                return rankPointsComparison;
            }

            var lx = x.LeaderboardRank == null || x.LeaderboardRank == 0 ? null : x.LeaderboardRank;
            var ly = y.LeaderboardRank == null || y.LeaderboardRank == 0 ? null : y.LeaderboardRank;

            if (lx == null && ly == null) return 0;
            if (lx == null) return 1;
            if (ly == null) return -1;
           
            return lx.Value.CompareTo(ly);
        }
    }
}