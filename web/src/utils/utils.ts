import { beautifyDate } from '@/filters';
import { ApplicationService } from '@/services/ApplicationService';
import { AuthService } from '@/services/AuthService';
import { LookupService } from '@/services/LookupService';
import { useCommonStore } from '@/stores';
import { CommonStore } from '@/stores/CommonStore';
import { civilAppearancesListType } from '@/types/civil';
import { civilApprDetailType } from '@/types/civil/jsonTypes';
import { CourtClassEnum, LookupCode } from '@/types/common';
import { criminalAppearancesListType } from '@/types/criminal';
import { criminalApprDetailType } from '@/types/criminal/jsonTypes';
import _ from 'underscore';
import { inject } from 'vue';
import { LocationQueryValue } from 'vue-router';

export const SessionManager = {
  getSettings: async function () {
    const commonStore = useCommonStore();
    const authService = inject<AuthService>('authService');
    const appService = inject<ApplicationService>('applicationService');

    try {
      const [userInfo, appInfo] = await Promise.all([
        authService?.getUserInfo(),
        appService?.getApplicationInfo(),
      ]);
      let succeeded = true;
      if (!userInfo) {
        console.error('User info not available.');
        succeeded = false;
      }
      if (!appInfo) {
        console.error('Application info not available.');
        succeeded = false;
      }
      commonStore.setUserInfo(userInfo ?? null);
      commonStore.appInfo = appInfo ?? null;
      return succeeded;
    } catch (error) {
      console.log(error);
      return false;
    }
  },
};

export const splunkLog = (message) => {
  // TODO: This has to be refactored to use a better way to call Splunk via REST API
  console.log(message);
  // const token = import.meta.env["SPLUNK_TOKEN"] || ""
  // const url = import.meta.env["SPLUNK_COLLECTOR_URL"] || ""

  // if (token && url) {
  //   const config = {
  //     token: token,
  //     url: url
  //   }

  //   const Logger = new SplunkLogger(config)
  //   const payload = {
  //     message: message
  //   }

  //   Logger.send(payload, (err, resp, body) => {
  //     console.log("Response from Splunk", body)
  //   })
  // }
};

export const getSingleValue = (value: string | string[]): string => {
  return Array.isArray(value) ? value[0] : value;
};

export const fetchStoreData = (store, methodName, data) => {
  store[methodName](data);
  return store[methodName.replace('update', '').toLowerCase()];
};

const getStatusStyle = (status, commonStore) =>
  fetchStoreData(commonStore, 'updateStatusStyle', status);

const getNameOfParticipant = (lastName, givenName, commonStore) =>
  fetchStoreData(commonStore, 'updateDisplayName', {
    lastName: lastName,
    givenName: givenName,
  });

const getTime = (time, commonStore) =>
  fetchStoreData(commonStore, 'updateTime', time);

const getDuration = (hr, min, commonStore) =>
  fetchStoreData(commonStore, 'updateDuration', { hr: hr, min: min });

enum appearanceStatus {
  UNCF = 'Unconfirmed',
  CNCL = 'Canceled',
  SCHD = 'Scheduled',
}

