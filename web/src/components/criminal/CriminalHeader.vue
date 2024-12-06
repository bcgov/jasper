<template>
  <div>
    <b-navbar
      type="white"
      variant="white"
      v-if="isMounted"
      style="height: 45px"
    >
      <b-navbar-nav>
        <b-nav-text
          class="text-primary mr-2"
          style="margin-top: 6px; font-size: 12px"
        >
          <b-icon icon="file-earmark-text"></b-icon>
          {{ fileNumberText }}
        </b-nav-text>

        <b-nav-text class="mt-2 ml-1 mr-2" style="font-size: 11px">
          {{ agencyLocation.name }}
          <span v-if="agencyLocation.code"> ({{ agencyLocation.code }}) </span>
        </b-nav-text>

        <b-nav-text class="text-muted mr-3 mt-2" style="font-size: 11px">
          {{ agencyLocation.region }}
        </b-nav-text>

        <b-nav-item-dropdown class="mr-3 mt-1" right no-caret size="sm">
          <template v-slot:button-content>
            <b-button
              variant="outline-primary text-info"
              style="
                transform: translate(0, -4px);
                border: 0px;
                font-size: 14px;
              "
              size="sm"
            >
              <b-icon class="mr-2" icon="person-fill"></b-icon>
              <b>{{ getNameOfParticipantTrunc }}</b> and
              {{ participantList.length - 1 }} other(s)
              <b-icon
                class="ml-1"
                icon="caret-down-fill"
                font-scale="1"
              ></b-icon>
            </b-button>
          </template>
          <b-dropdown-item-button
            v-for="participant in SortedParticipants"
            :key="participant.index"
            v-on:click="setActiveParticipantIndex(participant.index)"
            >{{ participant.name }}</b-dropdown-item-button
          >
        </b-nav-item-dropdown>

        <b-nav-text style="margin-top: 4px; font-size: 12px" variant="white">
          <b-badge pill variant="danger">{{
            adjudicatorRestrictions.length
          }}</b-badge>
        </b-nav-text>

        <b-nav-item-dropdown
          right
          no-caret
          :disabled="adjudicatorRestrictions.length == 0"
        >
          <template v-slot:button-content>
            <b-button
              :variant="
                adjudicatorRestrictions.length > 0
                  ? 'outline-primary text-info'
                  : 'white'
              "
              :disabled="adjudicatorRestrictions.length == 0"
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
                v-if="adjudicatorRestrictions.length > 0"
                class="ml-1"
                icon="caret-down-fill"
                font-scale="1"
              ></b-icon>
            </b-button>
          </template>

          <b-dropdown-item-button
            v-for="(restriction, index) in adjudicatorRestrictions"
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
          v-if="bans.length > 0"
          style="margin-top: 4px; font-size: 12px"
          variant="white"
        >
          <b-badge pill variant="danger">{{ bans.length }}</b-badge>
        </b-nav-text>

        <b-nav-item-dropdown v-if="bans.length > 0" right no-caret>
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
              Ban Details
              <b-icon
                class="ml-1"
                icon="caret-down-fill"
                font-scale="1"
              ></b-icon>
            </b-button>
          </template>

          <b-dropdown-item-button>
            <b-card bg-variant="white" no-body border-variant="white">
              <b-table
                borderless
                :items="bans"
                :fields="fields"
                small
                responsive="sm"
              >
                <template v-slot:cell()="data">
                  <span style="transform: translate(0, +6px)">{{
                    data.value
                  }}</span>
                </template>
                <template v-slot:cell(banParticipant)="data">
                  <span
                    style="transform: translate(0, +5px)"
                    v-if="data.item.comment.length == 0"
                    >{{ data.value }}</span
                  >
                  <b-button
                    class="text-success bg-white border-white"
                    v-else
                    v-b-tooltip.hover.left
                    v-b-tooltip.hover.html="data.item.comment"
                  >
                    {{ data.value }}
                  </b-button>
                </template>
              </b-table>
            </b-card>
          </b-dropdown-item-button>
        </b-nav-item-dropdown>
      </b-navbar-nav>
    </b-navbar>

    <hr class="mx-3 bg-warning" style="border-top: 2px double #fcba19" />
  </div>
