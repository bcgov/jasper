using PCSS.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace PCSS.Models.REST
{
    public class Person
    {
        public int? Id { get; set; }

        public int? UserId { get; set; }
        public double? ParticipantId { get; set; }
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

        public Person()
        {
            this.Names = new List<PersonName>();
            this.Statuses = new List<PersonStatus>();
            this.Addresses = new List<PersonAddress>();
            this.Communications = new List<PersonCommunication>();
            this.Contacts = new List<PersonContact>();
            this.Entitlements = new List<PersonEntitlement>();
            this.HomeLocations = new List<HomeLocation>();
        }

        public List<PersonName> Names { get; set; }
        public void AddName(PersonName item)
        {
            item.PersonId = this.Id;
            if (item.Id != null && this.Names.Exists(x => x.Id != null && x.Id.Value == item.Id.Value))
            {
                this.Names.RemoveAll(x => x.Id.Value == item.Id.Value);
            }
            List<IEffectiveRange> casted = this.Names.Cast<IEffectiveRange>().ToList();
            item.AddToList(casted);
            this.Names = casted.Cast<PersonName>().ToList();
        }
 

        public List<PersonStatus> Statuses { get; set; }
        public void AddStatus(PersonStatus item)
        {
            item.PersonId = this.Id;
            if (item.Id != null && this.Statuses.Exists(x => x.Id != null && x.Id.Value == item.Id.Value) )
            {
                this.Statuses.RemoveAll(x => x.Id.Value == item.Id.Value);
            }
            List<IEffectiveRange> casted = this.Statuses.Cast<IEffectiveRange>().ToList();
            item.AddToList(casted);
            this.Statuses = casted.Cast<PersonStatus>().ToList();
        }
 
 

        public List<HomeLocation> HomeLocations { get; set; }
        public void AddHomeLocation(HomeLocation item)
        {
            item.PersonId = this.Id;
            if (item.Id != null && this.HomeLocations.Exists(x => x.Id != null && x.Id.Value == item.Id.Value))
            {
                this.HomeLocations.RemoveAll(x => x.Id.Value == item.Id.Value);
            }
            List<IEffectiveRange> casted = this.HomeLocations.Cast<IEffectiveRange>().ToList();
            item.PlaceInList(casted);
            this.HomeLocations = casted.Cast<HomeLocation>().ToList();
        }
 
 

        public List<PersonAddress> Addresses { get; set; }
        public void AddAddress(PersonAddress item)
        {
            item.PersonId = this.Id;
            this.Addresses.Add(item);
        }

        public List<PersonCommunication> Communications { get; set; }
        public void AddCommunication(PersonCommunication item)
        {
            item.PersonId = this.Id;
            this.Communications.Add(item);
        }

        public List<PersonContact> Contacts { get; set; }
        public void AddContact(PersonContact item)
        {
            item.PersonId = this.Id;
            this.Contacts.Add(item);
        }

        public List<PersonEntitlement> Entitlements { get; set; }
        public void AddEntitlement(PersonEntitlement item)
        {
            item.PersonId = this.Id;
            this.Entitlements.Add(item);
        }


        public string CurrentJudiciaryTypeCode { get; set; }
        public string CurrentIsSenior { get; set; }

        public string CurrentEntitlementCalcType { get; set; }
        public bool CurrentIsHours { get { return "H".Equals(CurrentEntitlementCalcType, StringComparison.OrdinalIgnoreCase); } }
    }
    public class PersonAddress
    {
        public int? Id { get; set; }
        public int? PersonId { get; set; }

        public string AddressTypeCode { get; set; }
        public string AddressTypeDescription { get; set; }

        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string PostalCode { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; }

        public int? CityId { get; set; }
        public string CityName { get; set; }

        public virtual string ProvinceCode { get; set; }
        public virtual string ProvinceDescription { get; set; }

        public int? CountryId { get; set; }
        public string CountryName { get; set; }

    }
    public class PersonContact
    {
        public int? Id { get; set; }
        public int? PersonId { get; set; }

        public string RelationshipCode { get; set; }
        public string RelationshipDescription { get; set; }

        public string ContactName { get; set; }
        public string PhoneNumber { get; set; }
    }
    public class PersonEntitlement
    {
        public int? Id { get; set; }
        public int? PersonId { get; set; }

        public double? Hours { get; set; }
        public double? Days { get; set; }

        public string Title { get; set; }

        public string EffectiveDate { get; set; }
        public string ExpiryDate { get; set; }

        public DateTime? GetEffectiveDate() 
        {
            try
            {
                return DateTime.ParseExact(EffectiveDate, Constants.DATE_FORMAT, CultureInfo.CurrentCulture);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public DateTime? GetExpiryDate()
        {
            try
            {
                return DateTime.ParseExact(ExpiryDate, Constants.DATE_FORMAT, CultureInfo.CurrentCulture);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
    public class PersonName : EffectiveRange
    {
        public int? Id { get; set; }
        public int? PersonId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Initials { get; set; }
    }
    public class PersonStatus : EffectiveRange
    {
        public int? Id { get; set; }
        public int? PersonId { get; set; }

        // position status...
        public int? PositionStatusId { get; set; }
        //... position type
        public int? PositionTypeId { get; set; }
        public string PositionCode { get; set; }
        public string PositionDescription { get; set; }
        //... judiciary type
        public string JudiciaryTypeCode { get; set; }
        public string JudiciaryTypeDescription { get; set; }
        //... status
        public string StatusCode { get; set; }
        public string StatusDescription { get; set; }

        public string InactiveReasonCode { get; set; }
        public string InactiveReasonDescription { get; set; }

        public string EntitlementCalcType { get; set; }
        public bool IsHours { get { return "H".Equals(EntitlementCalcType, StringComparison.OrdinalIgnoreCase); } }
    }
    public class PersonCommunication
    {
        public int? Id { get; set; }
        public int? PersonId { get; set; }
        public string CommunicationTypeCode { get; set; }
        public string CommunicationTypeDescription { get; set; }
        public string IdentifierText { get; set; }
        public bool IsActive { get; set; }
    }

    public class HomeLocation : EffectiveRange
    {
        public int? Id { get; set; }
        public int? UserId { get; set; }
        public int? PersonId { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
    }

    public interface IEffectiveRange
    {
        string EffectiveDate { get; set; }
        string ExpiryDate { get; set; }

        DateTime EffDate { get; }
        DateTime ExpDate { get; }

        bool IsEffective(DateTime date);

        void AddToList(List<IEffectiveRange> list);
    }

    public class EffectiveRange: IEffectiveRange
    {
        private string _effectiveDate;
        private string _expiryDate;
        private DateTime _effDate = DateTime.MinValue;
        private DateTime _expDate = DateTime.MaxValue;
        private bool _effectiveValid = false;

        //Disable the warning for now.
        #pragma warning disable 0414        
        private bool _expiryValid = false;
        #pragma warning restore 0414


        public string EffectiveDate 
        {
            get { return _effectiveDate; }
            set
            {
                _effDate = DateTime.MinValue;
                _effectiveValid = false;
                _effectiveDate = value;
                if (IsValidDateFormat(_effectiveDate))
                {
                    _effDate = GetDate(_effectiveDate, DateTime.MinValue).Value;
                    _effectiveValid = true;
                }
            }
        }
        public string ExpiryDate
        {
            get { return _expiryDate; }
            set
            {
                _expDate = DateTime.MaxValue;
                _expiryValid = false;
                _expiryDate = value;
                if (IsValidDateFormat(_expiryDate))
                {
                    _expDate = GetDate(_expiryDate, DateTime.MaxValue).Value;
                    _expiryValid = true;
                }
            }
        }


        public DateTime EffDate { get { return _effDate; } }
        public DateTime ExpDate { get { return _expDate; } }

        public bool IsEffective(DateTime date)
        {
            if (string.IsNullOrEmpty(EffectiveDate)) { return false; }
            return date.Date.Ticks >= EffDate.Ticks && date.Date.Ticks <= ExpDate.Ticks;
        }

        public void AddToList(List<IEffectiveRange> items)
        {
            if (!_effectiveValid)
                return;

            if (items.Count > 0)
            {
                //do we predate existing????
                DateTime minEff = items.Min(x => x.EffDate);
                if (this.EffDate.Ticks <= minEff.Ticks)
                {
                    items.Clear();
                }
                else
                {
                    IEffectiveRange eff = items.Find(x => x.IsEffective(this.EffDate));
                    DateTime exp = this.EffDate.Date.AddDays(-1);
                    eff.ExpiryDate = exp.ToString(Constants.DATE_FORMAT);
                }
            }
            this.ExpiryDate = null;
            items.Add(this);
        }

        public void PlaceInList(List<IEffectiveRange> items)
        {
            // only use for home locations...

            // find the exact place where this range item would fit in...
            if (!_effectiveValid)
                return;
            if (items.Count == 0)
            {
                items.Add(this);
            }
            else
            {
                // if this doesn't expire... wipe out any thing after this...
                if (string.IsNullOrEmpty(this._expiryDate))
                {
                    items.RemoveAll(x => x.EffDate >= this.EffDate);
                    // also, find the one that is effective when this starts and expire it...
                    IEffectiveRange eff = items.FirstOrDefault(x => x.IsEffective(this.EffDate));
                    if (eff != null)
                    {
                        DateTime exp = this.EffDate.Date.AddDays(-1);
                        eff.ExpiryDate = exp.ToString(Constants.DATE_FORMAT);
                    }
                }
                else
                {
                    // has effective and expiry...
                    // remove any that completely fall in the range
                    items.RemoveAll(x => x.EffDate.Date >= this.EffDate.Date && x.ExpDate.Date <= this.ExpDate.Date);


                    IEffectiveRange er = items.FirstOrDefault(x => x.IsEffective(this.EffDate));
                    if (er != null)
                    {
                        HomeLocation hl = (HomeLocation)er;
                        if (string.IsNullOrEmpty(er.ExpiryDate))
                        {
                            items.Add(new HomeLocation() { ExpiryDate = null, EffectiveDate = this.ExpDate.Date.AddDays(1).ToString(Constants.DATE_FORMAT), LocationId = hl.LocationId, UserId = hl.UserId });
                        }
                        else if (er.ExpDate.Date > this.ExpDate.Date)
                        {
                            items.Add(new HomeLocation() { ExpiryDate = er.ExpiryDate, EffectiveDate = this.ExpDate.Date.AddDays(1).ToString(Constants.DATE_FORMAT), LocationId = hl.LocationId, UserId = hl.UserId });
                        }

                        // make it end the day before this starts
                        DateTime d = this.EffDate.Date.AddDays(-1);
                        er.ExpiryDate = d.ToString(Constants.DATE_FORMAT);
                    }

                    er = items.FirstOrDefault(x => x.IsEffective(this.ExpDate));
                    if (er != null)
                    {
                        HomeLocation hl = (HomeLocation)er;
                        if (er.EffDate.Date < this.EffDate.Date)
                        {
                            items.Add(new HomeLocation() { ExpiryDate = this.EffDate.Date.AddDays(-1).ToString(Constants.DATE_FORMAT), EffectiveDate = er.EffectiveDate, LocationId = hl.LocationId, UserId = hl.UserId });
                        }

                        // make it start the day after this ends
                        DateTime d = this.ExpDate.Date.AddDays(1);
                        er.EffectiveDate = d.ToString(Constants.DATE_FORMAT);
                    }
                }
                items.Add(this);
            }
        }

        private DateTime? GetDate(string value, DateTime? defaultValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            try
            {
                return DateTime.ParseExact(value, Constants.DATE_FORMAT, CultureInfo.CurrentCulture);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        private bool IsValidDateFormat(string value)
        {
            if (string.IsNullOrEmpty(EffectiveDate))
            {
                return false;
            }
            if (GetDate(value, null) == null)
            {
                return false;
            }
            return true;
        }
    }


    #region PersonSearchItem

    public class PersonSearchItem
    {
        public int PersonId { get; set; }
        
        public int UserId { get; set; }
        public double? ParticipantId { get; set; }
        
        public int HomeLocationId { get; set; }
        public string HomeLocationName { get; set; }
        public string RegionCode { get; set; }
        public string RegionDescription { get; set; }
        public int WorkAreaSeqNo { get; set; }
        public string WorkAreaDescription { get; set; }

        public string RotaInitials { get; set; }

        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Initials { get; set; }
        public string FullName { get; set; }

        public string JudiciaryTypeCode { get; set; }
        public string JudiciaryTypeDescription { get; set; }
        public string PositionCode { get; set; }
        public string PositionDescription { get; set; }
        public string StatusCode { get; set; }
        public string StatusDescription { get; set; }

        public string InactiveReasonCode { get; set; }
        public string InactiveReasonDescription { get; set; }

        public bool IsNonStatus { get; set; }

        public string ScheduleGeneratedDate { get; set; }
        public string SchedulePublishedDate { get; set; }
    }
    #endregion

}