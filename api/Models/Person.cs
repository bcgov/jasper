using System;
using System.Collections.Generic;

namespace Scv.Api.Models;

public class Person
{
    public int? Id { get; set; }
    public int? UserId { get; set; }
    public int? ParticipantId { get; set; }
    public int? HomeLocationId { get; set; }
    public string HomeLocationName { get; set; }
    public string GenderTypeCode { get; set; }
    public string GenderTypeDescription { get; set; }
    public string MaritalStatusCode { get; set; }
    public string MaritalStatusDescription { get; set; }
    public string RotaInitials { get; set; }
    public string JudicialNo { get; set; }
    public string SocialInsuranceNo { get; set; }
    public string EmployeeNo { get; set; }
    public string ScheduleGeneratedDate { get; set; }
    public string SchedulePublishedDate { get; set; }
    public List<PersonName> Names { get; set; }
    public List<PersonStatus> Statuses { get; set; }
    public string CurrentJudiciaryTypeCode { get; set; }
    public string CurrentIsSenior { get; set; }
    public string CurrentEntitlementCalcType { get; set; }
    public bool? CurrentIsHours { get; set; }
}

public class PersonName
{
    public int? Id { get; set; }
    public int? PersonId { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
    public string Initials { get; set; }
    public string EffectiveDate { get; set; }
    public string ExpiryDate { get; set; }
    public DateTime? EffDate { get; set; }
    public DateTime? ExpDate { get; set; }
}

public class PersonStatus
{
    public int? Id { get; set; }
    public int? PersonId { get; set; }
    public int? PositionStatusId { get; set; }
    public int? PositionTypeId { get; set; }
    public string PositionCode { get; set; }
    public string PositionDescription { get; set; }
    public string JudiciaryTypeCode { get; set; }
    public string JudiciaryTypeDescription { get; set; }
    public string StatusCode { get; set; }
    public string StatusDescription { get; set; }
    public string InactiveReasonCode { get; set; }
    public string InactiveReasonDescription { get; set; }
    public string EntitlementCalcType { get; set; }
    public bool? IsHours { get; set; }
    public string EffectiveDate { get; set; }
    public string ExpiryDate { get; set; }
    public DateTime? EffDate { get; set; }
    public DateTime? ExpDate { get; set; }
}