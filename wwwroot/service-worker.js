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

self.addEventListener('push', event => {
    event.waitUntil(
        self.registration.showNotification("Verbilki temp", {
            lang: 'en',
            body: event.data.text(),
            icon: 'favicon-32x32.png',
        })
    );
});