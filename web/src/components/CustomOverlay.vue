<template>
  <div>
    <b-overlay :show="show" rounded="sm">
      <slot />
      <template v-slot:overlay>
        <div style="text-align: center">
          <loading-spinner />
        </div>
      </template>
    </b-overlay>
  </div>
</template>

<script lang="ts">
  import { defineComponent } from 'vue';

  export default defineComponent({
    props: {
      show: { required: true, type: Boolean },
    },
  });
</script>

<style></style>
