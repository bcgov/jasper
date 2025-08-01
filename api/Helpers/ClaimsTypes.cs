﻿using System.Collections.Generic;
using System.Security.Claims;

namespace Scv.Api.Helpers
{
    public class CustomClaimTypes
    {
        public const string ApplicationCode = nameof(CustomClaimTypes) + nameof(ApplicationCode);
        public const string JcParticipantId = nameof(CustomClaimTypes) + nameof(JcParticipantId);
        public const string JcAgencyCode = nameof(CustomClaimTypes) + nameof(JcAgencyCode);
        public const string IsSupremeUser = nameof(CustomClaimTypes) + nameof(IsSupremeUser);
        public const string CivilFileAccess = nameof(CustomClaimTypes) + nameof(CivilFileAccess);
        public const string ExternalRole = nameof(CustomClaimTypes) + nameof(ExternalRole);
        public const string SubRole = nameof(CustomClaimTypes) + nameof(SubRole);
        public const string Groups = "groups";
        public const string PreferredUsername = "preferred_username";
        public const string Permission = nameof(CustomClaimTypes) + nameof(Permission);
        public const string Role = nameof(CustomClaimTypes) + nameof(Role);
        public const string UserId = nameof(CustomClaimTypes) + nameof(UserId);
        public const string JudgeId = nameof(CustomClaimTypes) + nameof(JudgeId);

        public static readonly List<string> UsedKeycloakClaimTypes =
        [
            ClaimTypes.NameIdentifier,
            "idir_userid",
            "name",
            "preferred_username",
            "groups",
            ClaimTypes.Email
        ];
    }
}
