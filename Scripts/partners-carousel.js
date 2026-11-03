(function () {
    'use strict';
    var track = document.getElementById('glc-partners-track');
    if (!track || track.children.length < 2) return;
    var originals = Array.prototype.slice.call(track.children);
    // A second, inaccessible copy makes the loop continuous without duplicating
    // the partner list for screen readers or changing the CRM data.
    originals.forEach(function (card) {
        var copy = card.cloneNode(true);
        copy.classList.add('glc-partner-card-copy');
        copy.setAttribute('aria-hidden', 'true');
        copy.setAttribute('role', 'presentation');
        copy.querySelectorAll('[id]').forEach(function (element) { element.removeAttribute('id'); });
        copy.querySelectorAll('img').forEach(function (image) { image.alt = ''; });
        track.appendChild(copy);
    });
    track.classList.add('is-automatic');
    var reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    var canHover = window.matchMedia('(hover: hover)');
    var hovering = false;
    var touching = false;
    var focused = false;
    var pausedUntil = 0;
    var previousTime = 0;
    var position = track.scrollLeft;
    var loopWidth = 0;
    var animation;
    function measure() {
        var firstCopy = track.querySelector('.glc-partner-card-copy');
        loopWidth = firstCopy.getBoundingClientRect().left - originals[0].getBoundingClientRect().left;
        position = track.scrollLeft;
    }
    function frame(time) {
        var elapsed = previousTime ? Math.min(time - previousTime, 50) : 0;
        previousTime = time;
        if (!reducedMotion.matches && !document.hidden && !hovering && !touching && !focused && time >= pausedUntil && loopWidth > 0) {
            position += elapsed * 0.028;
            if (position >= loopWidth) position -= loopWidth;
            track.scrollLeft = position;
        } else {
            position = track.scrollLeft;
        }
        animation = window.requestAnimationFrame(frame);
    }
    track.addEventListener('mouseenter', function () { if (canHover.matches) hovering = true; });
    track.addEventListener('mouseleave', function () { hovering = false; });
    track.addEventListener('focusin', function () { focused = true; });
    track.addEventListener('focusout', function () { focused = false; });
    track.addEventListener('pointerdown', function () { touching = true; });
    function release() { if (touching) { touching = false; pausedUntil = performance.now() + 2500; } }
    document.addEventListener('pointerup', release);
    document.addEventListener('pointercancel', release);
    track.addEventListener('wheel', function () { pausedUntil = performance.now() + 2500; }, { passive: true });
    track.addEventListener('scroll', function () {
        if (hovering || touching || focused || reducedMotion.matches || performance.now() < pausedUntil) position = track.scrollLeft;
    }, { passive: true });
    window.addEventListener('resize', measure);
    window.addEventListener('pagehide', function () { window.cancelAnimationFrame(animation); });
    window.addEventListener('pageshow', function (event) { if (event.persisted) { previousTime = 0; animation = window.requestAnimationFrame(frame); } });
    measure();
    animation = window.requestAnimationFrame(frame);
}());
