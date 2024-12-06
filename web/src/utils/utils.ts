import { beautifyDate } from '@/filters';
import { AuthService } from '@/services/AuthService';
import { useCommonStore } from '@/stores';
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

// This helper function is created to solve duplication errors found by SonarCloud
// Include this as part of refactoring efforts once the pages is re-written
export const extractCriminalAppearanceInfo = (
  jApp: criminalApprDetailType,
  index: number,
  appearanceDate: string
): criminalAppearancesListType => {
  enum appearanceStatus {
    UNCF = 'Unconfirmed',
    CNCL = 'Canceled',
    SCHD = 'Scheduled',
  }

  const commonStore = useCommonStore();

  const getStatusStyle = (status) =>
    fetchStoreData(commonStore, 'updateStatusStyle', status);

  const getNameOfParticipant = (lastName, givenName) =>
    fetchStoreData(commonStore, 'updateDisplayName', {
      lastName: lastName,
      givenName: givenName,
    });

  const getTime = (time) => fetchStoreData(commonStore, 'updateTime', time);

  const getDuration = (hr, min) =>
    fetchStoreData(commonStore, 'updateDuration', { hr: hr, min: min });

  const appInfo: criminalAppearancesListType = {
    index: index.toString(),
    date: appearanceDate,
    formattedDate: beautifyDate(appearanceDate),
    time: getTime(jApp.appearanceTm.split(' ')[1].substring(0, 5)),
    reason: jApp.appearanceReasonCd,
    reasonDescription: jApp.appearanceReasonDsc ?? '',
    duration: getDuration(jApp.estimatedTimeHour, jApp.estimatedTimeMin),
    location: jApp.courtLocation ?? '',
    room: jApp.courtRoomCd,
    firstName: jApp.givenNm || '',
    lastName: jApp.lastNm || jApp.orgNm,
    accused: getNameOfParticipant(
      jApp.lastNm || jApp.orgNm,
      jApp.givenNm || ''
    ),
    status: jApp.appearanceStatusCd
      ? appearanceStatus[jApp.appearanceStatusCd]
      : '',
    statusStyle: getStatusStyle(
      jApp.appearanceStatusCd ? appearanceStatus[jApp.appearanceStatusCd] : ''
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
