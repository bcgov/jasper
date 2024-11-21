<template>
  <div class="app-outer fill-body" id="app">
    <navigation-topbar />
    <router-view />
    <navigation-footer id="footer" />
  </div>
</template>

<script lang="ts">
  import NavigationTopbar from '@components/NavigationTopbar.vue';
  import NavigationFooter from '@components/NavigationFooter.vue';
  import { defineComponent } from 'vue';

  export default defineComponent({
    name: 'App',
    components: {
      NavigationTopbar,
      NavigationFooter,
    },
  });
</script>
