/* Simple service worker for Web Push notifications (dev). Registers 'push' event and shows notification.
   Requires the client to subscribe with VAPID public key and send subscription to backend. */
self.addEventListener("push", function (event) {
  let data = {};
  try {
    data = event.data.json();
  } catch (e) {
    data = { body: event.data ? event.data.text() : "Bildirim" };
  }
  const title = data.title || "Yeni bildirim";
  const options = {
    body: data.body || data.message || "Yeni bildiriminiz var",
    icon: "/images/icon-192.png",
    badge: "/images/icon-72.png",
    data: data,
  };
  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener("notificationclick", function (event) {
  event.notification.close();
  event.waitUntil(
    clients.matchAll({ type: "window" }).then(function (c) {
      if (c.length > 0) return c[0].focus();
      return clients.openWindow("/");
    })
  );
});
