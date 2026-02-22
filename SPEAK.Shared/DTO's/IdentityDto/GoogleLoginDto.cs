using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SPEAK.Shared.DTO_s.IdentityDto
{
    public class GoogleLoginDto
    {
        public const string PROVIDER = "google";

        [JsonPropertyName("idToken")]
        [Required]
        public string IdToken { get; set; } = null!;
    }
}
