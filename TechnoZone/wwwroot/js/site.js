/* ============================================================================
   TechnoZone - site.js
   Behaviour shared by every page: the navigation bar, the account menu,
   the mobile menu, the cart counter and the newsletter sign-up.
   No libraries, no jQuery - plain browser JavaScript.
   ============================================================================ */

(function () {
    'use strict';

    /* ------------------------------------------------------------------
       Navigation: add a shadow once the page has scrolled past the top
       ------------------------------------------------------------------ */
    function initStickyNav() {
        var nav = document.querySelector('.global-nav');
        if (!nav) { return; }

        var ticking = false;

        function update() {
            nav.classList.toggle('is-scrolled', window.scrollY > 8);
            ticking = false;
        }

        window.addEventListener('scroll', function () {
            if (!ticking) {
                window.requestAnimationFrame(update);
                ticking = true;
            }
        }, { passive: true });

        update();
    }

    /* ------------------------------------------------------------------
       Account menu: opens on click, closes on outside click or Escape.
       CSS already opens it on hover for mouse users; this adds keyboard
       and touch support.
       ------------------------------------------------------------------ */
    function initAccountMenu() {
        var menu = document.querySelector('.nav-user-menu');
        if (!menu) { return; }

        var button = menu.querySelector('.nav-icon-btn');
        var dropdown = menu.querySelector('.dropdown-menu');
        if (!button || !dropdown) { return; }

        button.setAttribute('aria-expanded', 'false');
        button.setAttribute('aria-haspopup', 'true');

        function close() {
            menu.classList.remove('is-open');
            button.setAttribute('aria-expanded', 'false');
        }

        button.addEventListener('click', function (event) {
            event.stopPropagation();
            var open = menu.classList.toggle('is-open');
            button.setAttribute('aria-expanded', String(open));
        });

        document.addEventListener('click', function (event) {
            if (!menu.contains(event.target)) { close(); }
        });

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') { close(); }
        });
    }

    /* ------------------------------------------------------------------
       Mobile menu
       ------------------------------------------------------------------ */
    function initMobileMenu() {
        var toggle = document.querySelector('[data-menu-toggle]');
        var links = document.querySelector('.nav-links');
        if (!toggle || !links) { return; }

        toggle.setAttribute('aria-expanded', 'false');

        toggle.addEventListener('click', function () {
            var open = links.classList.toggle('is-open');
            toggle.classList.toggle('is-open', open);
            toggle.setAttribute('aria-expanded', String(open));
            toggle.setAttribute('aria-label', open ? 'Close menu' : 'Open menu');
        });

        /* Close the menu after picking a link */
        Array.prototype.forEach.call(links.querySelectorAll('a'), function (link) {
            link.addEventListener('click', function () {
                links.classList.remove('is-open');
                toggle.classList.remove('is-open');
                toggle.setAttribute('aria-expanded', 'false');
            });
        });
    }

    /* ------------------------------------------------------------------
       Search box in the navigation bar
       ------------------------------------------------------------------ */
    function initSearch() {
        var button = document.querySelector('[data-search-toggle]');
        var panel = document.querySelector('[data-search-panel]');
        if (!button || !panel) { return; }

        var input = panel.querySelector('input');

        button.addEventListener('click', function () {
            var open = panel.classList.toggle('is-open');
            button.setAttribute('aria-expanded', String(open));
            if (open && input) { input.focus(); }
        });

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                panel.classList.remove('is-open');
                button.setAttribute('aria-expanded', 'false');
            }
        });
    }

    /* ------------------------------------------------------------------
       Cart counter
       The count is kept in localStorage so it survives a page change.
       Any button marked data-add-to-cart increases it.
       ------------------------------------------------------------------ */
    var Cart = {
        key: 'technozone.cart.count',

        read: function () {
            var stored = parseInt(window.localStorage.getItem(this.key), 10);
            return isNaN(stored) ? 0 : stored;
        },

        write: function (count) {
            window.localStorage.setItem(this.key, String(count));
            this.paint(count);
        },

        add: function (quantity) {
            this.write(this.read() + (quantity || 1));
        },

        paint: function (count) {
            var badge = document.querySelector('.cart-badge');
            if (!badge) { return; }

            badge.textContent = count;
            badge.hidden = count === 0;

            badge.classList.remove('is-bumped');
            void badge.offsetWidth;          // restart the animation
            badge.classList.add('is-bumped');
        }
    };

    function initCart() {
        Cart.paint(Cart.read());

        document.addEventListener('click', function (event) {
            var trigger = event.target.closest('[data-add-to-cart]');
            if (!trigger) { return; }

            event.preventDefault();
            Cart.add(1);

            var original = trigger.textContent;
            trigger.textContent = 'Added';
            trigger.disabled = true;

            setTimeout(function () {
                trigger.textContent = original;
                trigger.disabled = false;
            }, 1400);
        });
    }

    /* ------------------------------------------------------------------
       Newsletter sign-up
       Posts the address with fetch() and reports the result in place.
       ------------------------------------------------------------------ */
    function initNewsletter() {
        var form = document.querySelector('.newsletter-form');
        if (!form) { return; }

        var input = form.querySelector('.newsletter-input');
        var button = form.querySelector('.btn-subscribe');
        var note = form.querySelector('[data-newsletter-message]');

        function say(text, type) {
            if (!note) { return; }
            note.textContent = text;
            note.className = 'newsletter-message newsletter-message--' + type + ' is-visible';
        }

        form.addEventListener('submit', function (event) {
            event.preventDefault();

            var email = input.value.trim();

            if (!/^[^\s@]+@[^\s@]+\.[a-zA-Z]{2,}$/.test(email)) {
                say('Enter an email address in the form name@example.com', 'error');
                input.focus();
                return;
            }

            var token = form.querySelector('input[name="__RequestVerificationToken"]');
            var payload = new URLSearchParams();
            payload.append('email', email);
            if (token) { payload.append('__RequestVerificationToken', token.value); }

            button.disabled = true;
            button.textContent = 'Subscribing…';

            fetch(form.getAttribute('data-ajax-url') || '/Home/Subscribe', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: payload.toString()
            })
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    button.disabled = false;
                    button.textContent = 'Subscribe';

                    if (data.success) {
                        say(data.message || 'You are on the list.', 'success');
                        input.value = '';
                    } else {
                        say(data.message || 'That did not work. Try again.', 'error');
                    }
                })
                .catch(function () {
                    button.disabled = false;
                    button.textContent = 'Subscribe';
                    say('The server could not be reached. Try again in a moment.', 'error');
                });
        });
    }

    /* ------------------------------------------------------------------
       Smooth scrolling for same-page anchor links
       ------------------------------------------------------------------ */
    function initAnchorLinks() {
        document.addEventListener('click', function (event) {
            var link = event.target.closest('a[href^="#"]');
            if (!link) { return; }

            var id = link.getAttribute('href');
            if (id === '#' || id.length < 2) { return; }

            var target = document.querySelector(id);
            if (!target) { return; }

            event.preventDefault();

            var reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
            target.scrollIntoView({ behavior: reduced ? 'auto' : 'smooth', block: 'start' });
        });
    }

    /* ------------------------------------------------------------------
       Start everything once the markup is ready
       ------------------------------------------------------------------ */
    document.addEventListener('DOMContentLoaded', function () {
        initStickyNav();
        initAccountMenu();
        initMobileMenu();
        initSearch();
        initCart();
        initNewsletter();
        initAnchorLinks();
    });

    /* Make the cart available to other scripts */
    window.TechnoZone = { Cart: Cart };

})();
