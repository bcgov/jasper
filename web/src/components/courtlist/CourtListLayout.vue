<template>
  <b-card bg-variant="white" no-body>
    <b-card bg-variant="light" v-if="!isMounted && !isDataReady">
      <b-overlay :show="true">
        <b-card style="min-height: 100px" />
        <template v-slot:overlay>
          <div>
            <loading-spinner />
            <p id="loading-label">Loading ...</p>
          </div>
        </template>
      </b-overlay>
    </b-card>

    <b-card
      bg-variant="white"
      v-if="isDataReady"
      no-body
      class="mx-3"
      style="overflow: auto"
    >
      <b-table
        :items="SortedCourtList"
        :fields="fields"
        borderless
        small
        responsive="sm"
      >
        <template v-slot:head()="data">
          <b> {{ data.label }}</b>
        </template>

        <template v-slot:cell()="data">
          <b-badge :style="data.field.cellStyle" variant="white">
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(fileNumber)="data">
          <b-button
            :style="data.field.cellStyle"
            size="sm"
            :id="data.item.listClass + 'case-' + data.item.tag"
            :href="'#' + data.item.listClass + 'case-' + data.item.tag"
            @click="
              data.item.listClass == 'criminal'
                ? OpenCriminalDetails(data)
                : OpenCivilDetails(data);
              data.toggleDetails();
            "
            :variant="
              'outline-primary border-white text-' + data.item.listClass
            "
            class="mr-2"
          >
            <b-icon-caret-right-fill
              :variant="data.item.listClass"
              v-if="!data.item['_showDetails']"
            ></b-icon-caret-right-fill>
            <b-icon-caret-down-fill
              :variant="data.item.listClass"
              v-if="data.item['_showDetails']"
            ></b-icon-caret-down-fill>
            {{ data.value }}
          </b-button>
        </template>

        <template v-slot:row-details="data">
          <b-card
            v-if="data.item.listClass == 'criminal'"
            no-body
            bg-border="dark"
          >
            <criminal-appearance-details
              :tagcasename="'criminalcase-' + data.item.tag"
            />
          </b-card>
          <b-card v-else no-body bg-border="dark">
            <civil-appearance-details
              :tagcasename="data.item.listClass + 'case-' + data.item.tag"
            />
          </b-card>
        </template>

        <template v-slot:cell(reason)="data">
          <b-badge
            variant="secondary"
            v-b-tooltip.hover.right
            :title="data.item.reasonDesc"
            :style="data.field.cellStyle"
          >
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(accused)="data">
          <b-button
            v-if="data.item.listClass == 'criminal'"
            :style="data.field.cellStyle"
            size="sm"
            @click="OpenCriminalFilePage(data)"
            v-b-tooltip.hover.right
            :title="
              data.item['accusedTruncApplied'] ? data.item['accusedDesc'] : null
            "
            variant="outline-primary border-white text-criminal"
            class="mr-2"
          >
            {{ data.value }}
          </b-button>
        </template>

        <template v-slot:cell(parties)="data">
          <b-button
            v-if="data.item.listClass != 'criminal' && data.value.length > 0"
            :style="data.field.cellStyle"
            size="sm"
            @click="OpenCivilFilePage(data, 'All Documents')"
            v-b-tooltip.hover.right
            :title="
              data.item.partiesTruncApplied ? data.item.partiesDesc : null
            "
            :variant="
              'outline-primary border-white text-' + data.item.listClass
            "
            class="mr-2"
          >
            {{ data.value }}
          </b-button>
          <b-button
            v-else-if="
              data.item.listClass != 'criminal' && data.value.length == 0
            "
            :style="data.field.cellStyle"
            size="sm"
            @click="OpenCivilFilePage(data, 'All Documents')"
            v-b-tooltip.hover.right
            :title="
              data.item.partiesTruncApplied ? data.item.partiesDesc : null
            "
            :variant="
              'outline-primary border-white text-' + data.item.listClass
            "
            class="mr-2"
          >
            File
          </b-button>
        </template>

        <template v-slot:cell(time)="data">
          <div :style="data.field.cellStyle">
            {{ convertTime(data.value) }}
          </div>
        </template>

        <template v-slot:cell(providedDocuments)="data">
          <div class="text-center">
            <b-button
              v-if="countReferenceDocs(data) == 1"
              :style="data.field.cellStyle"
              size="sm"
              :variant="
                'outline-primary border-white text-' + data.item.listClass
              "
              class="mr-2"
              @click="downloadProvidedDocument(data)"
            >
              <b-icon-file-earmark-text
                :variant="data.item.listClass"
              ></b-icon-file-earmark-text>
            </b-button>
            <b-button
              v-else-if="countReferenceDocs(data) > 1"
              :style="data.field.cellStyle"
              size="sm"
              :variant="
                'outline-primary border-white text-' + data.item.listClass
              "
              class="mr-2"
              @click="OpenCivilFilePage(data, 'Provided Documents')"
            >
              <b-icon-files :variant="data.item.listClass"></b-icon-files>
            </b-button>
          </div>
        </template>

        <template v-slot:cell(counsel)="data">
          <div
            v-if="data.item.listClass == 'criminal'"
            :style="data.field.cellStyle"
          >
            {{ data.value }}
          </div>
          <b-badge
            v-if="data.item.listClass != 'criminal' && data.item.counselDesc"
            variant="white text-success"
            v-b-tooltip.hover.left.html="getFullCounsel(data.item.counselDesc)"
            :style="data.field.cellStyle"
          >
            {{ data.value }}
          </b-badge>
          <b-badge
            v-else-if="
              data.item.listClass != 'criminal' && !data.item.counselDesc
            "
            variant="white"
            :style="data.field.cellStyle"
          >
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(crown)="data">
          <b-badge
            v-if="data.item.listClass == 'criminal' && data.item.crownDesc"
            variant="white text-success"
            v-b-tooltip.hover.left
            :title="data.item.crownDesc"
            :style="data.field.cellStyle"
          >
            {{ data.value }}
          </b-badge>
          <b-badge
            v-else-if="
              data.item.listClass == 'criminal' && !data.item.crownDesc
            "
            variant="white"
            :style="data.field.cellStyle"
          >
            {{ data.value }}
          </b-badge>
        </template>

        <template v-slot:cell(fileMarkers)="data">
          <b-badge
            v-for="(field, index) in data.value"
            :key="index"
            class="mr-1"
            :style="data.field.cellStyle"
            v-b-tooltip.hover.right
            :title="field.key"
          >
            {{ field.abbr }}
          </b-badge>
        </template>

        <template v-slot:cell(hearingRestrictions)="data">
          <b-badge
            v-for="(field, index) in data.value"
            :key="index"
            class="mr-1"
            :style="data.field.cellStyle"
            v-b-tooltip.hover.right
            :title="field.key"
          >
            {{ field.abbr }}
          </b-badge>
        </template>

        <template v-slot:cell(notes)="data">
          <b-button
            v-if="data.item.noteExist"
            size="sm"
            :style="data.field.cellStyle"
            @click="OpenNotes(data.value)"
            variant="outline-primary border-white text-info"
            class="mt-1"
            v-b-tooltip.hover.left
            title="Notes"
          >
            <b-icon icon="chat-square-fill" font-scale="1.5"></b-icon>
          </b-button>
        </template>
      </b-table>
    </b-card>

    <b-modal
      v-if="isMounted"
      v-model="showNotes"
      id="bv-modal-notes"
      hide-footer
    >
      <template v-slot:modal-title>
        <h2 class="mb-0">Notes</h2>
      </template>
      <b-card
        v-if="notes.trialNotes"
        title="Trial Notes"
        border-variant="white"
      >
        {{ notes.trialNotes }}
      </b-card>

      <b-card
        v-if="notes.fileComment"
        title="File Comment"
        border-variant="white"
      >
        {{ notes.fileComment }}
      </b-card>

      <b-card
        v-if="notes.commentToJudge"
        title="Comment To Judge"
        border-variant="white"
      >
        {{ notes.commentToJudge }}
      </b-card>

      <b-card
        v-if="notes.sheriffComment"
        title="Sheriff Comment"
        border-variant="white"
      >
        {{ notes.sheriffComment }}
      </b-card>

      <b-card v-if="notes.text" title="Trial Notes" border-variant="white">
        {{ notes.text }}
      </b-card>
      <!-- As per Kevin's request SCV-140, hide JCM Notes. 
            <b-card 
                v-if="notes.remarks.length>0" 
                title="Crown Notes to JCM" 
                border-variant="white">
                    <b-table        
                        borderless
                        :items="notes.remarks"                                    
                        thead-class="d-none"
                        responsive="sm"          
                        striped
                        >
                    </b-table>
               
            </b-card>             
            -->

      <!--<b-button class="mt-3 bg-info" @click="$bvModal.hide('bv-modal-notes')">Close</b-button>-->
    </b-modal>
  </b-card>