// This helper function is created to resolve duplication errors found by SonarCloud.
// It might be better to put this logic as part of the Parent component and pass it as prop
export const extractCriminalAppearanceInfo = (
  jApp: criminalApprDetailType,
  index: number,
  appearanceDate: string,
  commonStore: CommonStore
): criminalAppearancesListType => {
  const appInfo: criminalAppearancesListType = {
    index: index.toString(),
    date: appearanceDate,
    formattedDate: beautifyDate(appearanceDate),
    time: getTime(jApp.appearanceTm.split(' ')[1].substring(0, 5), commonStore),
    reason: jApp.appearanceReasonCd,
    reasonDescription: jApp.appearanceReasonDsc ?? '',
    duration: getDuration(
      jApp.estimatedTimeHour,
      jApp.estimatedTimeMin,
      commonStore
    ),
    location: jApp.courtLocation ?? '',
    room: jApp.courtRoomCd,
    firstName: jApp.givenNm || '',
    lastName: jApp.lastNm || jApp.orgNm,
    accused: getNameOfParticipant(
      jApp.lastNm || jApp.orgNm,
      jApp.givenNm || '',
      commonStore
    ),
    status: jApp.appearanceStatusCd
      ? appearanceStatus[jApp.appearanceStatusCd]
      : '',
    statusStyle: getStatusStyle(
      jApp.appearanceStatusCd ? appearanceStatus[jApp.appearanceStatusCd] : '',
      commonStore
    ),
    presider: jApp.judgeInitials || '',
    judgeFullName: jApp.judgeInitials ? jApp.judgeFullNm : '',
    appearanceId: jApp.appearanceId,
    partId: jApp.partId,
    supplementalEquipment: jApp.supplementalEquipmentTxt,
    securityRestriction: jApp.securityRestrictionTxt,
    outOfTownJudge: jApp.outOfTownJudgeTxt,
    profSeqNo: jApp.profSeqNo,
  };

  return appInfo;
};

export const extractCivilAppearancesInfo = (
  jApp: civilApprDetailType,
  index: number,
  commonStore: CommonStore
): civilAppearancesListType => {
  const date = jApp.appearanceDt.split(' ')[0];
  const status = jApp.appearanceStatusCd
    ? appearanceStatus[jApp.appearanceStatusCd]
    : '';

  const appInfo: civilAppearancesListType = {
    index: index.toString(),
    date,
    formattedDate: beautifyDate(date),
    documentType: jApp.documentTypeDsc || '',
    result: jApp.appearanceResultCd,
    resultDescription: jApp.appearanceResultDsc || '',
    time: getTime(jApp.appearanceTm.split(' ')[1].substring(0, 5), commonStore),
    reason: jApp.appearanceReasonCd,
    reasonDescription: jApp.appearanceReasonDsc || '',
    duration: getDuration(
      jApp.estimatedTimeHour,
      jApp.estimatedTimeMin,
      commonStore
    ),
    location: jApp.courtLocation || '',
    room: jApp.courtRoomCd,
    status,
    statusStyle: getStatusStyle(status, commonStore),
    presider: jApp.judgeInitials ? jApp.judgeInitials : '',
    judgeFullName: jApp.judgeInitials ? jApp.judgeFullNm : '',
    appearanceId: jApp.appearanceId,
    supplementalEquipment: jApp.supplementalEquipmentTxt,
    securityRestriction: jApp.securityRestrictionTxt,
    outOfTownJudge: jApp.outOfTownJudgeTxt,
  };

  return appInfo;
};

/**
 * Formats a full name by combining the last name and given name.
 *
 * @param lastName - The last name of the individual. Defaults to an empty string if not provided.
 * @param givenName - The given (first) name of the individual. Defaults to an empty string if not provided.
 * @returns A formatted string in the format "LastName, GivenName" if both names are provided,
 *          otherwise returns the last name only.
 */
export const formatToFullName = (lastName = '', givenName = ''): string =>
  lastName && givenName ? `${lastName}, ${givenName}` : lastName;

/**
 * Formats a full name into a "LastName, GivenNames" format.
 *
 * @param fullName - The full name to format, consisting of given names and a last name.
 * @returns A formatted string in the "LastName, GivenNames" format.
 *          If the input is empty or invalid, returns an empty string.
 */
export const formatFromFullname = (fullName: string): string => {
  if (!fullName) return '';
  const nameParts = fullName.split(' ');
  const lastName = nameParts.pop() ?? '';
  const givenNames = nameParts.join(' ');
  let name = lastName;

  return lastName.trim() && givenNames.trim() ? name + `, ${givenNames}` : name;
};

/**
 * Retrieves the list of roles, either from the common store if already available,
 * or by fetching them using the lookup service. The roles are then stored in the
 * common store for future use.
 *
 * @returns {Promise<LookupCode[] | undefined>} A promise that resolves to an array of roles
 * (of type `LookupCode[]`) or `undefined` if the lookup service is unavailable.
 *
 * @remarks
 * - This function uses the `useCommonStore` to access and update the roles in the common store.
 * - It relies on dependency injection to obtain the `lookupService` for fetching role types.
 */
