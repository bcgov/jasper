import { SESSION_STATES } from '@/constants/sessionStates';
import { beautifyDate } from '@/filters';
import { ApplicationService } from '@/services/ApplicationService';
import { AuthService } from '@/services/AuthService';
import { LookupService } from '@/services/LookupService';
import redirectHandlerService from '@/services/RedirectHandlerService';
import { UserService } from '@/services/UserService';
import { useCommonStore } from '@/stores';
import { CommonStore } from '@/stores/CommonStore';
import { getApiErrorResponse } from '@/types/ApiResponse';
import { civilAppearancesListType } from '@/types/civil';
import { civilApprDetailType } from '@/types/civil/jsonTypes';
import {
  CourtClassEnum,
  CourtRoomsJsonInfoType,
  LookupCode,
  UserRecord,
} from '@/types/common';
import { criminalAppearancesListType } from '@/types/criminal';
import { criminalApprDetailType } from '@/types/criminal/jsonTypes';
import _ from 'underscore';
import { inject } from 'vue';
import { LocationQueryValue } from 'vue-router';

const sessionInitializations = new WeakMap<CommonStore, Promise<void>>();

const isValidJudgeId = (id: number | null | undefined): id is number =>
  typeof id === 'number' && Number.isInteger(id) && id > 0;

export function useSessionInitializer() {
  const commonStore = useCommonStore();
  const authService = inject<AuthService>('authService');
  const appService = inject<ApplicationService>('applicationService');
  const userService = inject<UserService>('userService');

  async function loadSession() {
    const previousUser = commonStore.userInfo;
    const previousLogin = commonStore.loggedInUserInfo;
    commonStore.clearSession(SESSION_STATES.INITIALIZING);

    try {
      if (!authService || !userService || !appService) {
        throw new Error('Session services are unavailable.');
      }

      const authUser = await authService.getUserInfo();
      if (commonStore.sessionState !== SESSION_STATES.INITIALIZING) return;
      if (!authUser || typeof authUser.isActive !== 'boolean') {
        throw new Error('Authentication information is unavailable.');
      }

      let user: UserRecord;
      try {
        user = await userService.getMyUser({ sessionInitialization: true });
      } catch (error) {
        const status = getApiErrorResponse(error)?.status;
        if (status !== 400 && status !== 404) throw error;
        if (commonStore.sessionState === SESSION_STATES.INITIALIZING) {
          redirectHandlerService.clearAttempt();
          commonStore.clearSession(SESSION_STATES.ACCESS_REQUIRED);
        }
        return;
      }

      if (commonStore.sessionState !== SESSION_STATES.INITIALIZING) return;
      if (
        !user ||
        typeof user.id !== 'string' ||
        !user.id.trim() ||
        user.id !== authUser.userId ||
        typeof user.isActive !== 'boolean' ||
        !Array.isArray(user.roles) ||
        !user.roles.every((role) => typeof role === 'string' && role.trim())
      ) {
        throw new Error('Current user information is incomplete.');
      }

      const userInfo = {
        ...authUser,
        ...user,
        email: authUser.email || user.email || '',
        judgeId: 0,
        permissions: Array.isArray(user.permissions) ? user.permissions : [],
      };

      if (!user.isActive || user.roles.length === 0) {
        redirectHandlerService.clearAttempt();
        commonStore.$patch({
          userInfo,
          sessionState: SESSION_STATES.ACCESS_REQUIRED,
        });
        return;
      }

      // Eligibility comes from the database. The server supplies the default judge context.
      const judgeId = isValidJudgeId(user.judgeId)
        ? user.judgeId
        : authUser.judgeId;
      if (!isValidJudgeId(judgeId)) {
        throw new Error(
          'No valid judge context is available for the current user.'
        );
      }
      const currentUserInfo = { ...userInfo, judgeId };

      if (
        previousUser?.userId === user.id &&
        previousLogin?.userId === user.id &&
        previousLogin.judgeId === authUser.judgeId &&
        isValidJudgeId(previousUser.judgeId) &&
        previousUser.judgeId !== authUser.judgeId &&
        previousUser.judgeId !== judgeId
      ) {
        currentUserInfo.judgeId = previousUser.judgeId;
        currentUserInfo.judgeHomeLocationId = previousUser.judgeHomeLocationId;
      }

      const appInfo = await appService.getApplicationInfo();
      if (commonStore.sessionState !== SESSION_STATES.INITIALIZING) return;
      if (!appInfo || typeof appInfo.version !== 'string') {
        throw new Error('Application information is unavailable.');
      }

      redirectHandlerService.clearAttempt();
      commonStore.$patch({
        loggedInUserInfo: authUser,
        userInfo: currentUserInfo,
        appInfo,
        sessionState: SESSION_STATES.AUTHORIZED,
      });
    } catch (error) {
      if (commonStore.sessionState !== SESSION_STATES.INITIALIZING) return;
      const status = getApiErrorResponse(error)?.status;
      if (
        redirectHandlerService.isRedirecting ||
        ((status === 401 || status === 409) &&
          redirectHandlerService.trySessionRedirect(status))
      ) {
        return;
      }
      commonStore.clearSession(SESSION_STATES.FAILED);
      redirectHandlerService.markInitializationFailed();
    }
  }

  return function initializeSessionSettings(): Promise<void> {
    const pending = sessionInitializations.get(commonStore);
    if (pending) return pending;

    const task = loadSession().finally(() => {
      sessionInitializations.delete(commonStore);
    });
    sessionInitializations.set(commonStore, task);
    return task;
  };
}

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
  const name = lastName;

  return lastName.trim() && givenNames.trim() ? name + `, ${givenNames}` : name;
};

