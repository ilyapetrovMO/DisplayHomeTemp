// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

navigator.serviceWorker.register('./js/service-worker.js');

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

async function notificationBellClick() {
    switch (Notification.permission) {
        case "granted":
            break;
        case "denied":
        case "default":
        Notification.requestPermission().then(permission => {
            if (permission == "denied" || permission == "default") return;
        });
        break;
    
        default:
            console.error("Error on getting notification permissions.");
            break;
    }

    const responseBody = await (await fetch('./api/vapidPublicKey')).body;
    const convertedVapidKey = urlBase64ToUint8Array(responseBody.VapidPublicKey);

    navigator.serviceWorker.ready.then(registration => {
        registration.pushManager.getSubscription().then(async sub => {
            if (!sub) {
                return registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: convertedVapidKey
                });
            } else {
                throw ('Subscription already exists');
            }
        })
    }).then(sub => {
        fetch('./api/register', {
            method: 'post',
            headers: {
                'Content-type': 'application/jason'
            },
            body: JSON.stringify({
                subscription: sub
            }),
        });
    }).catch( err => console.error(err));
};