export const getRoles = async (): Promise<LookupCode[] | undefined> => {
  const commonStore = useCommonStore();
  const lookupService = inject<LookupService>('lookupService');
  const roles = commonStore.roles.length
    ? commonStore.roles
    : await lookupService?.getRoles();

  commonStore.setRoles(roles);

  return roles;
};

/**
 * Retrieves the short description of a lookup code from a list of lookup codes.
 *
 * @param code - The code to search for in the list of lookup codes.
 * @param lookupCodes - An array of lookup codes to search within.
 * @returns The short description (`shortDesc`) of the matching lookup code, or `undefined` if no match is found.
 */
export const getLookupShortDescription = (
  code: string,
  lookupCodes: LookupCode[]
) => lookupCodes?.find((role) => role.code === code)?.shortDesc;

/**
 * Retrieves the name of an enum based from its value.
 * @param enumObj The enum
 * @param value Value of the enum member
 * @returns Name of enum member
 */
export const getEnumName = <T extends Record<string | number, string | number>>(
  enumObj: T,
  value: string | number
): string => enumObj[value] as string;

/**
 * Retrieves the CSS class name to represent a CourtClass
 * @param courtClassCd The court class code
 * @returns class name
 */
export const getCourtClassStyle = (courtClassCd: string): string => {
  switch (courtClassCd) {
    case getEnumName(CourtClassEnum, CourtClassEnum.A):
    case getEnumName(CourtClassEnum, CourtClassEnum.Y):
    case getEnumName(CourtClassEnum, CourtClassEnum.T):
      return 'criminal';
    case getEnumName(CourtClassEnum, CourtClassEnum.C):
    case getEnumName(CourtClassEnum, CourtClassEnum.L):
    case getEnumName(CourtClassEnum, CourtClassEnum.M):
      return 'small-claims';
    case getEnumName(CourtClassEnum, CourtClassEnum.F):
      return 'family';
    default:
      return 'unknown';
  }
};

/**
 * Retrieves the label for the associated court-class
 * @param courtClassCd The court class code
 * @returns court class label
 */
export const getCourtClassLabel = (courtClassCd: string): string => {
  switch (courtClassCd) {
    case getEnumName(CourtClassEnum, CourtClassEnum.A):
      return 'Criminal - Adult';
    case getEnumName(CourtClassEnum, CourtClassEnum.Y):
      return 'Youth';
    case getEnumName(CourtClassEnum, CourtClassEnum.T):
      return 'Tickets';
    case getEnumName(CourtClassEnum, CourtClassEnum.C):
    case getEnumName(CourtClassEnum, CourtClassEnum.L):
    case getEnumName(CourtClassEnum, CourtClassEnum.M):
      return 'Small Claims';
    case getEnumName(CourtClassEnum, CourtClassEnum.F):
      return 'Family';
    default:
      return 'Unknown';
  }
};

/**
 * Determines if the provided court class label corresponds to a criminal court class.
 * @param courtClassLabel The court class label
 * @returns Whether the court class is criminal
 */
export const isCourtClassLabelCriminal = (courtClassLabel: string): boolean => {
  switch (courtClassLabel) {
    case 'Criminal - Adult':
    case 'Youth':
    case 'Tickets':
      return true;
    default:
      return false;
  }
};

/**
 * Parses QueryString to string
 * @param value The query string param
 * @param fallback The fallback param when parsing fails
 * @returns string
 */
export const parseQueryStringToString = (
  value: LocationQueryValue | LocationQueryValue[] | null | undefined,
  fallback = ''
): string => {
  if (Array.isArray(value)) {
    return value[0] ?? fallback;
  }
  return value ?? fallback;
};

export const isPositiveInteger = (value) => {
  return _.isNumber(value) && value > 0;
};
