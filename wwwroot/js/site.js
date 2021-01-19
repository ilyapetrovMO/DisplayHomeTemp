// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

function notificationBellButtonDisabled(state) {
    var btn = document.querySelector("#NotificationBellBtn");

    if (state) {
        btn.setAttribute("disabled", "");
    } else {
        btn.removeAttribute("disabled");
    }
}

async function notificationBellClick() {
    notificationBellButtonDisabled(true);

    switch (Notification.permission) {
        case "granted":
            break;
        case "denied":
        case "default":
        await Notification.requestPermission().then(permission => {
            if (permission == "denied" || permission == "default") return;
        });
        break;
        default:
            console.error("Error on getting notification permissions.");
            break;
    }

    let registration = await navigator.serviceWorker.ready;
    let subscription = await registration.pushManager.getSubscription();

    if (subscription) {
        await fetch('./api/webPush/delete', {
            method: 'delete',
            headers: {
                'Content-type': 'application/json',
            },
            body: JSON.stringify(subscription),
        }).then(async res => {
            if (res.ok) {
                await subscription.unsubscribe();
            }
        }).catch(err => console.error(err))
        .then(async () => {
            await checkSubAndUpdateBell(registration);
            notificationBellButtonDisabled(false);
        });

        return;
    }

    const response = await fetch('./api/webPush/vapidPublicKey');

    if (!response.ok) {
        console.error("server could not provide a vapidPublicKey");

        return;
    }

    const responseBody = await response.json();
    const convertedVapidKey = urlBase64ToUint8Array(responseBody.vapidPublicKey);

    await registration.pushManager.getSubscription().then( sub => {
        if (!sub) {
            return registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: convertedVapidKey
            });
        } else {
            fetch
        }
    }).then(sub => {
        fetch('./api/webPush/register', {
            method: 'post',
            headers: {
                'Content-type': 'application/json'
            },
            body: JSON.stringify(sub),
        }).then( async res => {
            if (!res.ok) throw new Error(`${res.status}: Encountered error while sending subscription to server.`);
        }).catch(err => {
            console.error(err);

            registration.pushManager.getSubscription().then(async sub => {
                await sub.unsubscribe();
            });
        }).then(async () => {
            await checkSubAndUpdateBell(registration);
            notificationBellButtonDisabled(false);
        });
    });
};

async function verifySub(registration) {
    let sub = await registration.pushManager.getSubscription();

    let res = await fetch('./api/webpush/isSubscriptionActive', {
        method: 'Post',
        headers: {
            'Content-type': 'application/json',
        },
        body: JSON.stringify(sub),
    })

    if (res.ok) {
        console.info("subscription verified.");

        return;
    } else if ( res.status == 410) {
        await sub.unsubscribe();

        return;
    }
}

async function checkSubAndUpdateBell(registration) {
    await registration.pushManager.getSubscription().then(sub => {
        let bell = document.querySelector("#NotificationBell");
        if (sub) {
            bell.classList = "bi bi-bell-fill";
        } else {
            bell.classList = "bi bi-bell";
        }
    });
}

function urlBase64ToUint8Array(base64String) {
    var padding = '='.repeat((4 - base64String.length % 4) % 4);
    var base64 = (base64String + padding)
        .replace(/\-/g, '+')
        .replace(/_/g, '/');

    var rawData = window.atob(base64);
    var outputArray = new Uint8Array(rawData.length);

    for (var i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}

(async () => {
    if (!navigator.serviceWorker) {
        document.querySelector("#NotificationBellBtn").style.display = "none";
        console.warn("This browser does not support service-workers or you are not using https.")
        return;
    }

    notificationBellButtonDisabled(true);

    navigator.serviceWorker.register('./service-worker.js').then(async registration => {
        await verifySub(registration);

        await navigator.serviceWorker.ready;

        await Promise.all([
            registration.sync.register('sync-subscriptions'),
        ]);

        await checkSubAndUpdateBell(registration);
        notificationBellButtonDisabled(false);
    });
})()
