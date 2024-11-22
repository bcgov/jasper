using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{
    public class Ban
    {
        public string BanSeqNo { get; set; }
        public string CommentTxt { get; set; }
        public string BanTypeCd { get; set; }
        public string BanTypeDsc { get; set; }
        public string BanTypeAct { get; set; }
        public string BanTypeSection { get; set; }
        public string BanTypeSubSection { get; set; }
        public string BanStatuteId { get; set; }
        public string BanOrderDate { get; set; }
        public string PartId { get; set; }
    }

    public class AppearanceCount
    {
        public string AppearanceDate { get; set; }
        public string ChargeTxt { get; set; }
        public string ChargeDscTxt { get; set; }
        public string CountNumber { get; set; }
        public string Finding { get; set; }
        public string AppearanceResult { get; set; }
        public string ParticipantId { get; set; }
        public List<Sentence> Sentences { get; set; }
    }

    public class Sentence
    {
        public string AppearanceDate { get; set; }
        public string Charge { get; set; }
        public string CountNumber { get; set; }
        public string Finding { get; set; }
        public string ParticipantId { get; set; }
        public string SntpCd { get; set; }
        public string SentTermPeriodQty { get; set; }
        public string SentTermCd { get; set; }
        public string SentSubtermPeriodQty { get; set; }
        public string SentSubtermCd { get; set; }
        public string SentTertiaryTermPeriodQty { get; set; }
        public string SentTertiaryTermCd { get; set; }
        public string SentIntermittentYn { get; set; }
        public string SentMonetaryAmt { get; set; }
        public string SentDueTtpDt { get; set; }
        public string SentEffectiveDt { get; set; }
        public string SentDetailTxt { get; set; }
        public string SentRecTxt { get; set; }
        public string SentYcjaAdultYouthCd { get; set; }
        public string SentCustodySecureYn { get; set; }
        public List<string> DocmIds { get; set; }
    }

    public class Seal
    {
        public string SealTypeCd { get; set; }
        public string SealTypeDsc { get; set; }
    }

    public class Charge
    {
        public string SectionTxt { get; set; }
        public string SectionDscTxt { get; set; }
    }

    public class Crown
    {
        public string PartId { get; set; }
        public string LastNm { get; set; }
        public string GivenNm { get; set; }
        public string AssignedYn { get; set; }

    }

    public class JustinCounsel
    {
        public string LastNm { get; set; }
        public string GivenNm { get; set; }
        public string CounselEnteredDt { get; set; }
        public string CounselPartId { get; set; }
        public string CounselRelatedRepTypeCd { get; set; }
        public string CounselRrepId { get; set; }
    }

    public class PcssCounsel
    {
        public int? CounselId { get; set; }
        public int? LawSocietyId { get; set; }
        public string LastNm { get; set; }
        public string GivenNm { get; set; }
        public string PrefNm { get; set; }
        public string AddressLine1Txt { get; set; }
        public string AddressLine2Txt { get; set; }
        public string CityTxt { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }
        public string PhoneNoTxt { get; set; }
        public string EmailAddressTxt { get; set; }
        public string ActiveYn { get; set; }
        public string CounselType { get; set; }
        public string OrgNm { get; set; }
    }

    public class JcmComment
    {
        public int? JcmCommentId { get; set; }
        public string JustinNo { get; set; }
        public string PhysicalFileId { get; set; }
        public string CommentTxt { get; set; }
        public string EntDtm { get; set; }
        public string UpdDtm { get; set;}
        public string RotaInitialsCd { get; set; }
        public string FullName { get; set; }
    }

    public class CivilDocumentDetail
    {
        public string ImageId { get; set; }
        public string CivilDocumentId { get; set; }
        public string FileSeqNo { get; set; }
        public string DocumentTypeCd { get; set; }
        public string DocumentTypeDsc { get; set; }
        public string OrderMadeDt { get; set; }
        public string FiledDt { get; set; }
        public string FiledByName { get; set; }
        public string Category { get; set; }
        public string CommentTxt { get; set; }
        public string ConcludedYn { get; set; }
        public bool? HasFutureAppearance { get; set; }
        public string LastAppearanceDt { get; set; }
        public string NextAppearanceDt { get; set; }
        public string CeisAppearanceId { get; set; }
        public string JcDocument { get; set; }
        public string PageNumberTotal { get; set; }
        public string SealedYN { get; set; }
        public string SwornByNm { get;set;}
        public string AffidavitNo { get;set;}
        public IEnumerable<DocumentSupport> DocumentSupport { get; set; }
        public IEnumerable<Issue> Issue { get; set; }
        public IEnumerable<ReferenceDocumentInterest> ReferenceDocumentInterest { get;set; }

    }

    public class ReferenceDocumentInterest
    {
        public string PartyId { get;set; }
        public string PartyName { get;set; }
        public string NonPartyName { get;set; }
    }

    public class DocumentSupport
    {
        public string ActCd { get; set; }
        public string ActDsc { get; set; }
    }

    public class Issue
    {
        public string IssueTypeCd { get; set; }
        public string IssueNumber { get; set; }
        public string IssueDsc { get; set; }
        public string ConcludedYn { get; set; }
    }
}