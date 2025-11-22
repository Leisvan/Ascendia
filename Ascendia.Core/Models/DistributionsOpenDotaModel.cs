using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ascendia.Core.Models;

public class DistributionsOpenDotaModel
{
    [JsonPropertyName("ranks")]
    public RanksOpenDotaModel? Ranks { get; set; }

    public class RanksOpenDotaModel
    {
        [JsonPropertyName("sum")]
        public SumOpenDotaModel? Sum { get; set; }
    }

    public class SumOpenDotaModel
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}