export default {
  fetch(request) {
    return fetch(new Request("https://reki-app.ru/api/v1/telegram/webhook", request));
  },
};
