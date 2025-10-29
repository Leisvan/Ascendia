using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Ascendia.Core.Models
{
    public class ProfileOpenDotaModel
    {
        [JsonPropertyName("account_id")]
        public int AccountId { get; set; }

        [JsonPropertyName("avatarfull")]
        public string? Avatar { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("personaname")]
        public string? PersonaName { get; set; }

        [JsonPropertyName("profileurl")]
        public string? ProfileUrl { get; set; }
    }
}