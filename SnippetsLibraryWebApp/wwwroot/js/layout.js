$(document).ready(function () {

    // Function to show login modal
    $('#openLoginModal').on('click', function () {
        $('#loginModal').fadeIn();
    });

    // Function to show register modal
    $('#openRegisterModal').on('click', function () {
        $('#registerModal').fadeIn();
    });

    // Function to close modals
    $('.close-register-modal').on('click', function () {
        $('#registerModal').fadeOut();
        resetRegisterForm();
    });

    $('.close-login-modal').on('click', function () {
        $('#loginModal').fadeOut();
        resetLoginForm();
    });

    // Function to handle logout
    $('#logout').on('click', function (e) {
        e.preventDefault();
        $.ajax({
            type: 'POST',
            url: '/Account/Logout',
            data: {
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function () {
                location.reload();
            },
            error: function () {
                alert("Помилка при виході. Спробуйте пізніше.");
            }
        });
    });

    // Helper functions to reset forms
    function resetLoginForm() {
        $('#loginForm')[0].reset();
        $('#loginForm').find('.has-error').removeClass('has-error');
    }

    function resetRegisterForm() {
        $('#registerForm')[0].reset();
        $('#registerForm').find('.has-error').removeClass('has-error');
    }

});
