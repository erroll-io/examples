<script>
import { useUserStore } from '../stores/userStore.js'

export default {
  setup() {
    const userStore = useUserStore();

    const isAuthenticated = userStore.id_token ? true : false;

    return {
      isAuthenticated,
    };
  }
};
</script>

<template>
  <main>
    <div class="container" v-if="isAuthenticated">
      WELCOME
    </div>
  </main>
</template>