</template>

<script lang="ts">
  import {
    useCivilFileStore,
    useCommonStore,
    useCourtListStore,
    useCriminalFileStore,
  } from '@/stores';
  import { IconInfoType } from '@/types/common';
  import { civilListInfoType, courtListInfoType } from '@/types/courtlist';
  import { criminalFileInformationType } from '@/types/criminal';
  import { CourtDocumentType, DocumentData } from '@/types/shared';
  import * as _ from 'underscore';
  import { defineComponent, inject, onMounted, ref } from 'vue';

  import { beautifyDate, convertTime } from '@/filters';
  import { HttpService } from '@/services/HttpService';
  import { useRouter } from 'vue-router';

  import {
    civilCourtListType,
    criminalCourtListType,
  } from '@/types/courtlist/jsonTypes';
  import shared from '../shared';

  enum HearingType {
    'A' = '+',
    'G' = '@',
    'D' = '-',
    'S' = '*',
  }

  export default defineComponent({
    setup() {
      let civilCourtListJson: civilCourtListType[] = [];
      let criminalCourtListJson: criminalCourtListType[] = [];
      const courtList = ref<courtListInfoType[]>([]);
      const fields = ref<any>([]);
      const courtRoom = ref('');
      const isMounted = ref(false);
      const isDataReady = ref(false);
      const showNotes = ref(false);

      const notes = {
        remarks: [],
        text: '',
        trialNotes: '',
        fileComment: '',
        commentToJudge: '',
        sheriffComment: '',
      };
      const physicalIds = ref<string[]>([]);
      const referenceDocs = ref<any>([]);

      const router = useRouter();

      const commonStore = useCommonStore();
      const courtListStore = useCourtListStore();
      const criminalFileStore = useCriminalFileStore();
      const civilFileStore = useCivilFileStore();
      const httpService = inject<HttpService>('httpService');

      if (!httpService) {
        throw new Error('HttpService is undefined');
      }

      const initialFields = [
        {
          key: 'seq',
          label: 'Seq.',
          tdClass: 'border-top',
          cellStyle: 'font-weight: normal; font-size: 16px; padding-top:8px',
        },
        {
          key: 'providedDocuments',
          label: 'Prov. Docs',
          tdClass: 'border-top',
          cellStyle: 'margin-top: 3px; font-size: 16px; font-weight:normal;',
        },
        {
          key: 'fileNumber',
          label: 'File Number',
          tdClass: 'border-top',
          cellStyle: 'font-size:16px',
        },
        {
          key: 'parties',
          label: 'Parties',
          tdClass: 'border-top',
          cellStyle: 'font-size:16px; font-weight: bold;',
        },
        {
          key: 'accused',
          label: 'Accused',
          tdClass: 'border-top',
          cellStyle: 'font-size:16px; font-weight: bold;',
        },

        {
          key: 'time',
          label: 'Time',
          tdClass: 'border-top',
          cellStyle: 'margin-top: 3px;',
        },
        {
          key: 'est',
          label: 'Est.',
          tdClass: 'border-top',
          cellStyle: 'font-weight: normal; font-size: 16px; padding-top:8px',
        },
        {
          key: 'reason',
          label: 'Reason',
          tdClass: 'border-top',
          cellStyle: 'margin-top: 6px; font-size: 14px;',
        },
        {
          key: 'room',
          label: 'Room',
          tdClass: 'border-top text-primary',
          cellStyle: 'font-weight: normal; font-size: 16px; padding-top:9px',
        },
        {
          key: 'counsel',
          label: 'Counsel',
          tdClass: 'border-top',
          cellStyle: 'margin-top: 3px; font-size: 16px; font-weight:normal;',
        },
        {
          key: 'fileMarkers',
          label: 'File Markers',
          tdClass: 'border-top',
          cellStyle: 'margin-top: 6px; font-weight: normal; font-size: 14px;',
        },
        {
          key: 'hearingRestrictions',
          label: 'Hearing Restrictions',
          tdClass: 'border-top',
          cellStyle: 'margin-top: 6px; font-weight: normal; font-size: 14px;',
        },
        {
          key: 'crown',
          label: 'Crown',
          tdClass: 'border-top',
          cellStyle: 'margin-top: 4px; font-size: 16px; font-weight:normal;',
        },
        {
          key: 'caseAge',
          label: 'Case Age',
          tdClass: 'border-top',
          cellStyle: 'font-weight: normal; font-size: 16px; padding-top:9px',
        },
        {
          key: 'notes',
          label: 'Notes',
          tdClass: 'border-top',
          cellStyle: 'font-size:12px; border:0px;',
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
        fields.value = JSON.parse(JSON.stringify(initialFields));
        if (criminalCourtListJson.length == 0) {
          fields.value.splice(4, 1);
        }
        if (civilCourtListJson.length == 0) {
          fields.value.splice(3, 1);
        }
        if (courtList.value.length) {
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

          const accusedName = getNameOfAccusedTrunc(
            jcriminalList.accusedFullName
          );
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
          criminalListInfo.justinNo =
            jcriminalList.fileInformation.mdocJustinNo;
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
          criminalListInfo.securityRestriction =
            jcriminalList.securityRestriction;
          criminalListInfo.outOfTownJudge = jcriminalList.outOfTownJudge;

          criminalListInfo.courtLevel =
            jcriminalList.fileInformation.courtLevelCd;
          criminalListInfo.courtClass =
            jcriminalList.fileInformation.courtClassCd;
          criminalListInfo.profSeqNo = jcriminalList.fileInformation.profSeqNo;
          criminalListInfo.noteExist =
            isCriminalNoteAvailable(criminalListInfo);
          criminalListInfo.listClass = 'criminal';
          courtList.value.push(criminalListInfo);
        }
      };

      const isCriminalNoteAvailable = (criminalListInfo) => {
        if (criminalListInfo.trialRemarks.length > 0) return true;
        if (criminalListInfo.trialNotes) return true;
        return false;
      };

      const OpenCriminalNotes = (notesData) => {
        notes.text = notesData;
        showNotes.value = true;
      };

      const getNameOfParticipant = (lastName, givenName) => {
        commonStore.updateDisplayName({
          lastName: lastName,
          givenName: givenName,
        });
        return commonStore.displayName;
      };

      const OpenCriminalFilePage = (data) => {
        const fileInformation = {} as criminalFileInformationType;
        fileInformation.fileNumber = data.item.justinNo;
        criminalFileStore.updateCriminalFile(fileInformation);
        const routeData = router.resolve({
          name: 'CriminalCaseDetails',
          params: { fileNumber: fileInformation.fileNumber },
        });
        window.open(routeData.href, '_blank');
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
          civilListInfo.tag =
            civilListInfo.fileNumber + '-' + civilListInfo.seq;
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

          civilListInfo.supplementalEquipment =
            jcivilList.supplementalEquipment;
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
      const OpenNotes = (notesData) => {
        notes.text = notesData;
        showNotes.value = true;
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

      const OpenCriminalDetails = (data) => {
        if (!data.detailsShowing) {
          criminalFileStore.criminalAppearanceInfo = data;
          // criminalFileStore.criminalAppearanceInfo.fileNo = data.item.justinNo;
          // criminalFileStore.criminalAppearanceInfo.appearanceId = data.item.appearanceId;
          // criminalFileStore.criminalAppearanceInfo.partId = data.item.partId;
          // criminalFileStore.criminalAppearanceInfo.supplementalEquipmentTxt = data.item.supplementalEquipment;
          // criminalFileStore.criminalAppearanceInfo.securityRestrictionTxt = data.item.securityRestriction;
          // criminalFileStore.criminalAppearanceInfo.outOfTownJudgeTxt = data.item.outOfTownJudge;
          // criminalFileStore.criminalAppearanceInfo.courtLevel = data.item.courtLevel;
          // criminalFileStore.criminalAppearanceInfo.courtClass = data.item.courtClass;
          // criminalFileStore.criminalAppearanceInfo.profSeqNo = data.item.profSeqNo;
          criminalFileStore.updateCriminalAppearanceInfo(
            criminalFileStore.criminalAppearanceInfo
          );
        }
      };

      const OpenCivilDetails = (data) => {
        if (!data.detailsShowing) {
          civilFileStore.civilAppearanceInfo.fileNo = data.item.fileId;
          civilFileStore.civilAppearanceInfo.appearanceId =
            data.item.appearanceId;
          civilFileStore.civilAppearanceInfo.supplementalEquipmentTxt =
            data.item.supplementalEquipment;
          civilFileStore.civilAppearanceInfo.securityRestrictionTxt =
            data.item.securityRestriction;
          civilFileStore.civilAppearanceInfo.outOfTownJudgeTxt =
            data.item.outOfTownJudge;
          civilFileStore.updateCivilAppearanceInfo(
            civilFileStore.civilAppearanceInfo
          );
        }
      };

      const countReferenceDocs = (data) => {
        if (data?.item?.fileId) {
          const target = referenceDocs.value.find(
            (doc) => doc['fileId'] === data.item.fileId
          );
          return target && target['doc'] ? target['doc'].length : 0;
        }
        return 0;
      };

      const downloadProvidedDocument = (data) => {
        const target = referenceDocs.value.find(
          (rd) => rd.fileId === data.item.fileId
        );
        shared.openDocumentsPdf(CourtDocumentType.ProvidedCivil, target.doc[0]);
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

      const OpenCivilFilePage = (data, section = '') => {
        let params;
        if (section) {
          params = {
            fileNumber: data.item.fileId,
            section: section,
          };
        } else {
          params = { fileNumber: data.item.fileId };
        }

        const routeData = router.resolve({
          name: 'CivilCaseDetails',
          params: params,
        });
        window.open(routeData.href, '_blank');
      };

      const getFullCounsel = (counselDesc) => {
        return '<b style="white-space: pre-line;">' + counselDesc + '</b>';
      };

      const SortedCourtList = () => {
        // TODO: sort by appearance time
        return _.sortBy(courtList.value, 'time');
      };
      // Return the reactive variables to the template
      return {
        civilCourtListJson,
        criminalCourtListJson,
        courtList,
        fields,
        courtRoom,
        isMounted,
        isDataReady,
        showNotes,
        notes,
        physicalIds,
        referenceDocs,
        SortedCourtList,
        getFullCounsel,
        OpenCivilFilePage,
        downloadProvidedDocument,
        countReferenceDocs,
        getNameOfParticipant,
        OpenCriminalNotes,
        OpenNotes,
        OpenCivilDetails,
        OpenCriminalDetails,
        OpenCriminalFilePage,
        convertTime,
      };
    },
  });
</script>

<style scoped>
  .card {
    border: white;
  }
</style>
