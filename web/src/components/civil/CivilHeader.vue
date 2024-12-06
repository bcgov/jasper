<template>
  <b-card no-body>
    <b-navbar
      type="white"
      variant="white"
      v-if="isMounted"
      style="height: 45px"
    >
      <b-navbar-nav>
        <b-nav-text class="mr-2" style="margin-top: 6px; font-size: 14px">
          <b-icon icon="file-earmark-text"></b-icon>
          <span :style="getActivityClass()" class="file-number-txt">
            {{ fileNumberText }}</span
          >
        </b-nav-text>

        <b-nav-text class="mt-2 ml-1 mr-2" style="font-size: 11px">
          {{ agencyLocation.name }}
          <span v-if="agencyLocation.code"> ({{ agencyLocation.code }}) </span>
        </b-nav-text>

        <b-nav-text
          class="text-muted mr-3"
          style="margin-top: 8px; font-size: 11px"
        >
          {{ agencyLocation.region }}
        </b-nav-text>

        <b-dropdown class="mt-1 mr-2" no-caret right variant="white">
          <template v-slot:button-content>
            <b-button
              variant="outline-primary text-info"
              style="
                transform: translate(0, -4px);
                border: 0px;
                font-size: 14px;
                text-overflow: ellipsis;
              "
              v-b-tooltip.hover.bottomleft
              :title="partyDisplayedTxt"
              size="sm"
            >
              <b-icon class="mr-2" icon="person-fill"></b-icon>
              <b style="text-overflow: ellipsis">
                {{ getNameOfPartyTrunc() }}
              </b>
              <b-icon
                class="ml-1"
                icon="caret-down-fill"
                font-scale="1"
              ></b-icon>
            </b-button>
          </template>
          <b-dropdown-item-button
            disabled
            v-for="leftParty in leftPartiesInfo"
            v-bind:key="leftParty.index"
            >{{ leftParty.name }}</b-dropdown-item-button
          >
          <b-dropdown-divider></b-dropdown-divider>
          <b-dropdown-item-button
            disabled
            v-for="rightParty in rightPartiesInfo"
            v-bind:key="rightParty.index"
            >{{ rightParty.name }}</b-dropdown-item-button
          >
        </b-dropdown>

        <b-nav-text style="margin-top: 4px; font-size: 12px" variant="white">
          <b-badge pill variant="danger">{{
            adjudicatorRestrictionsInfo.length
          }}</b-badge>
        </b-nav-text>

        <b-nav-item-dropdown
          right
          no-caret
          :disabled="adjudicatorRestrictionsInfo.length == 0"
          v-if="userInfo.userType != 'vc'"
        >
          <template v-slot:button-content>
            <b-button
              :variant="
                adjudicatorRestrictionsInfo.length > 0
                  ? 'outline-primary text-info'
                  : 'white'
              "
              :disabled="adjudicatorRestrictionsInfo.length == 0"
              style="
                transform: translate(-5px, 0);
                border: 0px;
                font-size: 12px;
                text-overflow: ellipsis;
              "
              size="sm"
            >
              Adjudicator Restrictions
              <b-icon
                v-if="adjudicatorRestrictionsInfo.length > 0"
                class="ml-1"
                icon="caret-down-fill"
                font-scale="1"
              ></b-icon>
            </b-button>
          </template>

          <b-dropdown-item-button
            v-for="(restriction, index) in adjudicatorRestrictionsInfo"
            :key="index"
          >
            <b-button
              style="font-size: 12px; padding: 5px 5px"
              variant="secondary"
              v-b-tooltip.hover.left
              :title="restriction.fullName"
            >
              {{ restriction.adjRestriction }}
            </b-button>
          </b-dropdown-item-button>
        </b-nav-item-dropdown>

        <b-nav-text
          v-if="sheriffComment.length > 0"
          style="margin-top: 4px; font-size: 12px"
          variant="white"
        >
          <b-badge pill variant="danger">1</b-badge>
        </b-nav-text>
        <b-nav-item-dropdown v-if="sheriffComment.length > 0" right no-caret>
          <template v-slot:button-content>
            <b-button
              variant="outline-primary text-info"
              style="
                transform: translate(-5px, 0);
                border: 0px;
                font-size: 12px;
                text-overflow: ellipsis;
              "
              size="sm"
            >
              Sheriff Comments
              <b-icon
                class="ml-1"
                icon="caret-down-fill"
                font-scale="1"
              ></b-icon>
            </b-button>
          </template>

          <b-dropdown-item-button>
            <b-card bg-variant="white" no-body border-variant="white">
              {{ sheriffComment }}
            </b-card>
          </b-dropdown-item-button>
        </b-nav-item-dropdown>

        <b-nav-text style="margin-top: 4px; font-size: 12px" variant="white">
          <b-badge v-if="isSealed" variant="danger">Sealed</b-badge>
        </b-nav-text>
      </b-navbar-nav>
    </b-navbar>
    <hr class="mx-1 bg-warning" style="border-top: 2px double #fcba19" />
  </b-card>
