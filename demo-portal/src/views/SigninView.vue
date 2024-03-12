<script>
import { ref, onMounted } from "vue";
import { useRouter, useRoute } from "vue-router";
import { googleConfig } from "../config.js";
import { useUserStore } from '../stores/userStore.js'

export default {
  setup() {
    const router = useRouter();
    const route = useRoute();
    const userStore = useUserStore();

    function handleGoogleSigninResponse(resp) {
      console.log('id_token: ' + JSON.stringify(resp?.credential));
      userStore.id_token = resp.credential;

      router.replace({
        name: "home"
      });
    }

    onMounted(() => {
      google.accounts.id.initialize({
          client_id: googleConfig.clientId,
          callback: handleGoogleSigninResponse
        });
        google.accounts.id.renderButton(
          document.getElementById("google-signin-btn"),
          { theme: "outline", size: "large" }  // customization attributes
        );
    })

    return {
    };
  },
};
</script>

<template>
  <div class="container">
    <div id="google-signin-btn"></div>
  </div>
</template>

<style></style>
