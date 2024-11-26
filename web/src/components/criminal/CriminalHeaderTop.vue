<template>
  <b-navbar type="white" variant="white" v-if="isMounted" style="height: 30px">
    <b-navbar-nav>
      <b-nav-text style="font-size: 14px" variant="white">
        <b-badge class="mr-1" variant="primary">{{
          courtClassDescription
        }}</b-badge>
        <b-badge class="mr-1" variant="danger">{{ indictable }}</b-badge>
        <b-badge class="mr-1" variant="secondary">{{
          courtLevelDescription
        }}</b-badge>
        <b-badge v-if="hasAssignmentTypeDsc" class="mr-1" variant="success">{{
          assignmentTypeDescription
        }}</b-badge>
        <b-badge v-if="hasCrownNotesToJCM" variant="danger"
          >Crown Notes to JCM</b-badge
        >
      </b-nav-text>
    </b-navbar-nav>
  </b-navbar>
</template>

<script lang="ts">
  import { useCriminalFileStore } from '@/stores';
  import { defineComponent, onMounted, ref } from 'vue';

  export default defineComponent({
    setup() {
      const criminalFileStore = useCriminalFileStore();

      const courtLevelDescription = ref('');
      const courtClassDescription = ref('');
      const indictable = ref('');
      const assignmentTypeDescription = ref('');
      const hasCrownNotesToJCM = ref(false);
      const hasAssignmentTypeDsc = ref(false);
      const isMounted = ref(false);

      onMounted(() => {
        getFileDescription();
      });

      const getFileDescription = () => {
        const data = criminalFileStore.criminalFileInformation.detailsData;
        courtClassDescription.value = data.courtClassDescription;
        courtLevelDescription.value = data.courtLevelDescription;
        if (data.assignmentTypeDsc && data.assignmentTypeDsc.length > 0) {
          assignmentTypeDescription.value = data.assignmentTypeDsc;
          hasAssignmentTypeDsc.value = true;
        }
        hasCrownNotesToJCM.value =
          data.trialRemark && data.trialRemark.length > 0;
        indictable.value =
          data.indictableYN == 'Y' ? 'Indictable' : 'Summarily';
        isMounted.value = true;
      };

      return {
        isMounted,
        courtClassDescription,
        courtLevelDescription,
        indictable,
        assignmentTypeDescription,
        hasCrownNotesToJCM,
        hasAssignmentTypeDsc,
      };
    },
  });
</script>