</template>

<script lang="ts">
  import { useCivilFileStore, useCommonStore } from '@/stores';
  import { partiesInfoType } from '@/types/civil';
  import { AdjudicatorRestrictionsInfoType } from '@/types/common';
  import { defineComponent, onMounted, ref } from 'vue';

  export default defineComponent({
    setup() {
      const commonStore = useCommonStore();
      const civilFileStore = useCivilFileStore();

      const maximumFullNameLength = 15;
      const fileNumberText = ref('');
      const sheriffComment = ref('');
      let activityClassCode;
      const agencyLocation = ref({ name: '', code: '0', region: '' });
      const isMounted = ref(false);
      const isSealed = ref(false);
      const partyDisplayedTxt = ref('');
      const leftPartiesInfo = ref<partiesInfoType[]>([]);
      const rightPartiesInfo = ref<partiesInfoType[]>([]);
      const adjudicatorRestrictionsInfo = ref<
        AdjudicatorRestrictionsInfoType[]
      >([]);
      const activityClassCodeMapping = {
        S: 'color: #21B851;',
        R: 'color: #17A5E7;',
        M: 'color: #DF882A;',
        I: 'color: #A22BB9;',
        F: 'color: #21B851;',
        SIT: 'color: #d33;',
        NS: 'color: #999;',
      };

      onMounted(() => {
        getHeaderInfo();
      });

      const getHeaderInfo = () => {
        const data = civilFileStore.civilFileInformation.detailsData;
        fileNumberText.value = data.fileNumberTxt;
        activityClassCode = data.activityClassCd;
        agencyLocation.value.name = data.homeLocationAgencyName;
        agencyLocation.value.code = data.homeLocationAgencyCode;
        agencyLocation.value.region = data.homeLocationRegionName;
        partyDisplayedTxt.value = data.socTxt;
        sheriffComment.value = data.sheriffCommentText
          ? data.sheriffCommentText
          : '';
        isSealed.value = civilFileStore.civilFileInformation.isSealed;
        leftPartiesInfo.value =
          civilFileStore.civilFileInformation.leftPartiesInfo;
        rightPartiesInfo.value =
          civilFileStore.civilFileInformation.rightPartiesInfo;
        adjudicatorRestrictionsInfo.value =
          civilFileStore.civilFileInformation.adjudicatorRestrictionsInfo;
        isMounted.value = true;
      };

      const getNameOfPartyTrunc = () => {
        if (partyDisplayedTxt.value) {
          let firstParty = partyDisplayedTxt.value.split('/')[0]
            ? partyDisplayedTxt.value.split('/')[0].trim()
            : '';
          let secondParty = partyDisplayedTxt.value.split('/')[1]
            ? partyDisplayedTxt.value.split('/')[1].trim()
            : '';

          if (firstParty.length > maximumFullNameLength)
            firstParty =
              firstParty.substring(0, maximumFullNameLength) + ' ...';

          if (secondParty.length > maximumFullNameLength)
            secondParty =
              secondParty.substring(0, maximumFullNameLength) + ' ...';

          return firstParty + ' / ' + secondParty;
        } else {
          return '';
        }
      };

      const getActivityClass = () => {
        return activityClassCodeMapping[activityClassCode];
      };
      return {
        isMounted,
        getActivityClass,
        fileNumberText,
        agencyLocation,
        partyDisplayedTxt,
        getNameOfPartyTrunc,
        leftPartiesInfo,
        rightPartiesInfo,
        adjudicatorRestrictionsInfo,
        userInfo: commonStore.userInfo,
        sheriffComment,
        isSealed,
      };
    },
  });
</script>

<style scoped>
  .file-number-txt {
    font-weight: bold;
  }
</style>
