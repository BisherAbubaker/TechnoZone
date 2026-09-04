/* ============================================================================
   TechnoZone - validation.js
   A small, dependency-free validation library shared by the login and
   register screens. Every rule returns either null (valid) or a message
   written for the person filling in the form.
   ============================================================================ */

(function (window) {
    'use strict';

    var Rules = {
        required: function (label) {
            return function (value) {
                return value.trim().length === 0 ? label + ' is required' : null;
            };
        },

        minLength: function (label, min) {
            return function (value) {
                return value.trim().length < min
                    ? label + ' needs at least ' + min + ' characters'
                    : null;
            };
        },

        maxLength: function (label, max) {
            return function (value) {
                return value.trim().length > max
                    ? label + ' can be at most ' + max + ' characters'
                    : null;
            };
        },

        /* Letters, numbers, dot and underscore. Keeps usernames URL-safe. */
        username: function () {
            return function (value) {
                return /^[a-zA-Z0-9._]+$/.test(value.trim())
                    ? null
                    : 'Use letters, numbers, dots or underscores only';
            };
        },

        email: function () {
            return function (value) {
                return /^[^\s@]+@[^\s@]+\.[a-zA-Z]{2,}$/.test(value.trim())
                    ? null
                    : 'Enter an email address in the form name@example.com';
            };
        },

        /* At least one letter and one number, so "aaaaaa" is rejected. */
        password: function (min) {
            return function (value) {
                if (value.length < min) {
                    return 'Password needs at least ' + min + ' characters';
                }
                if (!/[a-zA-Z]/.test(value) || !/[0-9]/.test(value)) {
                    return 'Password needs at least one letter and one number';
                }
                return null;
            };
        },

        matches: function (otherFieldId, label) {
            return function (value) {
                var other = document.getElementById(otherFieldId);
                if (!other) { return null; }
                return value === other.value ? null : label;
            };
        }
    };

    /* ------------------------------------------------------------------
       Scores a password from 0 to 4 and returns a label to display.
       ------------------------------------------------------------------ */
    function scorePassword(value) {
        var score = 0;
        if (value.length >= 6) { score++; }
        if (value.length >= 10) { score++; }
        if (/[a-z]/.test(value) && /[A-Z]/.test(value)) { score++; }
        if (/[0-9]/.test(value)) { score++; }
        if (/[^a-zA-Z0-9]/.test(value)) { score++; }
        if (value.length === 0) { score = 0; }

        score = Math.min(score, 4);

        var labels = ['', 'Weak', 'Fair', 'Good', 'Strong'];
        return { score: score, label: labels[score] };
    }

    /* ------------------------------------------------------------------
       Validator: wires a set of fields to a form and reports errors
       under each input as the person types.
       ------------------------------------------------------------------ */
    function Validator(formId) {
        this.form = document.getElementById(formId);
        this.fields = {};
    }

    Validator.prototype.addField = function (inputId, rules) {
        var input = document.getElementById(inputId);
        if (!input) { return this; }

        this.fields[inputId] = { input: input, rules: rules, touched: false };

        var self = this;

        input.addEventListener('blur', function () {
            self.fields[inputId].touched = true;
            self.validateField(inputId);
        });

        input.addEventListener('input', function () {
            if (self.fields[inputId].touched) {
                self.validateField(inputId);
            }
        });

        return this;
    };

    Validator.prototype.validateField = function (inputId) {
        var field = this.fields[inputId];
        if (!field) { return true; }

        var value = field.input.value;
        var message = null;

        for (var i = 0; i < field.rules.length; i++) {
            message = field.rules[i](value);
            if (message) { break; }
        }

        this.showFieldState(inputId, message);
        return message === null;
    };

    Validator.prototype.showFieldState = function (inputId, message) {
        var field = this.fields[inputId];
        var errorBox = document.querySelector('[data-error-for="' + inputId + '"]');

        if (message) {
            field.input.classList.add('is-invalid');
            field.input.classList.remove('is-valid');
            field.input.setAttribute('aria-invalid', 'true');
            if (errorBox) {
                errorBox.textContent = message;
                errorBox.classList.add('is-visible');
            }
        } else {
            field.input.classList.remove('is-invalid');
            if (field.input.value.trim().length > 0) {
                field.input.classList.add('is-valid');
            } else {
                field.input.classList.remove('is-valid');
            }
            field.input.removeAttribute('aria-invalid');
            if (errorBox) {
                errorBox.textContent = '';
                errorBox.classList.remove('is-visible');
            }
        }
    };

    /* Validates every field. Returns true when the whole form passes. */
    Validator.prototype.validateAll = function () {
        var allValid = true;
        var firstInvalid = null;

        for (var id in this.fields) {
            if (!Object.prototype.hasOwnProperty.call(this.fields, id)) { continue; }
            this.fields[id].touched = true;
            var ok = this.validateField(id);
            if (!ok) {
                allValid = false;
                if (!firstInvalid) { firstInvalid = this.fields[id].input; }
            }
        }

        if (firstInvalid) { firstInvalid.focus(); }
        return allValid;
    };

    /* Expose the module */
    window.TZValidation = {
        Rules: Rules,
        Validator: Validator,
        scorePassword: scorePassword
    };

})(window);
