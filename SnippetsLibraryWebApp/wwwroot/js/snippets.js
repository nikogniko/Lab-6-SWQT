$(document).ready(function () {

    // Отримання поточної сторінки
    let currentPage = $('#currentPage').data('page');
    let ajaxUrl = '';

    switch (currentPage) {
        case 'AllSnippets':
            ajaxUrl = '/Snippets/AllSnippets';
            break;
        case 'FavoriteSnippets':
            ajaxUrl = '/Snippets/FavoriteSnippets';
            break;
        case 'MySnippets':
            ajaxUrl = '/Snippets/MySnippets';
            break;
        default:
            ajaxUrl = '/Snippets/AllSnippets';
    }

    console.log("Current page:", currentPage);
    console.log("AJAX URL:", ajaxUrl);

    // Об'єкти для збереження обраних фільтрів
    let selectedAuthors = [];
    let selectedCategories = [];
    let selectedTags = [];
    let selectedLanguages = [];
    let selectedSortOrder = '';

    // Функція для відображення обраних фільтрів
    function renderSelectedFilters() {
        // Авторами
        $('#selectedAuthors').empty();
        selectedAuthors.forEach(author => {
            $('#selectedAuthors').append(`
                <span class="selected-filter" data-id="${author.ID}">
                    ${author.Username}
                    <span class="remove-filter">&times;</span>
                </span>
            `);
        });

        // Категоріями
        $('#selectedCategories').empty();
        selectedCategories.forEach(category => {
            $('#selectedCategories').append(`
                <span class="selected-filter" data-id="${category.ID}">
                    ${category.Name}
                    <span class="remove-filter">&times;</span>
                </span>
            `);
        });

        // Тегами
        $('#selectedTags').empty();
        selectedTags.forEach(tag => {
            $('#selectedTags').append(`
                <span class="selected-filter" data-id="${tag.ID}">
                    ${tag.Name}
                    <span class="remove-filter">&times;</span>
                </span>
            `);
        });

        // Мовами програмування
        $('#selectedLanguages').empty();
        selectedLanguages.forEach(language => {
            $('#selectedLanguages').append(`
                <span class="selected-filter" data-id="${language.ID}">
                    ${language.Name}
                    <span class="remove-filter">&times;</span>
                </span>
            `);
        });
    }

    // Обробка вибору сортування
    $('#sortOrder').on('change', function () {
        selectedSortOrder = $(this).val();
        console.log("Selected sort order:", selectedSortOrder); // Для налагодження
    });

    // Обробка пошуку авторів
    // Обробка пошуку авторів
    $('#authorSearch').on('input', function () {
        let query = $(this).val().trim();
        console.log("Author search input:", query); // Для налагодження
        if (query.length < 1) {
            $('#authorDropdown').hide();
            return;
        }
        $.ajax({
            url: '/Snippets/SearchAuthors',
            method: 'GET',
            data: { query: query },
            success: function (data) {
                console.log("Author search results:", data); // Для налагодження
                $('#authorDropdown').empty();
                if (data.length > 0) {
                    data.forEach(author => {
                        $('#authorDropdown').append(`<div class="dropdown-item" data-id="${author.id}">${author.username}</div>`);
                    });
                    $('#authorDropdown').show();
                } else {
                    $('#authorDropdown').hide();
                }
            },
            error: function (xhr, status, error) {
                console.error("Error fetching authors:", error);
                $('#authorDropdown').hide();
            }
        });
    });

    // Обробка вибору автора (делегування події)
    $('#authorDropdown').on('click', '.dropdown-item', function () {
        let id = $(this).data('id');
        let username = $(this).text();
        console.log(`Selected author: ${username} (ID: ${id})`); // Для налагодження
        // Перевірка, чи вже обрано
        if (!selectedAuthors.some(a => a.ID === id)) {
            selectedAuthors.push({ ID: id, Username: username });
            renderSelectedFilters();
        }
        $('#authorSearch').val('');
        $('#authorDropdown').hide();
    });

    // Обробка видалення обраного автора (делегування події)
    $('#selectedAuthors').on('click', '.remove-filter', function () {
        let parent = $(this).parent();
        let id = parent.data('id');
        console.log(`Removing author ID: ${id}`); // Для налагодження
        selectedAuthors = selectedAuthors.filter(a => a.ID !== id);
        renderSelectedFilters();
    });

    // Обробка пошуку тегів
    $('#tagSearch').on('input', function () {
        let query = $(this).val().trim();
        console.log("Tag search input:", query); // Для налагодження
        if (query.length < 1) {
            $('#tagDropdown').hide();
            return;
        }
        $.ajax({
            url: '/Snippets/SearchTags',
            method: 'GET',
            data: { query: query },
            success: function (data) {
                console.log("Tag search results:", data); // Для налагодження
                $('#tagDropdown').empty();
                if (data.length > 0) {
                    data.forEach(tag => {
                        $('#tagDropdown').append(`<div class="dropdown-item" data-id="${tag.id}">${tag.name}</div>`);
                    });
                    $('#tagDropdown').show();
                } else {
                    $('#tagDropdown').hide();
                }
            },
            error: function (xhr, status, error) {
                console.error("Error fetching tags:", error);
                $('#tagDropdown').hide();
            }
        });
    });

    // Обробка вибору тегу (делегування події)
    $('#tagDropdown').on('click', '.dropdown-item', function () {
        let id = $(this).data('id');
        let name = $(this).text();
        console.log(`Selected tag: ${name} (ID: ${id})`); // Для налагодження
        // Перевірка, чи вже обрано
        if (!selectedTags.some(t => t.ID === id)) {
            selectedTags.push({ ID: id, Name: name });
            renderSelectedFilters();
        }
        $('#tagSearch').val('');
        $('#tagDropdown').hide();
    });

    // Обробка видалення обраного тегу (делегування події)
    $('#selectedTags').on('click', '.remove-filter', function () {
        let parent = $(this).parent();
        let id = parent.data('id');
        console.log(`Removing tag ID: ${id}`); // Для налагодження
        selectedTags = selectedTags.filter(t => t.ID !== id);
        renderSelectedFilters();
    });

    // Обробка пошуку категорій
    $('#categorySearch').on('input', function () {
        let query = $(this).val().toLowerCase();
        console.log("Category search input:", query); // Для налагодження
        $('#categoryDropdown .dropdown-item').each(function () {
            let text = $(this).text().toLowerCase();
            if (text.includes(query)) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
        $('#categoryDropdown').show();
    });

    // Обробка вибору категорії (делегування події)
    $('#categoryDropdown').on('click', '.dropdown-item', function () {
        let id = $(this).data('id');
        let name = $(this).text();
        console.log(`Selected category: ${name} (ID: ${id})`); // Для налагодження
        // Перевірка, чи вже обрано
        if (!selectedCategories.some(c => c.ID === id)) {
            selectedCategories.push({ ID: id, Name: name });
            renderSelectedFilters();
        }
        $('#categorySearch').val('');
        $('#categoryDropdown').hide();
    });

    // Обробка видалення обраної категорії (делегування події)
    $('#selectedCategories').on('click', '.remove-filter', function () {
        let parent = $(this).parent();
        let id = parent.data('id');
        console.log(`Removing category ID: ${id}`); // Для налагодження
        selectedCategories = selectedCategories.filter(c => c.ID !== id);
        renderSelectedFilters();
    });

    // Обробка пошуку мов програмування
    $('#languageSearch').on('input', function () {
        let query = $(this).val().toLowerCase();
        console.log("Language search input:", query); // Для налагодження
        $('#languageDropdown .dropdown-item').each(function () {
            let text = $(this).text().toLowerCase();
            if (text.includes(query)) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
        $('#languageDropdown').show();
    });

    // Обробка вибору мови програмування (делегування події)
    $('#languageDropdown').on('click', '.dropdown-item', function () {
        let id = $(this).data('id');
        let name = $(this).text();
        console.log(`Selected language: ${name} (ID: ${id})`); // Для налагодження
        // Перевірка, чи вже обрано
        if (!selectedLanguages.some(l => l.ID === id)) {
            selectedLanguages.push({ ID: id, Name: name });
            renderSelectedFilters();
        }
        $('#languageSearch').val('');
        $('#languageDropdown').hide();
    });

    // Обробка видалення обраної мови програмування (делегування події)
    $('#selectedLanguages').on('click', '.remove-filter', function () {
        let parent = $(this).parent();
        let id = parent.data('id');
        console.log(`Removing language ID: ${id}`); // Для налагодження
        selectedLanguages = selectedLanguages.filter(l => l.ID !== id);
        renderSelectedFilters();
    });

    // Закриття випадаючих списків при кліку за межами
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.dropdown-container').length) {
            $('.dropdown-list').hide();
        }
    });

    // Обробка натискання на кнопку "Застосувати фільтри"
    // Обробка натискання на кнопку "Застосувати фільтри"
    $('#applyFilters').on('click', function () {
        console.log("Apply Filters button clicked");

        // Збір ID обраних фільтрів
        let authorIds = selectedAuthors.map(a => a.ID);
        let categoryIds = selectedCategories.map(c => c.ID);
        let tagIds = selectedTags.map(t => t.ID);
        let languageIds = selectedLanguages.map(l => l.ID);
        let sortOrder = selectedSortOrder; // Додано

        console.log("Applying filters:", { authorIds, categoryIds, tagIds, languageIds, sortOrder });

        // Виконання AJAX запиту для отримання відфільтрованих сніпетів
        $.ajax({
            url: ajaxUrl, // Використання визначеного AJAX URL
            method: 'GET',
            traditional: true, // Переконайтесь, що цей параметр додано
            data: {
                authorIds: authorIds,
                categoryIds: categoryIds,
                tagIds: tagIds,
                languageIds: languageIds,
                sortOrder: sortOrder // Додано
            },
            success: function (data) {
                console.log("Filtered snippets received.");
                $('#snippetsContent').html(data); // Пряме вставлення отриманого HTML
            },
            error: function () {
                console.error('Error applying filters.');
                alert('Сталася помилка при застосуванні фільтрів.');
            }
        });
    });

    // Делегування подій для динамічно доданих елементів сніпетів
    $(document).on('click', '.snippet-card', function (e) {
        // Перевіряємо, чи клікували на кнопку "Деталі"
        if ($(e.target).hasClass('details-btn')) {
            e.preventDefault();
            e.stopPropagation();
            return;
        }

        var extra = $(this).find('.snippet-extra');
        extra.slideToggle(300);
    });

    $(document).on('click', '.details-btn', function (e) {
        e.preventDefault();
        e.stopPropagation();

        var snippetCard = $(this).closest('.snippet-card');
        var snippetId = snippetCard.closest('[data-id]').data('id');
        // Перенаправлення на сторінку деталей сніпета
        window.open('/Snippets/Details/' + snippetId, '_blank');
    });

    $('.filter-button').click(function () {
        $('.filter-form').slideToggle();
    });
});
