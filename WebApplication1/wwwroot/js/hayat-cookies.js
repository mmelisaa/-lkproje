/**
 * Hayat İşleri - 6698 Sayılı KVKK & Çerez Yönetim Sistemi
 * Yasal Uyum: Zorunlu çerezler doğrudan çalışır; analitik ve pazarlama çerezleri
 * yalnızca kullanıcı açık onay (Opt-in) verdikten sonra devreye alınır.
 */
(function (window, document) {
    'use strict';

    const COOKIE_NAME = 'hayat_cookie_consent';
    const COOKIE_MAX_AGE = 31536000; // 1 Yıl (Saniye)

    const defaultConsent = {
        necessary: true,
        functional: false,
        analytics: false,
        marketing: false,
        decided: false,
        timestamp: null,
        version: '1.0'
    };

    let currentConsent = Object.assign({}, defaultConsent);

    function getCookie(name) {
        const matches = document.cookie.match(new RegExp(
            '(?:^|; )' + name.replace(/([\.$?*|{}\(\)\[\]\\\/\+^])/g, '\\$1') + '=([^;]*)'
        ));
        return matches ? decodeURIComponent(matches[1]) : undefined;
    }

    function setCookie(name, value, maxAgeSeconds) {
        const encoded = encodeURIComponent(value);
        let updatedCookie = name + '=' + encoded + '; path=/; max-age=' + maxAgeSeconds + '; SameSite=Lax';
        if (location.protocol === 'https:') {
            updatedCookie += '; Secure';
        }
        document.cookie = updatedCookie;
    }

    function deleteCookie(name, path, domain) {
        let cookieStr = name + '=; path=' + (path || '/') + '; expires=Thu, 01 Jan 1970 00:00:01 GMT';
        if (domain) {
            cookieStr += '; domain=' + domain;
        }
        document.cookie = cookieStr;
    }

    function loadSavedConsent() {
        try {
            // Önce LocalStorage dene
            let saved = localStorage.getItem(COOKIE_NAME);
            if (!saved) {
                // Yoksa Cookie'den oku
                saved = getCookie(COOKIE_NAME);
            }
            if (saved) {
                const parsed = JSON.parse(saved);
                if (parsed && typeof parsed === 'object') {
                    currentConsent = Object.assign({}, defaultConsent, parsed);
                }
            }
        } catch (e) {
            console.warn('[HayatCookies] Tercih okuma hatası:', e);
        }
    }

    function persistConsent() {
        currentConsent.timestamp = new Date().toISOString();
        currentConsent.decided = true;
        const serialized = JSON.stringify(currentConsent);
        try {
            localStorage.setItem(COOKIE_NAME, serialized);
        } catch (e) { }
        setCookie(COOKIE_NAME, serialized, COOKIE_MAX_AGE);
    }

    // İsteğe bağlı çerezleri temizleme (Reddedildiğinde veya izin geri alındığında)
    function purgeOptionalCookies() {
        if (!currentConsent.analytics) {
            ['_ga', '_gid', '_gat', '_gat_gtag'].forEach(function (c) {
                deleteCookie(c, '/');
                deleteCookie(c, '/', window.location.hostname);
                deleteCookie(c, '/', '.' + window.location.hostname);
            });
        }
        if (!currentConsent.marketing) {
            ['_fbp', '_gcl_au', '_uetsid', '_uetvid'].forEach(function (c) {
                deleteCookie(c, '/');
                deleteCookie(c, '/', window.location.hostname);
                deleteCookie(c, '/', '.' + window.location.hostname);
            });
        }
    }

    // Analitik ve Pazarlama motorlarını dinamik devreye alma
    function dispatchConsentEvents() {
        try {
            const event = new CustomEvent('hayatCookieConsentChanged', {
                detail: Object.assign({}, currentConsent)
            });
            window.dispatchEvent(event);

            // Google Tag Manager / Analytics dataLayer desteği (eğer varsa)
            if (window.dataLayer && Array.isArray(window.dataLayer)) {
                window.dataLayer.push({
                    event: 'cookie_consent_update',
                    consent_analytics: currentConsent.analytics ? 'granted' : 'denied',
                    consent_marketing: currentConsent.marketing ? 'granted' : 'denied',
                    consent_functional: currentConsent.functional ? 'granted' : 'denied'
                });
            }

            // Analitik izni verildiyse telemetry bildirimi
            if (currentConsent.analytics) {
                console.info('[HayatCookies] Analitik çerezleri aktif edildi.');
            }
        } catch (e) {
            console.warn('[HayatCookies] Event dispatch error:', e);
        }
    }

    function showBanner() {
        const bannerWrap = document.getElementById('hayatCookieBannerWrap');
        if (bannerWrap) {
            bannerWrap.classList.add('show');
        }
    }

    function hideBanner() {
        const bannerWrap = document.getElementById('hayatCookieBannerWrap');
        if (bannerWrap) {
            bannerWrap.classList.remove('show');
        }
    }

    function updateModalCheckboxes() {
        const chkFunctional = document.getElementById('hayatCookieChkFunctional');
        const chkAnalytics = document.getElementById('hayatCookieChkAnalytics');
        const chkMarketing = document.getElementById('hayatCookieChkMarketing');

        if (chkFunctional) chkFunctional.checked = !!currentConsent.functional;
        if (chkAnalytics) chkAnalytics.checked = !!currentConsent.analytics;
        if (chkMarketing) chkMarketing.checked = !!currentConsent.marketing;
    }

    function openPreferences() {
        updateModalCheckboxes();
        const modalEl = document.getElementById('hayatCookieModal');
        if (modalEl && window.bootstrap && window.bootstrap.Modal) {
            const modalInstance = bootstrap.Modal.getOrCreateInstance(modalEl);
            modalInstance.show();
        }
    }

    function closePreferences() {
        const modalEl = document.getElementById('hayatCookieModal');
        if (modalEl && window.bootstrap && window.bootstrap.Modal) {
            const modalInstance = bootstrap.Modal.getInstance(modalEl);
            if (modalInstance) {
                modalInstance.hide();
            }
        }
    }

    function acceptAll() {
        currentConsent.functional = true;
        currentConsent.analytics = true;
        currentConsent.marketing = true;
        persistConsent();
        hideBanner();
        closePreferences();
        dispatchConsentEvents();
    }

    function rejectOptional() {
        currentConsent.functional = false;
        currentConsent.analytics = false;
        currentConsent.marketing = false;
        persistConsent();
        purgeOptionalCookies();
        hideBanner();
        closePreferences();
        dispatchConsentEvents();
    }

    function saveCustomPreferences() {
        const chkFunctional = document.getElementById('hayatCookieChkFunctional');
        const chkAnalytics = document.getElementById('hayatCookieChkAnalytics');
        const chkMarketing = document.getElementById('hayatCookieChkMarketing');

        currentConsent.functional = chkFunctional ? chkFunctional.checked : false;
        currentConsent.analytics = chkAnalytics ? chkAnalytics.checked : false;
        currentConsent.marketing = chkMarketing ? chkMarketing.checked : false;

        persistConsent();
        purgeOptionalCookies();
        hideBanner();
        closePreferences();
        dispatchConsentEvents();
    }

    // Sayfa Yüklendiğinde Başlat
    function init() {
        loadSavedConsent();

        // Eğer kullanıcı henüz karar vermemişse banner'ı göster
        if (!currentConsent.decided) {
            setTimeout(showBanner, 700);
        } else {
            // Karar verilmiş, izinli scriptleri tetikle
            dispatchConsentEvents();
        }

        // Event listener bağlamaları
        const btnAcceptAll = document.getElementById('hayatCookieBtnAcceptAll');
        const btnReject = document.getElementById('hayatCookieBtnReject');
        const btnCustomize = document.getElementById('hayatCookieBtnCustomize');
        const btnSaveModal = document.getElementById('hayatCookieBtnSaveModal');
        const btnAcceptAllModal = document.getElementById('hayatCookieBtnAcceptAllModal');
        const btnRejectModal = document.getElementById('hayatCookieBtnRejectModal');
        const floatingTrigger = document.getElementById('hayatCookieFloatingTrigger');

        if (btnAcceptAll) btnAcceptAll.addEventListener('click', acceptAll);
        if (btnReject) btnReject.addEventListener('click', rejectOptional);
        if (btnCustomize) btnCustomize.addEventListener('click', openPreferences);
        if (btnSaveModal) btnSaveModal.addEventListener('click', saveCustomPreferences);
        if (btnAcceptAllModal) btnAcceptAllModal.addEventListener('click', acceptAll);
        if (btnRejectModal) btnRejectModal.addEventListener('click', rejectOptional);
        if (floatingTrigger) floatingTrigger.addEventListener('click', openPreferences);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Dışarıya Açılan Global API
    window.HayatCookies = {
        openPreferences: openPreferences,
        acceptAll: acceptAll,
        rejectOptional: rejectOptional,
        saveCustomPreferences: saveCustomPreferences,
        getConsent: function () { return Object.assign({}, currentConsent); },
        hasConsent: function (type) {
            if (type === 'necessary') return true;
            return !!currentConsent[type];
        }
    };

})(window, document);
