declare module '*.vue' {
  import { DefineComponent } from 'vue';
  const component: DefineComponent<{}, {}, any>;
  export default component;
}

declare module 'vue' {
  import { CompatVue } from '@vue/runtime-dom';
  import 'vite/client';

  // Declare a `Vue` constant of type `CompatVue`, which simulates Vue 2 behavior
  const Vue: CompatVue;
  export default Vue;

  // Re-export all the original exports from '@vue/runtime-dom'
  export * from '@vue/runtime-dom';
  export { configureCompat };

  // Add `configureCompat` explicitly, as it is commonly needed for the compatibility layer
  const { configureCompat } = Vue;
}
