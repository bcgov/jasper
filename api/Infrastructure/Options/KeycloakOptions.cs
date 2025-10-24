namespace Scv.Api.Infrastructure.Options
{
    public class KeycloakOptions
    {
        public string Audience { get; set; } = default!;
        public string Authority { get; set; } = default!;
        public string TokenEndpoint { get; set; }
        public string ClientId { get; set; } = default!;
        public string Secret { get; set; }
        public string Scope { get; set; }

        public int RefreshSkewSeconds { get; set; } = 60; // refresh if within this window
        public int ClockSkewSeconds { get; set; } = 10;   // tolerate small clock drift
    }
}
