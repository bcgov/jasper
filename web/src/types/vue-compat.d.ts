declare module "vue" {
  import { CompatVue } from "@vue/runtime-dom";
  const Vue: CompatVue;
  export default Vue;
  export * from "@vue/runtime-dom";
  export { configureCompat };
  const { configureCompat } = Vue;
}

export {}; // Ensures this file is treated as a module.
