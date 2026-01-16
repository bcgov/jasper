using System;
using System.Collections.Generic;
using MongoDB.EntityFrameworkCore;
using Scv.Db.Contants;

namespace Scv.Db.Models;

[Collection(CollectionNameConstants.ORDERS)]
public class Order : EntityBase
{
    public CourtFile CourtFile { get; set; }
    public Referral Referral { get; set; }
    public List<PackageDocument> PackageDocuments { get; set; } = [];
    public List<RelevantCeisDocument> RelevantCeisDocuments { get; set; } = [];
    public OrderStatus Status { get; set; }
    public bool Signed { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string ReviewComments { get; set; }
    public string DocumentData { get; set; }
}

public enum OrderStatus
{
    NotApproved,
    AwaitingFurtherDocumentation,
    Approved
}

public class CourtFile
{
    public string CourtFileNo { get; set; }
    public string CourtLevelCd { get; set; }
    public string CourtDivisionCd { get; set; }
    public string CourtClassCd { get; set; }
    public int? CourtLocationId { get; set; }
    public int? PhysicalFileId { get; set; }
    public string CourtLevelDesc { get; set; }
    public string CourtDivisionDesc { get; set; }
    public string CourtClassDesc { get; set; }
    public int? CourtLocationDesc { get; set; }
    public string FullFileNo { get; set; }
    public string StyleOfCause { get; set; }
    public List<Party> Parties { get; set; } = [];
}

public class Party
{
    public int? PartyId { get; set; }
    public string SurnameNm { get; set; }
    public string FirstGivenNm { get; set; }
    public string SecondGivenNm { get; set; }
    public string ThirdGivenNm { get; set; }
    public string OrganizationNm { get; set; }
    public string PartyType { get; set; }
    public List<PartyRole> Roles { get; set; } = [];
}

public class PartyRole
{
    public string RoleCd { get; set; }
    public string RoleDesc { get; set; }
}

public class Referral
{
    public int? ReferredDocumentId { get; set; }
    public int? PackageId { get; set; }
    public string PackageCreatedBy { get; set; }
    public string ReferralDtm { get; set; }
    public string ReferralNotesTxt { get; set; }
    public int? ReferredByAgenId { get; set; }
    public int? ReferredByPartId { get; set; }
    public int? ReferredByPaasSeqNo { get; set; }
    public string ReferredByName { get; set; }
    public string DutyTypeCd { get; set; }
    public int? SentToAgenId { get; set; }
    public int? SentToPartId { get; set; }
    public int? SentToPaasSeqNo { get; set; }
    public string SentToName { get; set; }
    public string PriorityType { get; set; }
}

public class PackageDocument
{
    public int? DocumentId { get; set; }
    public string DocumentTypeCd { get; set; }
    public string DocumentTypeDesc { get; set; }
    public bool Order { get; set; }
    public bool ReferredDocument { get; set; }
}

public class RelevantCeisDocument
{
    public int? CivilDocumentId { get; set; }
    public string DocumentTypeCd { get; set; }
    public string DocumentTypeDesc { get; set; }
}