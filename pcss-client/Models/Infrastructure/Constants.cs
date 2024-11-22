namespace PCSS.Infrastructure
{
    public class Constants
    {
        public const string TIME_FORMAT = "hh:mmtt";
        public const string DATE_FORMAT = "dd-MMM-yyyy";
        public const string TIMESTAMP_FORMAT = DATE_FORMAT + " " + TIME_FORMAT;

        //2002-09-23 00:00:00.0
        public const string DATE_FORMAT_WS = "yyyy-MM-dd HH:mm:ss.f";
        public const double HOURS_PER_DAY = 5.0;

        public const string FILE_DIVISION_CRIMINAL = "R";
        public const string FILE_DIVISION_CIVIL = "I";
        public const string MIXED_ACTIVITY_CLASS = "M";
        public const string SPECIALITY_ACTIVITY_CLASS = "S";

        public static string[] Y_N = new string[] { "Y", "N" };

        public const double MAGIC_CASE_UNLIMITED_QUANTITY_NO = 100.0;
        public const double MAGIC_WITNESS_SCORE = 0.8;
        public const double MAX_UNLIMITED_CAPACITY = 0.75;


        public const string ESTIMATED_UNIT_HOURS = "HRS";
        public const string ESTIMATED_UNIT_MINS = "MINS";
        public const string ESTIMATED_UNIT_DAYS = "DYS";

        public const string CONTRAINT_HOURS = "HOURS";
        public const string CONTRAINT_CASES = "CASES";
        public const string CONTRAINT_CASES_UNLIMITED = "CASES_UN";
        public const string CONTRAINT_SLOT = "SLOT";

        public const string HOLIDAY_ACTIVITY_TYPE = "HOL";
        public const string ASSIGNMENT_LIST_ACTIVITY_TYPE = "ASL";
        public const string TBA_ACTIVITY_TYPE = "TBA";
        public const string NOT_WORKING_ACTIVITY_TYPE = "NW";
        public const string SITTING_ACTIVITY_TYPE = "SIT";

        public const int MAX_SYNC_LOG = 5000;
        public const string SEIZED = "S";

        public const string USER_ROLE = "USER";
        public const string CONTINUATION_REASON = "CNT";
        public static int VIRTUAL_TBA_SLOT_ID = -1;


        public const string APPEARANCE_STATUS_SCHEDULED = "SCHD";
        public const string APPEARANCE_STATUS_CANCELLED = "CNCL";
        public const string APPEARANCE_STATUS_UNCONFIRMED = "UNCF";
        public const string APPEARANCE_STATUS_TENTATIVE = "TENT";


        public static string JUDICIAL_SCHEDULE_RULE_NO_SCHEDULE = "NS";
        public static DateTime EARLIEST_SYNC_DATE = new DateTime(2014, 01, 01);
        public static string AVAIL_PROV_COURT = "PC";
        public static string AVAIL_UNKNOWN = "?";


        public const string SITTING_FOR_ACTIVITY = "SA";
        public const string SITTING_BUT_NOT_SCHEDULED = "SN";
        public const string SITTING_FOR_OTHER_ACTIVITY = "SO";
        public const string SITTING_IN_OTHER_LOCATION = "SE";
        public const string NOT_SITTING = "NS";
        public const string NOT_SCHEDULED_TO_WORK = "NW";

        public const string TRIAL_TRACKER_PROCEEDED = "PROC";


    }
}