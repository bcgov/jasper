<template>
  <v-data-table
    v-model="selectedItems"
    items-per-page="100"
    :headers
    :items="courtList"
    :item-key="idSelector"
  />
</template>

<script setup lang="ts">
  import { beautifyDate } from '@/filters';
  import { HttpService } from '@/services/HttpService';
  import { useCommonStore, useCourtListStore } from '@/stores';
  import { IconInfoType } from '@/types/common';
  import { civilListInfoType, courtListInfoType } from '@/types/courtlist';
  import {
    civilCourtListType,
    criminalCourtListType,
  } from '@/types/courtlist/jsonTypes';
  import { DocumentData } from '@/types/shared';
  import { inject, onMounted, ref } from 'vue';

  enum HearingType {
    'A' = '+',
    'G' = '@',
    'D' = '-',
    'S' = '*',
  }

  let civilCourtListJson: civilCourtListType[] = [];
  let criminalCourtListJson: criminalCourtListType[] = [];
  const courtList = ref<courtListInfoType[]>([]);
  const fields = ref<any>([]);
  const courtRoom = ref('');
  const isMounted = ref(false);
  const isDataReady = ref(false);
  const selectedItems = ref<[]>([]);
  const idSelector = ref('appearanceId');
  const physicalIds = ref<string[]>([]);
  const referenceDocs = ref<any>([]);
  const commonStore = useCommonStore();
  const courtListStore = useCourtListStore();
  const httpService = inject<HttpService>('httpService');

  if (!httpService) {
    throw new Error('HttpService is undefined');
  }

  const headers = [
    { key: 'data-table-group' },
    {
      key: 'fileNumber',
      title: 'File #',
    },
    {
      key: 'partiesDesc',
      title: 'Accused / Parties',
    },
    {
      key: 'time',
      title: 'Time',
    },
    {
      key: 'est',
      title: 'Est.',
    },
    {
      key: 'room',
      title: 'Room',
    },
    {
      key: 'reason',
      title: 'Reason',
    },
    {
      key: 'fileMakers',
      title: 'File Markers',
    },
    {
      key: 'counselDesc',
      title: 'Counsel',
    },
    {
      key: 'crown',
      title: 'Crown',
    },
    {
      key: 'caseAge',
      title: 'Case Age',
    },
  ];

  // Fetch data on mount
  onMounted(() => {
    getCourtList();
  });

  const getCourtList = () => {
    const data = courtListStore.courtListInformation.detailsData;
    civilCourtListJson = [...data.civilCourtList];
    courtRoom.value = data.courtRoomCode;
    ExtractCivilListInfo();
    criminalCourtListJson = data.criminalCourtList;
    ExtractCriminalListInfo();
    BuildReferenceDocsList();
    fields.value = JSON.parse(JSON.stringify(headers));
    if (criminalCourtListJson.length == 0) {
      fields.value.splice(4, 1);
    }
    if (civilCourtListJson.length == 0) {
      fields.value.splice(3, 1);
    }
    if (courtList.value.length) {
      console.log(courtList.value);
      isDataReady.value = true;
    }
    isMounted.value = true;
  };

  const ExtractCriminalListInfo = () => {
    for (const criminalListIndex in criminalCourtListJson) {
      const criminalListInfo = {} as courtListInfoType;
      const jcriminalList = criminalCourtListJson[criminalListIndex];

      criminalListInfo.index = criminalListIndex;

      criminalListInfo.seq = jcriminalList.appearanceSequenceNumber
        ? parseInt(jcriminalList.appearanceSequenceNumber)
        : 0;
      criminalListInfo.fileNumber = jcriminalList.fileNumberText;
      criminalListInfo.tag =
        criminalListInfo.fileNumber + '-' + criminalListInfo.seq;

      criminalListInfo.icons = [];
      const iconInfo: IconInfoType[] = [];
      let iconExists = false;
      if (jcriminalList.appearanceStatusCd) {
        iconInfo.push({ info: jcriminalList.appearanceStatusCd, desc: '' });
        iconExists = true;
      }
      if (jcriminalList.video) {
        iconInfo.push({ info: 'Video', desc: '' });
        iconExists = true;
      }
      if (jcriminalList.fileHomeLocationName) {
        iconInfo.push({
          info: 'Home',
          desc: jcriminalList.fileHomeLocationName,
        });
        iconExists = true;
      }
      if (iconExists) {
        commonStore.updateIconStyle(iconInfo);
        criminalListInfo.icons = commonStore.iconStyles;
      }
      criminalListInfo.caseAge = jcriminalList.caseAgeDaysNumber
        ? jcriminalList.caseAgeDaysNumber
        : '';
      criminalListInfo.time = getTime(
        jcriminalList.appearanceTime.split(' ')[1].substr(0, 5)
      );

      criminalListInfo.room = courtRoom.value;

      const accusedName = getNameOfAccusedTrunc(jcriminalList.accusedFullName);
      criminalListInfo.accused = accusedName.name;
      criminalListInfo.accusedTruncApplied = accusedName.trunc;
      criminalListInfo.accusedDesc = jcriminalList.accusedFullName;

      criminalListInfo.reason = jcriminalList.appearanceReasonCd;
      criminalListInfo.reasonDesc = jcriminalList.appearanceReasonDesc;

      criminalListInfo.counsel = jcriminalList.counselFullName;

      criminalListInfo.crown = '';
      criminalListInfo.crownDesc = '';
      if (jcriminalList.crown && jcriminalList.crown.length > 0) {
        let firstCrownSet = false;
        for (const crown of jcriminalList.crown) {
          if (crown.assigned) {
            if (!firstCrownSet) {
              criminalListInfo.crown = crown.fullName;
              firstCrownSet = true;
            } else {
              criminalListInfo.crownDesc += crown.fullName + ', ';
            }
          }
        }

        if (criminalListInfo.crownDesc)
          criminalListInfo.crownDesc += criminalListInfo.crown;
      }
      criminalListInfo.est = getDuration(
        jcriminalList.estimatedTimeHour,
        jcriminalList.estimatedTimeMin
      );
      criminalListInfo.partId = jcriminalList.fileInformation.partId;
      criminalListInfo.justinNo = jcriminalList.fileInformation.mdocJustinNo;
      criminalListInfo.appearanceId = jcriminalList.criminalAppearanceID;

      criminalListInfo.fileMarkers = [];
      if (jcriminalList.inCustody) {
        criminalListInfo.fileMarkers.push({
          abbr: 'IC',
          key: 'In Custody',
        });
      }
      if (jcriminalList.otherFileInformationText) {
        criminalListInfo.fileMarkers.push({
          abbr: 'OTH',
          key: jcriminalList.otherFileInformationText,
        });
      }
      if (jcriminalList.detained) {
        criminalListInfo.fileMarkers.push({
          abbr: 'DO',
          key: 'Detention Order',
        });
      }

      criminalListInfo.hearingRestrictions = [];
      for (const hearingRestriction of jcriminalList.hearingRestriction) {
        const marker =
          hearingRestriction.adjInitialsText +
          HearingType[hearingRestriction.hearingRestrictiontype];
        const markerDesc =
          hearingRestriction.judgeName +
          ' (' +
          hearingRestriction.hearingRestrictionTypeDesc +
          ')';
        criminalListInfo.hearingRestrictions.push({
          abbr: marker,
          key: markerDesc,
        });
      }
      criminalListInfo.trialNotes = jcriminalList.trialRemarkTxt;

      criminalListInfo.trialRemarks = [];
      if (jcriminalList.trialRemark) {
        for (const trialRemark of jcriminalList.trialRemark) {
          criminalListInfo.trialRemarks.push({
            txt: trialRemark.commentTxt,
          });
        }
      }
      criminalListInfo.notes = {
        remarks: criminalListInfo.trialRemarks,
        text: criminalListInfo.trialNotes,
      };
      criminalListInfo.supplementalEquipment =
        jcriminalList.supplementalEquipment;
      criminalListInfo.securityRestriction = jcriminalList.securityRestriction;
      criminalListInfo.outOfTownJudge = jcriminalList.outOfTownJudge;

      criminalListInfo.courtLevel = jcriminalList.fileInformation.courtLevelCd;
      criminalListInfo.courtClass = jcriminalList.fileInformation.courtClassCd;
      criminalListInfo.profSeqNo = jcriminalList.fileInformation.profSeqNo;
      criminalListInfo.noteExist = isCriminalNoteAvailable(criminalListInfo);
      criminalListInfo.listClass = 'criminal';
      courtList.value.push(criminalListInfo);
    }
  };

  const isCriminalNoteAvailable = (criminalListInfo) => {
    if (criminalListInfo.trialRemarks.length > 0) return true;
    if (criminalListInfo.trialNotes) return true;
    return false;
  };

  const getNameOfAccusedTrunc = (nameOfAccused) => {
    const maximumFullNameLength = 20;
    if (nameOfAccused.length > maximumFullNameLength)
      return {
        name: nameOfAccused.substr(0, maximumFullNameLength) + ' ... ',
        trunc: true,
      };
    else return { name: nameOfAccused, trunc: false };
  };

  const ExtractCivilListInfo = () => {
    const familyListClass = ['F', 'E'];
    const civilListClass = ['I', 'B', 'V', 'D', 'H', 'P', 'S'];
    /* 
      Unfortunately these don't follow the usual pattern of the other lookups.
      B = "Bankruptcy"
      V = "Caveat"
      D = "Divorce"
      E = "Family Law Proceeding"
      H = "Forclosure"
      P = "Probate"
      S = "Supreme Civil (General)"
      Future:
      SCH – Supreme Court Chambers
      SCV – Supreme Court Trial List
  */

    for (const civilListIndex in civilCourtListJson) {
      const civilListInfo = {} as civilListInfoType;
      const jcivilList = civilCourtListJson[civilListIndex];
      civilListInfo.index = civilListIndex;
      if (familyListClass.indexOf(jcivilList.activityClassCd) != -1) {
        civilListInfo.listClass = 'family';
      } else if (civilListClass.indexOf(jcivilList.activityClassCd) != -1) {
        civilListInfo.listClass = 'civil';
      }

      civilListInfo.seq = jcivilList.courtListPrintSortNumber
        ? parseInt(jcivilList.courtListPrintSortNumber)
        : 0;

      civilListInfo.fileNumber = jcivilList.physicalFile.fileNumber;
      civilListInfo.tag = civilListInfo.fileNumber + '-' + civilListInfo.seq;
      civilListInfo.icons = [];
      const iconInfo: IconInfoType[] = [];
      let iconExists = false;
      if (jcivilList.appearanceStatusCd) {
        iconInfo.push({ info: jcivilList.appearanceStatusCd, desc: '' });
        iconExists = true;
      }
      if (jcivilList.video) {
        iconInfo.push({ info: 'Video', desc: '' });
        iconExists = true;
      }
      if (iconExists) {
        commonStore.updateIconStyle(iconInfo);
        civilListInfo.icons = commonStore.iconStyles;
      }

      civilListInfo.time = getTime(jcivilList.appearanceTime.substr(0, 5));
      civilListInfo.room = courtRoom.value;
      const partyNames = getNameOfPartyTrunc(jcivilList.sealFileSOCText);
      civilListInfo.parties = partyNames.name;
      civilListInfo.partiesTruncApplied = partyNames.trunc;
      civilListInfo.partiesDesc = jcivilList.sealFileSOCText;
      civilListInfo.reason = jcivilList.appearanceReasonCd;
      civilListInfo.reasonDesc = jcivilList.appearanceReasonDesc;
      civilListInfo.est = getDuration(
        jcivilList.estimatedTimeHour,
        jcivilList.estimatedTimeMin
      );

      civilListInfo.supplementalEquipment = jcivilList.supplementalEquipment;
      civilListInfo.securityRestriction = jcivilList.securityRestriction;
      civilListInfo.outOfTownJudge = jcivilList.outOfTownJudge;
      civilListInfo.counsel = '';
      civilListInfo.counselDesc = '';

      let firstCounselSet = false;
      for (const party of jcivilList.parties) {
        for (const counsel of party.counsel) {
          if (!firstCounselSet) {
            civilListInfo.counsel = counsel.counselFullName;
            firstCounselSet = true;
          } else {
            civilListInfo.counselDesc += counsel.counselFullName + ',\n ';
          }
        }
      }
      if (civilListInfo.counselDesc)
        civilListInfo.counselDesc += civilListInfo.counsel;

      civilListInfo.fileId = jcivilList.physicalFile.physicalFileID;
      physicalIds.value.push(jcivilList.physicalFile.physicalFileID);
      civilListInfo.appearanceId = jcivilList.appearanceId;

      civilListInfo.fileMarkers = [];
      if (jcivilList.cfcsaFile) {
        civilListInfo.fileMarkers.push({
          abbr: 'CFCSA',
          key: 'Child, Family and Community Service Act',
        });
      }

      civilListInfo.hearingRestrictions = [];
      for (const hearingRestriction of jcivilList.hearingRestriction) {
        const marker =
          hearingRestriction.adjInitialsText +
          HearingType[hearingRestriction.hearingRestrictiontype];
        const markerDesc =
          hearingRestriction.judgeName +
          ' (' +
          hearingRestriction.hearingRestrictionTypeDesc +
          ')';

        civilListInfo.hearingRestrictions.push({
          abbr: marker,
          key: markerDesc,
        });
      }

      civilListInfo.notes = {
        TrialNotes: jcivilList.trialRemarkTxt,
        FileComment: jcivilList.fileCommentText,
        CommentToJudge: jcivilList.commentToJudgeText,
        SheriffComment: jcivilList.sheriffCommentText,
      };
      civilListInfo.noteExist = isNoteAvailable(civilListInfo);
      courtList.value.push(civilListInfo);
    }
  };

  const isNoteAvailable = (civilListInfo) => {
    if (
      civilListInfo.notes.TrialNotes ||
      civilListInfo.notes.FileComment ||
      civilListInfo.notes.CommentToJudge ||
      civilListInfo.notes.SheriffComment
    )
      return true;
    else return false;
  };

  const getNameOfPartyTrunc = (partyNames) => {
    const maximumFullNameLength = 15;
    let truncApplied = false;
    if (partyNames) {
      let firstParty = partyNames.split('/')[0].trim();
      let secondParty = partyNames.split('/')[1].trim();

      if (firstParty.length > maximumFullNameLength) {
        firstParty = firstParty.substr(0, maximumFullNameLength) + ' ...';
        truncApplied = true;
      }

      if (secondParty.length > maximumFullNameLength) {
        secondParty = secondParty.substr(0, maximumFullNameLength) + ' ...';
        truncApplied = true;
      }

      return {
        name: firstParty + ' / ' + secondParty,
        trunc: truncApplied,
      };
    } else {
      return { name: '', trunc: truncApplied };
    }
  };

  const getTime = (time) => {
    return time;
  };

  const getDuration = (hr, min) => {
    commonStore.updateDuration({ hr: hr, min: min });
    return commonStore.duration;
  };

  const BuildReferenceDocsList = () => {
    const promises: Promise<any>[] = [];
    for (const physicalId of physicalIds.value) {
      promises.push(getReferenceDocs(physicalId));
    }

    Promise.all(promises).then((values) => {
      referenceDocs.value = [...values];
    });
  };

  const getReferenceDocs = (fileId: string): Promise<any> => {
    return new Promise<any>((resolve) => {
      httpService
        .get<any>(`api/files/civil/${fileId}`)
        .then((Response) => Response)
        .then((data) => {
          const detailsData = data;
          const documents = data.referenceDocument;
          const documentsData: DocumentData[] = [];

          for (const docIndex in documents) {
            const doc = documents[docIndex];

            const documentData: DocumentData = {
              appearanceDate: beautifyDate(doc.AppearanceDate),
              courtClass: detailsData.courtClassCd,
              courtLevel: detailsData.courtLevelCd,
              documentId: doc.ObjectGuid,
              documentDescription: doc.ReferenceDocumentTypeDsc,
              fileId: fileId,
              fileNumberText: detailsData.fileNumberTxt,
              partyName: doc.ReferenceDocumentInterest.map(
                (pn) => pn.partyName
              ),
              location: detailsData.homeLocationAgencyName,
            };

            documentsData.push(documentData);
          }
          resolve({
            fileId: fileId,
            doc: documentsData,
          });
        });
    });
  };
</script>
