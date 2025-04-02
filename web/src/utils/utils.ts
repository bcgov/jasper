import { beautifyDate } from '@/filters';
import { AuthService } from '@/services/AuthService';
import { useCommonStore } from '@/stores';
import { CommonStore } from '@/stores/CommonStore';
import { civilAppearancesListType } from '@/types/civil';
import { civilApprDetailType } from '@/types/civil/jsonTypes';
import { criminalAppearancesListType } from '@/types/criminal';
import { criminalApprDetailType } from '@/types/criminal/jsonTypes';
import { inject } from 'vue';

export const SessionManager = {
  getSettings: async function () {
    const commonStore = useCommonStore();
    const authService = inject<AuthService>('authService');

    if (commonStore.userInfo) {
      return true;
    }

    try {
      const userInfo = await authService?.getUserInfo();
      if (!userInfo) {
        console.error('User info not available.');
        return false;
      }
      commonStore.userInfo = userInfo;
      return true;
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
 * Formats a given date string into the format "DD-MMM-YYYY".
 *
 * @param dateString - The date string to format. It should be a valid date string.
 * @returns {string} A formatted date string in "DD-MMM-YYYY" format (e.g., "01-Jan-2023").
 */
export const formatDateToDDMMMYYYY = (dateString: string): string => {
  const date = dateString ? new Date(dateString) : null;
  if (!date) {
    return '';
  }

  const day = new Intl.DateTimeFormat('en', { day: '2-digit' }).format(date);
  const month = new Intl.DateTimeFormat('en', { month: 'short' }).format(date);
  const year = new Intl.DateTimeFormat('en', { year: 'numeric' }).format(date);

  return `${day}-${month}-${year}`;
};
