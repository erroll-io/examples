<template>
  <header>
    <!-- <img alt="Vue logo" class="logo" src="@/assets/logo.svg" width="125" height="125" style="transform: rotate(90deg);" /> -->

    <Suspense>
      <div class="wrapper">
        <nav>
          <RouterLink to="/">Home</RouterLink>
          <RouterLink to="/about">About</RouterLink>
          <RouterLink v-if="!isAuthenticated" to="/signin">Sign In</RouterLink>
          <RouterLink v-if="isAuthenticated" to="/profile">Profile</RouterLink>
        </nav>
      </div>
      <template #fallback>
        <div>Loading...</div>
      </template>
    </Suspense>
  </header>

  <Suspense>
    <RouterView />

    <template #fallback>
      <div>Loading...</div>
    </template>
  </Suspense>
</template>

<script>
import { computed } from 'vue'
import { RouterLink, RouterView, useRouter } from 'vue-router'
import { useUserStore } from './stores/userStore.js'

export default {
  setup() {
    const router = useRouter();
    const userStore = useUserStore();

    function logout() {
      // TODO:
      //store.dispatch("logout");

      router.push({
        name: "SignIn",
        params: { message: "You have logged out" },
      });
    }

    const isAuthenticated = computed(function() {
      return userStore.id_token ? true : false;
    });

    return {
      logout,
      isAuthenticated,
    };
  },
};
</script>

<style scoped>
header {
  line-height: 1.5;
  max-height: 100vh;
}

.logo {
  display: block;
  margin: 0 auto 2rem;
}

nav {
  width: 100%;
  font-size: 12px;
  text-align: center;
  margin-top: 2rem;
}

nav a.router-link-exact-active {
  color: var(--color-text);
}

nav a.router-link-exact-active:hover {
  background-color: transparent;
}

nav a {
  display: block;
  padding: 0 1rem;
  border-left: 1px solid var(--color-border);
}

nav a:first-of-type {
  border: 0;
}

@media (min-width: 1024px) {
  header {
    display: flex;
    place-items: center;
    /* padding-right: calc(var(--section-gap) / 2); */
  }

  .logo {
    margin: 0 2rem 0 0;
  }

  header .wrapper {
    min-width: 200px;
    /* display: flex;
    place-items: flex-start;
    flex-wrap: wrap; */
  }

  nav {
    text-align: left;
    margin-left: -1rem;
    font-size: 1rem;

    padding: 1rem 0;
    margin-top: 1rem;
  }
}
</style>
