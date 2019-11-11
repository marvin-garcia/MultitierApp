using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Models
{
    public class AzureAd
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AudienceId { get; set; }
        public string[] AllowedScopes { get; set; }
        public string[] RequestedScopes { get; set; }
    }
}
