import {
  ApplicationInfo,
  CourtRoomsJsonInfoType,
  IconStyleType,
  LookupCode,
  UserInfo,
} from '@/types/common';
import { defineStore } from 'pinia';

enum appearanceStatus {
  UNCF = 'Unconfirmed',
  CNCL = 'Canceled',
  SCHD = 'Scheduled',
}

export const useCommonStore = defineStore('CommonStore', {
  state: () => ({
    displayName: '',
    userInfo: null as UserInfo | null,
    appInfo: null as ApplicationInfo | null,
    time: '',
    duration: '',
    statusStyle: '',
    iconStyles: [] as IconStyleType[],
    courtRoomsAndLocations: [] as CourtRoomsJsonInfoType[],
    roles: [] as LookupCode[],
    enableArchive: false,
    email: '',
  }),
  actions: {
    setUserInfo(userInfo): void {
      this.userInfo = userInfo;
    },
    updateUserInfo(newUserInfo): void {
      this.setUserInfo(newUserInfo);
    },
    setCourtRoomsAndLocations(courtRoomsAndLocations): void {
      this.courtRoomsAndLocations = courtRoomsAndLocations;
    },
    updateCourtRoomsAndLocations(newCourtRoomsAndLocations) {
      this.setCourtRoomsAndLocations(newCourtRoomsAndLocations);
    },
    setRoles(roles): void {
      this.roles = roles;
    },
    updateRoles(newRoles): void {
      this.setRoles(newRoles);
    },
    setDisplayName(displayName): void {
      this.displayName = displayName;
    },
    updateDisplayName(inputNames): void {
      let newDisplayName = '';
      if (inputNames.lastName.length == 0) {
        newDisplayName = inputNames.givenName;
      } else if (inputNames.givenName.length == 0) {
        newDisplayName = inputNames.lastName;
      } else {
        newDisplayName =
          inputNames.lastName.charAt(0).toUpperCase() +
          inputNames.lastName.slice(1).toLowerCase() +
          ', ' +
          inputNames.givenName.charAt(0).toUpperCase() +
          inputNames.givenName.slice(1).toLowerCase();
      }
      this.setDisplayName(newDisplayName);
    },
    formatDisplayName(newDisplayName): void {
      this.setDisplayName(
        newDisplayName.charAt(0).toUpperCase() +
          newDisplayName.slice(1).toLowerCase()
      );
    },
    setTime(time): void {
      this.time = time;
    },
    updateTime(time) {
      const time12 = (Number(time.substr(0, 2)) % 12 || 12) + time.substr(2, 3);

      if (Number(time.substr(0, 2)) < 12) {
        this.setTime(time12 + ' AM');
      } else {
        this.setTime(time12 + ' PM');
      }
    },
    setDuration(duration): void {
      this.duration = duration;
    },
    updateDuration(newDuration) {
      let duration = '';
      if (newDuration.hr) {
        if (Number(newDuration.hr) == 1) duration += '1 Hr ';
        else if (Number(newDuration.hr) > 1)
          duration += Number(newDuration.hr) + ' Hrs ';
      }

      if (newDuration.min) {
        if (Number(newDuration.min) == 1) duration += '1 Min ';
        else if (Number(newDuration.min) > 1)
          duration += Number(newDuration.min) + ' Mins ';
      }

      this.setDuration(duration);
    },
    setStatusStyle(statusStyle): void {
      this.statusStyle = statusStyle;
    },
    updateStatusStyle(status) {
      let style = '';
      if (status == appearanceStatus.UNCF) {
        style = 'badge badge-danger mt-2';
      } else if (status == appearanceStatus.CNCL) {
        style = 'badge badge-warning mt-2';
      } else if (status == appearanceStatus.SCHD) {
        style = 'badge badge-primary mt-2';
      }

      this.setStatusStyle(style);
    },
    setIconStyle(iconStyles): void {
      this.iconStyles = iconStyles;
    },
    updateIconStyle(newIconsInfo) {
      const iconStyles: IconStyleType[] = [];
      for (const iconInfo of newIconsInfo) {
        if (iconInfo['info'] == 'UNCF') {
          iconStyles.push({ icon: 'circle-half', desc: appearanceStatus.UNCF });
        } else if (iconInfo['info'] == 'CNCL') {
          iconStyles.push({ icon: 'trash', desc: appearanceStatus.CNCL });
        } else if (iconInfo['info'] == 'SCHD') {
          iconStyles.push({ icon: 'calendar', desc: appearanceStatus.SCHD });
        } else if (iconInfo['info'] == 'Video') {
          iconStyles.push({ icon: 'camera-video-fill', desc: 'video' });
        } else if (iconInfo['info'] == 'Home') {
          iconStyles.push({ icon: 'house-door-fill', desc: iconInfo['desc'] });
        }
      }
      this.setIconStyle(iconStyles);
    },
    setEnableArchive(newEnableArchive): void {
      this.enableArchive = newEnableArchive;
    },
    UpdateEnableArchive(newEnableArchive): void {
      this.setEnableArchive(newEnableArchive);
    },
  },
});

export type CommonStore = ReturnType<typeof useCommonStore>;