</template>

<script lang="ts">
  import { useCommonStore, useCriminalFileStore } from '@/stores';
  import { AdjudicatorRestrictionsInfoType } from '@/types/common';
  import { bansInfoType, participantListInfoType } from '@/types/criminal';
  import * as _ from 'underscore';
  import { computed, defineComponent, onMounted, ref } from 'vue';

  export default defineComponent({
    setup() {
      const commonStore = useCommonStore();
      const criminalFileStore = useCriminalFileStore();

      const participantList = ref<participantListInfoType[]>([]);
      const adjudicatorRestrictions = ref<AdjudicatorRestrictionsInfoType[]>(
        []
      );
      const bans = ref<bansInfoType[]>([]);

      const maximumFullNameLength = 15;
      //      let numberOfParticipants = 0;
      const fileNumberText = ref('');
      const agencyLocation = ref({ name: '', code: '0', region: '' });
      const isMounted = ref(false);

      const commonFieldStyles = {
        sortable: false,
        tdClass: 'border-top',
        headerStyle: 'text-primary',
      };

      const createField = (key, label, additionalStyles = {}) => ({
        key,
        label,
        ...commonFieldStyles,
        ...additionalStyles,
      });

      const fields = [
        createField('banParticipant', 'Ban Participant', {
          headerStyle: 'table-borderless text-primary',
        }),
        createField('banType', 'Ban Type'),
        createField('orderDate', 'Order Date'),
        createField('act', 'Act'),
        createField('sub', 'Sub'),
        createField('description', 'Description'),
      ];

      onMounted(() => {
        getHeaderInfo();
      });

      const getHeaderInfo = () => {
        const data = criminalFileStore.criminalFileInformation.detailsData;
        fileNumberText.value = data.fileNumberTxt;
        agencyLocation.value.name = data.homeLocationAgencyName;
        agencyLocation.value.code = data.homeLocationAgencyCode;
        agencyLocation.value.region = data.homeLocationRegionName;
        adjudicatorRestrictions.value =
          criminalFileStore.criminalFileInformation.adjudicatorRestrictionsInfo;
        participantList.value =
          criminalFileStore.criminalFileInformation.participantList;
        bans.value = criminalFileStore.criminalFileInformation.bans;
        //        numberOfParticipants = participantList.value.length - 1;
        isMounted.value = true;
        setActiveParticipantIndex(SortedParticipants.value[0].index);
      };

      const setActiveParticipantIndex = (index) => {
        criminalFileStore.updateActiveCriminalParticipantIndex(index);
      };

      const getNameOfParticipant = (num) => {
        commonStore.updateDisplayName({
          lastName: participantList.value[num].lastName,
          givenName: participantList.value[num].firstName,
        });
        return commonStore.displayName;
      };

      const getNameOfParticipantTrunc = computed(() => {
        const nameOfParticipant = getNameOfParticipant(
          criminalFileStore.activeCriminalParticipantIndex
        );

        if (nameOfParticipant.length > maximumFullNameLength)
          return nameOfParticipant.substr(0, maximumFullNameLength) + ' ... ';
        else return nameOfParticipant;
      });

      const SortedParticipants = computed(() => {
        return _.sortBy(participantList.value, (participant) => {
          return participant.lastName ? participant.lastName.toUpperCase() : '';
        });
      });

      return {
        isMounted,
        fileNumberText,
        agencyLocation,
        getNameOfParticipantTrunc,
        participantList,
        adjudicatorRestrictions,
        bans,
        SortedParticipants,
        setActiveParticipantIndex:
          criminalFileStore.setActiveCriminalParticipantIndex,
        fields,
      };
    },
  });
</script>
