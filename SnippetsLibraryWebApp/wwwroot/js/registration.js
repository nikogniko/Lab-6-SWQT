// wwwroot/js/registration.js

$(document).ready(function () {
    // Function to close the registration modal
    $('.close-register-modal').on('click', function () {
        $('#registerModal').fadeOut();
        resetRegisterForm();
    });

    // Function to toggle password visibility
    $('.toggle-register-password').on('click', function () {
        var passwordInput = $('#registerPassword');
        var type = passwordInput.attr('type') === 'password' ? 'text' : 'password';
        passwordInput.attr('type', type);
        $(this).text(type === 'password' ? 'Show' : 'Hide');
    });

    // Function to handle registration form submission
    $('#registerForm').on('submit', function (e) {
        e.preventDefault();
        var username = $('#registerUsername').val();
        var email = $('#registerEmail').val();
        var password = $('#registerPassword').val();

        // Basic validation
        if (!username || !email || !password) {
            alert("Будь ласка, заповніть всі поля.");
            return;
        }

        if (username.length < 3) {
            alert("Нікнейм повинен містити мінімум 3 символи.");
            return;
        }

        if (password.length < 8 || password.length > 25) {
            alert("Пароль повинен містити 8-25 символів.");
            return;
        }

        // Отримати Anti-Forgery Token
        var token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            type: 'POST',
            url: '/Account/RegisterUser',
            data: {
                __RequestVerificationToken: token, // Додати токен
                username: username,
                email: email,
                password: password
            },
            success: function (response) {
                if (response.success) {
                    // Оновити UI без використання localStorage, оскільки ми використовуємо серверну автентифікацію
                    location.reload(); // Перезавантажити сторінку, щоб оновити навігаційні елементи
                } else {
                    alert(response.message);
                }
            },
            error: function () {
                alert("Помилка сервера. Спробуйте пізніше.");
            }
        });
    });

    // Function to switch to login modal
    $('.btn-to-login').on('click', function () {
        $('#registerModal').fadeOut();
        $('#loginModal').fadeIn();
        resetRegisterForm();
    });

    // Helper function to reset registration form
    function resetRegisterForm() {
        $('#registerForm')[0].reset();
        $('#registerForm').find('.has-error').removeClass('has-error');
    }
});
