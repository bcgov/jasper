using System;
using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models
{
    [Collection(CollectionNameConstants.USERS)]
    public class User : EntityBase
    {
        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public required string Email { get; set; }

        public bool IsActive { get; set; }

        public bool? IsPendingRegistration { get; set; } = false;

        public Guid? ADId { get; set; }

        public string ADUsername { get; set; }

        /// <summary>
        /// Guid from DIAM
        /// </summary>
        public string UserGuid { get; set; }

        /// <summary>
        /// Guid from ProvJud. This is going to be populated manually for now.
        /// </summary>
        public string NativeGuid { get; set; }

        /// <summary>
        /// Id used as parameter for external systems backend APIs. This is going to be mapped manually for now.
        /// </summary>
        public int? JudgeId { get; set; }

        public List<string> GroupIds { get; set; } = [];
    }
}
