using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models
{
    [Collection(CollectionNameConstants.PERMISSIONS)]
    public class Permission : EntityBase
    {
        // Administrative Functions
        public const string UPDATE_PERMISSIONS_GROUPS = "UPDATE_PERMISSIONS_GROUPS";
        public const string UPDATE_POSITIONS = "UPDATE_POSITIONS";
        public const string UPDATE_POSITIONS_PCSS_MAPPINGS = "UPDATE_POSITIONS_PCSS_MAPPINGS";
        public const string LOCK_UNLOCK_USERS = "LOCK_UNLOCK_USERS";

        // Dashboard
        public const string VIEW_DASHBOARD = "VIEW_DASHBOARD";
        public const string VIEW_OWN_SCHEDULE = "VIEW_OWN_SCHEDULE";
        public const string VIEW_OTHER_JUDGE_SCHEDULE = "VIEW_OTHER_JUDGE_SCHEDULE";
        public const string VIEW_OTHER_CALENDARS = "VIEW_OTHER_CALENDARS";
        public const string VIEW_RESERVED_JUDGMENTS = "VIEW_RESERVED_JUDGMENTS";
        public const string VIEW_CONTINUATIONS = "VIEW_CONTINUATIONS";

        // Court List
        public const string VIEW_COURT_LIST = "VIEW_COURT_LIST";
        public const string RETRIEVE_OWN_COURT_LIST = "RETRIEVE_OWN_COURT_LIST";
        public const string SEARCH_WITHIN_COURT_LIST = "SEARCH_WITHIN_COURT_LIST";
        public const string FILTER_WITHIN_COURT_LIST = "FILTER_WITHIN_COURT_LIST";
        public const string SEARCH_FOR_COURT_LISTS_BY_COURTROOM_AND_DATE = "SEARCH_FOR_COURT_LISTS_BY_COURTROOM_AND_DATE";

        // Court File Search
        public const string SEARCH_FOR_COURT_FILES = "SEARCH_FOR_COURT_FILES";
        public const string ADVANCED_SEARCH_FOR_COURT_FILES = "ADVANCED_SEARCH_FOR_COURT_FILES";
        public const string SORT_SEARCH_RESULTS = "SORT_SEARCH_RESULTS";

        // File Details
        public const string VIEW_COURT_FILE = "VIEW_COURT_FILE";
        public const string VIEW_YOUTH_CRIMINAL_FILE = "VIEW_YOUTH_CRIMINAL_FILE";
        public const string VIEW_SUMMARY = "VIEW_SUMMARY";
        public const string VIEW_ADJUDICATOR_RESTRICTIONS = "VIEW_ADJUDICATOR_RESTRICTIONS";
        public const string VIEW_ACCUSED = "VIEW_ACCUSED";
        public const string VIEW_PARTIES = "VIEW_PARTIES";
        public const string VIEW_CHILDREN = "VIEW_CHILDREN";
        public const string VIEW_CASE_DETAILS = "VIEW_CASE_DETAILS";
        public const string VIEW_APPEARANCES = "VIEW_APPEARANCES";
        public const string VIEW_SENTENCE_ORDER_DETAILS = "VIEW_SENTENCE_ORDER_DETAILS ";

        // Document Access
        public const string VIEW_SINGLE_DOCUMENT = "VIEW_SINGLE_DOCUMENT";
        public const string VIEW_MULTIPLE_DOCUMENTS = "VIEW_MULTIPLE_DOCUMENTS";
        public const string VIEW_SIDE_BY_SIDE_DOCUMENTS = "VIEW_SIDE_BY_SIDE_DOCUMENTS ";
        public const string ADD_EDIT_OWN_DOCUMENT_ANNOTATIONS_ONLY = "ADD_EDIT_OWN_DOCUMENT_ANNOTATIONS_ONLY ";

        // Others
        public const string ACCESS_DARS = "ACCESS_DARS";
        public const string VIEW_QUICK_LINKS = "VIEW_QUICK_LINKS";
        public const string SET_DEFAULT_HOME_SCREEN = "SET_DEFAULT_HOME_SCREEN";
        public const string ADD_EDIT_OWN_NOTES_ONLY = "ADD_EDIT_OWN_NOTES_ONLY";

        public static readonly List<Permission> ALL_PERMISIONS =
        [
            // Administrative Functions
            new Permission
            {
                Code = UPDATE_PERMISSIONS_GROUPS,
                Name = "Update Permission Groups",
                Description = "Permissions to update permission groups",
                IsActive = true,
            },
            new Permission
            {
                Code = UPDATE_POSITIONS,
                Name = "Update Positions",
                Description = "Permissions to update positions",
                IsActive = true,
            },
            new Permission
            {
                Code = UPDATE_POSITIONS_PCSS_MAPPINGS,
                Name = "Update Position Mappings relative to PCSS",
                Description = "Permissions to update posisiont mappings relative to PCSS",
                IsActive = true,
            },
            new Permission
            {
                Code = LOCK_UNLOCK_USERS,
                Name = "Lock/Unlock Users",
                Description = "Permissions to lock or unlock users",
                IsActive = true,
            },

            // Dashboard
            new Permission
            {
                Code = VIEW_DASHBOARD,
                Name = "View Dashboard",
                Description = "Permissions to view dashboard",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_OWN_SCHEDULE,
                Name = "View Own Schedule",
                Description = "Permissions to view own schedule",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_OTHER_JUDGE_SCHEDULE,
                Name = "View Other Judge Schedule",
                Description = "Permissions to view other judge schedule",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_OTHER_CALENDARS,
                Name = "View Other Calendars",
                Description = "Permissions to view other calendars",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_RESERVED_JUDGMENTS,
                Name = "View Reserved Judgments",
                Description = "Permissions to view reserved judgments",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_CONTINUATIONS,
                Name = "View Continuations",
                Description = "Permissions to view continuations",
                IsActive = true
            },

            // Court List
            new Permission
            {
                Code = VIEW_COURT_LIST,
                Name = "View Court List",
                Description = "Permissions to view court list",
                IsActive = true
            },
            new Permission
            {
                Code = RETRIEVE_OWN_COURT_LIST,
                Name = "Retrieve Own Court List",
                Description = "Permissions to retrieve own court list",
                IsActive = true
            },
            new Permission
            {
                Code = SEARCH_WITHIN_COURT_LIST,
                Name = "Search Within Court List",
                Description = "Permissions to search within court list",
                IsActive = true
            },
            new Permission
            {
                Code = FILTER_WITHIN_COURT_LIST,
                Name = "Filter Within Court List",
                Description = "Permissions to filter within court list",
                IsActive = true
            },
            new Permission
            {
                Code = SEARCH_FOR_COURT_LISTS_BY_COURTROOM_AND_DATE,
                Name = "Search for Court Lists By Courtroom and Date",
                Description = "Permissions to search for court lists by courtroom and date",
                IsActive = true
            },

            // Court File Search
            new Permission
            {
                Code = SEARCH_FOR_COURT_FILES,
                Name = "Search for Court Files",
                Description = "Permissions to search for court files",
                IsActive = true
            },
            new Permission
            {
                Code = ADVANCED_SEARCH_FOR_COURT_FILES,
                Name = "Advanced Search for Court Files",
                Description = "Permissions to advanced search for court files",
                IsActive = true
            },
            new Permission
            {
                Code = SORT_SEARCH_RESULTS,
                Name = "Sort Search Results",
                Description = "Permissions to sort search results",
                IsActive = true
            },

            // File Details
            new Permission
            {
                Code = VIEW_COURT_FILE,
                Name = "View Court File",
                Description = "Permissions to view court file",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_YOUTH_CRIMINAL_FILE,
                Name = "View Youth Criminal File",
                Description = "Permissions to view youth criminal file",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_SUMMARY,
                Name = "View Summary",
                Description = "Permissions to view summary",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_ADJUDICATOR_RESTRICTIONS,
                Name = "View Adjudicator Restrictions",
                Description = "Permissions to view adjudicator restrictions",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_ACCUSED,
                Name = "View Accused",
                Description = "Permissions to view accused",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_PARTIES,
                Name = "View Parties",
                Description = "Permissions to view parties",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_CHILDREN,
                Name = "View Children",
                Description = "Permissions to view children",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_CASE_DETAILS,
                Name = "View Case Details",
                Description = "Permissions to view case details",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_APPEARANCES,
                Name = "View Appearances",
                Description = "Permissions to view appearances",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_SENTENCE_ORDER_DETAILS,
                Name = "View Sentence / Order Details",
                Description = "Permissions to view sentence / order details",
                IsActive = true
            },

            // Document Access
            new Permission
            {
                Code = VIEW_SINGLE_DOCUMENT,
                Name = "View Single Document",
                Description = "Permissions to view single document",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_MULTIPLE_DOCUMENTS,
                Name = "View Multiple Documents (Merged)",
                Description = "Permissions to view multiple documents (merged)",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_SIDE_BY_SIDE_DOCUMENTS,
                Name = "View Side-by-Side Documents",
                Description = "Permissions to view side-by-side documents",
                IsActive = true
            },
            new Permission
            {
                Code = ADD_EDIT_OWN_DOCUMENT_ANNOTATIONS_ONLY,
                Name = "Add/Edit Own Document Annotations Only",
                Description = "Permissions to add/edit own document annotations only",
                IsActive = true
            },

            // Others
            new Permission
            {
                Code = ACCESS_DARS,
                Name = "Access DARS",
                Description = "Permissions to access dars",
                IsActive = true
            },
            new Permission
            {
                Code = VIEW_QUICK_LINKS,
                Name = "View Quick Links",
                Description = "Permissions to view quick links",
                IsActive = true
            },
            new Permission
            {
                Code = SET_DEFAULT_HOME_SCREEN,
                Name = "Set Default Home Screen",
                Description = "Permissions to set default home screen",
                IsActive = true
            },
            new Permission
            {
                Code = ADD_EDIT_OWN_NOTES_ONLY,
                Name = "Add/Edit Own Notes Only",
                Description = "Permissions to add/edit own notes only",
                IsActive = true
            }
        ];

        public required string Name { get; set; }

        public required string Code { get; set; }

        public required string Description { get; set; }

        public bool IsActive { get; set; }
    }
}
