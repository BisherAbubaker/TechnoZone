/* ============================================================================
   TechnoZone - auth.js
   Drives the sign-in and create-account screens:
     - live field validation (validation.js)
     - show / hide password
     - password strength meter
     - checks with the server whether a username or email is free
     - submits the form with fetch() and shows the reply without a page reload

   The forms still work with JavaScript switched off: every handler calls
   preventDefault() only after it has taken over, so the plain HTML POST
   remains the fallback.
   ============================================================================ */

(function () {
    'use strict';

    var Rules = window.TZValidation.Rules;
    var Validator = window.TZValidation.Validator;
    var scorePassword = window.TZValidation.scorePassword;

    /* ------------------------------------------------------------------
       Helpers
       ------------------------------------------------------------------ */

    function antiForgeryToken(form) {
        var input = form.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function showFormMessage(form, text, type) {
        var box = form.querySelector('[data-form-message]');
        if (!box) { return; }
        box.textContent = text;
        box.className = 'form-message form-message--' + type + ' is-visible';
    }

    function clearFormMessage(form) {
        var box = form.querySelector('[data-form-message]');
        if (!box) { return; }
        box.textContent = '';
        box.className = 'form-message';
    }

    function setBusy(button, busy, busyText) {
        if (!button) { return; }
        if (busy) {
            button.dataset.idleText = button.textContent;
            button.textContent = busyText;
            button.disabled = true;
            button.classList.add('is-busy');
        } else {
            button.textContent = button.dataset.idleText || button.textContent;
            button.disabled = false;
            button.classList.remove('is-busy');
        }
    }

    /* Waits until the person stops typing before calling the server. */
    function debounce(fn, wait) {
        var timer = null;
        return function () {
            var args = arguments, self = this;
            clearTimeout(timer);
            timer = setTimeout(function () { fn.apply(self, args); }, wait);
        };
    }

    /* ------------------------------------------------------------------
       Show / hide password
       ------------------------------------------------------------------ */
    function wirePasswordToggles() {
        var toggles = document.querySelectorAll('[data-toggle-password]');

        Array.prototype.forEach.call(toggles, function (toggle) {
            toggle.addEventListener('click', function () {
                var target = document.getElementById(toggle.getAttribute('data-toggle-password'));
                if (!target) { return; }

                var showing = target.type === 'text';
                target.type = showing ? 'password' : 'text';
                toggle.setAttribute('aria-pressed', String(!showing));
                toggle.setAttribute('aria-label', showing ? 'Show password' : 'Hide password');
                toggle.classList.toggle('is-showing', !showing);
            });
        });
    }

    /* ------------------------------------------------------------------
       Password strength meter
       ------------------------------------------------------------------ */
    function wireStrengthMeter() {
        var input = document.getElementById('password');
        var meter = document.querySelector('[data-strength-meter]');
        if (!input || !meter) { return; }

        var bar = meter.querySelector('.strength-bar span');
        var label = meter.querySelector('.strength-label');

        input.addEventListener('input', function () {
            var result = scorePassword(input.value);

            bar.style.width = (result.score * 25) + '%';
            meter.setAttribute('data-score', result.score);
            label.textContent = result.label;
        });
    }

    /* ------------------------------------------------------------------
       Live availability check for username and email
       ------------------------------------------------------------------ */
    function wireAvailabilityCheck(inputId, url, freeText, takenText) {
        var input = document.getElementById(inputId);
        var hint = document.querySelector('[data-availability-for="' + inputId + '"]');
        if (!input || !hint) { return; }

        var check = debounce(function () {
            var value = input.value.trim();

            if (value.length < 3 || input.classList.contains('is-invalid')) {
                hint.textContent = '';
                hint.className = 'availability-hint';
                return;
            }

            hint.textContent = 'Checking…';
            hint.className = 'availability-hint is-checking is-visible';

            fetch(url + '?value=' + encodeURIComponent(value), {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            })
                .then(function (response) {
                    if (!response.ok) { throw new Error('Request failed'); }
                    return response.json();
                })
                .then(function (data) {
                    if (input.value.trim() !== value) { return; } // person kept typing

                    if (data.taken) {
                        hint.textContent = takenText;
                        hint.className = 'availability-hint is-taken is-visible';
                        input.classList.add('is-invalid');
                        input.classList.remove('is-valid');
                    } else {
                        hint.textContent = freeText;
                        hint.className = 'availability-hint is-free is-visible';
                        input.classList.remove('is-invalid');
                        input.classList.add('is-valid');
                    }
                })
                .catch(function () {
                    hint.textContent = '';
                    hint.className = 'availability-hint';
                });
        }, 450);

        input.addEventListener('input', check);
    }

    /* ------------------------------------------------------------------
       Sign-in screen
       ------------------------------------------------------------------ */
    function initLogin() {
        var form = document.getElementById('loginForm');
        if (!form) { return; }

        var validator = new Validator('loginForm');
        validator
            .addField('username', [Rules.required('Username')])
            .addField('password', [Rules.required('Password')]);

        var submitButton = form.querySelector('button[type="submit"]');

        form.addEventListener('submit', function (event) {
            if (!validator.validateAll()) {
                event.preventDefault();
                showFormMessage(form, 'Check the highlighted fields and try again.', 'error');
                return;
            }

            event.preventDefault();
            clearFormMessage(form);
            setBusy(submitButton, true, 'Signing in…');

            var payload = new URLSearchParams();
            payload.append('username', document.getElementById('username').value.trim());
            payload.append('password', document.getElementById('password').value);
            payload.append('rememberMe', document.getElementById('rememberMe').checked);
            payload.append('__RequestVerificationToken', antiForgeryToken(form));

            fetch(form.getAttribute('data-ajax-url'), {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: payload.toString()
            })
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    if (data.success) {
                        showFormMessage(form, 'Signed in. Taking you to the shop…', 'success');
                        window.location.href = data.redirectUrl || '/';
                    } else {
                        setBusy(submitButton, false);
                        showFormMessage(form, data.message || 'That username and password do not match.', 'error');
                        document.getElementById('password').value = '';
                        document.getElementById('password').focus();
                    }
                })
                .catch(function () {
                    setBusy(submitButton, false);
                    showFormMessage(form, 'The server could not be reached. Check your connection and try again.', 'error');
                });
        });
    }

    /* ------------------------------------------------------------------
       Create-account screen
       ------------------------------------------------------------------ */
    function initRegister() {
        var form = document.getElementById('registerForm');
        if (!form) { return; }

        var validator = new Validator('registerForm');
        validator
            .addField('firstName', [Rules.required('First name'), Rules.maxLength('First name', 100)])
            .addField('lastName', [Rules.required('Last name'), Rules.maxLength('Last name', 100)])
            .addField('username', [
                Rules.required('Username'),
                Rules.minLength('Username', 3),
                Rules.maxLength('Username', 50),
                Rules.username()
            ])
            .addField('email', [Rules.required('Email'), Rules.email()])
            .addField('password', [Rules.password(6)])
            .addField('confirmPassword', [
                Rules.required('Password confirmation'),
                Rules.matches('password', 'The two passwords do not match')
            ]);

        /* Re-check the confirmation box when the first password changes */
        var passwordInput = document.getElementById('password');
        passwordInput.addEventListener('input', function () {
            var confirm = document.getElementById('confirmPassword');
            if (confirm.value.length > 0) {
                validator.validateField('confirmPassword');
            }
        });

        wireAvailabilityCheck(
            'username',
            form.getAttribute('data-check-username-url'),
            'That username is free',
            'That username is already taken'
        );

        wireAvailabilityCheck(
            'email',
            form.getAttribute('data-check-email-url'),
            'That email is free',
            'An account already uses that email'
        );

        var submitButton = form.querySelector('button[type="submit"]');

        form.addEventListener('submit', function (event) {
            if (!validator.validateAll()) {
                event.preventDefault();
                showFormMessage(form, 'Check the highlighted fields and try again.', 'error');
                return;
            }

            event.preventDefault();
            clearFormMessage(form);
            setBusy(submitButton, true, 'Creating account…');

            var payload = new URLSearchParams();
            payload.append('firstName', document.getElementById('firstName').value.trim());
            payload.append('lastName', document.getElementById('lastName').value.trim());
            payload.append('username', document.getElementById('username').value.trim());
            payload.append('email', document.getElementById('email').value.trim());
            payload.append('password', document.getElementById('password').value);
            payload.append('confirmPassword', document.getElementById('confirmPassword').value);
            payload.append('__RequestVerificationToken', antiForgeryToken(form));

            fetch(form.getAttribute('data-ajax-url'), {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: payload.toString()
            })
                .then(function (response) { return response.json(); })
                .then(function (data) {
                    if (data.success) {
                        showFormMessage(form, 'Account created. Taking you to sign in…', 'success');
                        setTimeout(function () {
                            window.location.href = data.redirectUrl || '/Auth/Login';
                        }, 900);
                    } else {
                        setBusy(submitButton, false);

                        /* Paint any field-level errors the server sent back */
                        if (data.errors) {
                            Object.keys(data.errors).forEach(function (key) {
                                var id = key.charAt(0).toLowerCase() + key.slice(1);
                                var box = document.querySelector('[data-error-for="' + id + '"]');
                                var input = document.getElementById(id);
                                if (box) {
                                    box.textContent = data.errors[key];
                                    box.classList.add('is-visible');
                                }
                                if (input) { input.classList.add('is-invalid'); }
                            });
                        }

                        showFormMessage(form, data.message || 'The account could not be created.', 'error');
                    }
                })
                .catch(function () {
                    setBusy(submitButton, false);
                    showFormMessage(form, 'The server could not be reached. Check your connection and try again.', 'error');
                });
        });
    }

    /* ------------------------------------------------------------------
       Start
       ------------------------------------------------------------------ */
    document.addEventListener('DOMContentLoaded', function () {
        wirePasswordToggles();
        wireStrengthMeter();
        initLogin();
        initRegister();

        /* Put the cursor in the first empty field */
        var first = document.querySelector('.auth-form input:not([type="checkbox"])');
        if (first && first.value === '') { first.focus(); }
    });

})();
