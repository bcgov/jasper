<template>
  <div>
    <b-navbar
      type="white"
      variant="white"
      v-if="isMounted"
      style="height: 30px"
    >
      <b-navbar-nav>
        <b-nav-text style="font-size: 14px" variant="white">
          <b-badge class="mr-1" variant="primary">{{
            courtClassDescription
          }}</b-badge>
          <b-badge class="mr-1" variant="secondary">{{
            courtLevelDescription
          }}</b-badge>
        </b-nav-text>
      </b-navbar-nav>
    </b-navbar>
  </div>
</template>

<script lang="ts">
  import { useCivilFileStore } from '@/stores';
  import { defineComponent, onMounted, ref } from 'vue';

  export default defineComponent({
    setup() {
      const civilFileStore = useCivilFileStore();
      const courtLevelDescription = ref('');
      const courtClassDescription = ref('');
      const isMounted = ref(false);

      onMounted(() => {
        getFileDescription();
      });

      const getFileDescription = () => {
        const data = civilFileStore.civilFileInformation.detailsData;
        courtClassDescription.value = data.courtClassDescription;
        courtLevelDescription.value = data.courtLevelDescription;
        isMounted.value = true;
      };

      return {
        isMounted,
        courtClassDescription,
        courtLevelDescription,
      };
    },
  });
</script>
