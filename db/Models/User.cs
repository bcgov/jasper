﻿using System;
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

        public Guid? ADId { get; set; }

        public string ADUsername { get; set; }

        public List<string> GroupIds { get; set; } = [];
    }
}
