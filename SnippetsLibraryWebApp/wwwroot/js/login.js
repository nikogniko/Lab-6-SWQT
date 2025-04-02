// wwwroot/js/login.js

$(document).ready(function () {

    // Function to close the login modal
    $('.close-login-modal').on('click', function () {
        $('#loginModal').fadeOut();
        resetLoginForm();
    });

    // Function to toggle password visibility
    $('.toggle-login-password').on('click', function () {
        var loginInput = $('#loginPassword');
        var type = loginInput.attr('type') === 'password' ? 'text' : 'password';
        loginInput.attr('type', type);
        $(this).text(type === 'password' ? 'Show' : 'Hide');
    });

    // Function to handle login form submission
    $('#loginForm').on('submit', function (e) {
        e.preventDefault();
        var email = $('#loginEmail').val();
        var password = $('#loginPassword').val();

        // Basic validation
        if (!email || !password) {
            alert("Будь ласка, заповніть всі поля.");
            return;
        }

        // Get the anti-forgery token
        var token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            type: 'POST',
            url: '/Account/Login', // Переконайтесь, що шлях правильний
            data: {
                __RequestVerificationToken: token,
                Email: email,
                Password: password
            },
            success: function (responseLogin) {
                if (responseLogin.success) {
                    // Закрити модальне вікно
                    $('#loginModal').fadeOut();
                    resetLoginForm();

                    // Перезавантажити сторінку, щоб оновити інтерфейс
                    location.reload();
                } else {
                    alert(responseLogin.message);
                }
            },
            error: function () {
                alert("Помилка сервера. Спробуйте пізніше.");
            }
        });
    });

    // Function to switch to registration modal
    $('.btn-to-register').on('click', function () {
        $('#loginModal').fadeOut();
        $('#registerModal').fadeIn();
        resetLoginForm();
    });

    // Helper function to reset login form
    function resetLoginForm() {
        $('#loginForm')[0].reset();
        $('#loginForm').find('.has-error').removeClass('has-error');
    }
});