export const arrayBufferToBase64 = (buffer: ArrayBuffer): string => {
  const bytes = new Uint8Array(buffer);
  let binary = '';
  for (let i = 0; i < bytes.byteLength; i++) {
    binary += String.fromCodePoint(bytes[i]);
  }
  return btoa(binary);
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

export const hasPermission = (permission: string): boolean => {
  const commonStore = useCommonStore();
  return !!commonStore.userInfo?.permissions?.includes(permission);
};

export const hasRole = (role: string | string[]): boolean => {
  const commonStore = useCommonStore();
  const roles = Array.isArray(role) ? role : [role];
  return roles.some((r) => !!commonStore.userInfo?.roles?.includes(r));
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

/**
 * Finds a court location by id first, since it is the most reliable, then falls back to name.
 * @param locations The court locations to search.
 * @param id The location id to match.
 * @param name The location name to match when the id does not match.
 * @returns The matching location, or undefined if none matches.
 */
export const resolveCourtLocation = (
  locations: CourtRoomsJsonInfoType[],
  id?: string,
  name?: string
): CourtRoomsJsonInfoType | undefined =>
  (id ? locations.find((l) => l.locationId === id) : undefined) ??
  (name ? locations.find((l) => l.name === name) : undefined);

/**
 * Maps a known activity class description to its corresponding CSS class name.
 * Styles can be found in /public/styles/common.css
 * @param activityClassDescription The activity class description to map.
 * @returns The CSS class name for the given description, or an empty string if the description is unknown.
 */
export const cleanActivityClassDescription = (
  activityClassDescription: string | null | undefined
): string => {
  switch (activityClassDescription?.trim()) {
    case 'Civil':
      return 'civil';
    case 'Small Claims':
      return 'small-claims';
    case 'Criminal':
      return 'criminal';
    case 'Family':
      return 'family';
    case 'Mixed':
      return 'mixed';
    case 'Non-Sitting':
      return 'non-sitting';
    case 'Judge-Sitting':
      return 'judge-sitting';
    case 'Specialty':
      return 'specialty';
    default:
      return '';
  }
};
