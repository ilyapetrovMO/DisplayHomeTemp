let newSubscription = null;

self.addEventListener('pushsubscriptionchanged', event => {
    event.waitUntil(
        self.registration.pushManager.subscribe(event.oldSubscription.options)
            .then(sub => newSubscription = sub)
    );
});

self.addEventListener('sync', event => {
    if (event.tag == 'sync-subscriptions' && newSubscription) {
        event.waitUntil( 
            fetch('./api/webPush/register', {
                method: 'post',
                headers: {
                    'Content-type': 'application/json'
                },
                body: JSON.stringify({
                    subscription: newSubscription
                }),
            })
            .then(res => newSubscription = null)
            .catch(err => {
                console.error('problem while syncing new subscription.');
                return;
            })
        );
    }
});

self.addEventListener('periodicsync', async event => {
    if (event.tag == 'verify-subscription') {
      event.waitUntil(
          await verifySub()
      );
    }
  });

self.addEventListener('push', event => {
    event.waitUntil(
        self.registration.showNotification("Verbilki temp", {
            lang: 'en',
            body: event.data.text(),
            icon: 'favicon-32x32.png',
        })
    );
});

async function verifySub() {
    let sub = await self.registration.pushManager.getSubscription();

    let res = await fetch('./api/webpush/issubscriptionactive', {
        headers: {
            'Content-type': 'application/json',
        },
        body: JSON.stringify(sub),
    })

    if (res.ok) {
        console.info("subscription verified.");

        return;
    } else {
        await sub.unsubscribe();

        return;
    }
}