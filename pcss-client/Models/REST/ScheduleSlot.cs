using PCSS.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{
    public class ScheduleSlotRoom
    {
        public int? Id { get; set; }
        public string CourtRoomCode { get; set; }
        public bool IsAssignmentListRoom{ get; set;}

        public ScheduleSlotRoom()
        {
        }
        public ScheduleSlotRoom(int? id, string courtRoomCode, bool isAssignmentListRoom)
        {
            this.Id = id;
            this.CourtRoomCode = courtRoomCode;
            this.IsAssignmentListRoom = isAssignmentListRoom;
        }
    }

    public class ScheduleSlotTime
    {
        public string StartTime { get; set; }
        public List<ScheduleSlotRoom> Rooms { get; set; }

        public ScheduleSlotTime()
        {
            this.Rooms = new List<ScheduleSlotRoom>();
        }
    }

    public class ScheduleSlotSitting
    {
        public string CourtSittingCode { get; set; }
        public List<ScheduleSlotTime> Times { get; set; }

        public ScheduleSlotSitting()
        {
            this.Times = new List<ScheduleSlotTime>();
        }
    }

    public class ScheduleSlotActivity
    {
        public string ActivityCode { get; set; }
        public string ActivityDescription { get; set; }
        public string ActivityClassCode { get; set; }
        public string ActivityClassDescription { get; set; }
        public string CapacityConstraintCode { get; set; }
        public List<ScheduleSlotSitting> Sittings { get; set; }

        public ScheduleSlotActivity()
        {
            this.Sittings = new List<ScheduleSlotSitting>();
        }
    }

    public class ScheduleSlotDay
    {
        public string Date { get; set; }
        public List<ScheduleSlotActivity> Activities { get; set; }

        public ScheduleSlotDay(DateTime date)
        {
            this.Date = date.ToString(Constants.DATE_FORMAT);
            this.Activities = new List<ScheduleSlotActivity>();
        }
    }

    public class ScheduleSlotLocation
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<ScheduleSlotDay> Days { get; set; }

        public ScheduleSlotLocation(int id, string name)
        {
            this.Id = id;
            this.Name = name;
            this.Days = new List<ScheduleSlotDay>();
        }
    }
}