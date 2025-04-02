// wwwroot/js/addSnippet.js

document.addEventListener('DOMContentLoaded', function () {
    const maxCategories = 3;
    const maxTags = 6; // Встановіть ліміт для тегів за потребою або видаліть, якщо без ліміту
    var language = languageName || "plaintext"

    const languageModes = {
        bash: "shell",
        c: "text/x-csrc", // Режим для C
        csharp: "text/x-csharp", // Режим для C#
        cpp: "text/x-c++src", // Режим для C++
        css: "css",
        dart: "dart",
        elixir: "plaintext",
        erlang: "plaintext",
        fsharp: "plaintext",
        go: "go",
        groovy: "plaintext",
        haskell: "plaintext",
        html: "htmlmixed",
        java: "text/x-java", // Режим для Java
        javascript: "javascript",
        json: "application/json",
        julia: "plaintext",
        kotlin: "plaintext",
        lua: "lua",
        matlab: "plaintext",
        objectivec: "text/x-objectivec", // Режим для Objective-C
        other: "plaintext",
        perl: "perl",
        php: "php",
        plsql: "sql",
        powershell: "plaintext",
        python: "python",
        r: "plaintext",
        ruby: "ruby",
        rust: "rust",
        scala: "plaintext",
        shell: "shell",
        sql: "sql",
        swift: "swift",
        tsql: "sql",
        typescript: "javascript",
        vbnet: "plaintext",
        xml: "xml",
        yaml: "yaml",
    };

    language = language == "C#" ? "csharp" :
        language == "C++" ? "cpp" :
            language == "F#" ? "fsharp" :
                language == "VB.NET" ? "vbnet" :
                    language == "PL/SQL" ? "plsql" :
                        language == "Objective-C" ? "objectivec" :
                            language == "T-SQL" ? "tsql" :
                                language == "Visual Basic" ? "vbnet" :
                                    language == "Shell script" ? "shell" : language

    var mode = languageModes[language.toString().toLowerCase()] || "plaintext"

    // Перевірте, чи присутній будь-який з форм
    var isAddForm = $('#addSnippetForm').length > 0;
    var isEditForm = $('#editSnippetForm').length > 0;

    if (isAddForm) {
        initializeForm('#addSnippetForm', '/Snippets/CreateSnippetAsync', 'add');
    }

    if (isEditForm) {
        initializeForm('#editSnippetForm', '/Snippets/EditSnippet', 'edit');
    }

    function initializeForm(formSelector, submitUrl, formType) {
        const form = $(formSelector);

        // Ініціалізація випадайок
        initializeDropdown(form.find('.category-select-container #categoryDropdown'), '.category-checkbox', form.find('#selectedCategories'), maxCategories, 'category', true);
        initializeDropdown(form.find('.tag-select-container #tagDropdown'), '.tag-checkbox', form.find('#selectedTags'), maxTags, 'tag', true);

        // Ініціалізація CodeMirror
        var editor = CodeMirror.fromTextArea(document.getElementById('Code'), {
            mode: mode,
            theme: "monokai",
            lineNumbers: true,
            matchBrackets: true,
            autoCloseTags: true,
            lineWrapping: true,
        });
        editor.save();

        // Обробка зміни мови програмування
        form.find('#ProgrammingLanguageID').on('change', function (e) {
            language = form.find('#ProgrammingLanguageID option:selected').text();
            language = language == "C#" ? "csharp" :
                language == "C++" ? "cpp" :
                    language == "F#" ? "fsharp" :
                        language == "VB.NET" ? "vbnet" :
                            language == "PL/SQL" ? "plsql" :
                                language == "Objective-C" ? "objectivec" :
                                    language == "T-SQL" ? "tsql" :
                                        language == "Visual Basic" ? "vbnet" :
                                            language == "Shell script" ? "shell" : language

            var mode = languageModes[language.toString().toLowerCase()] || "plaintext"
            editor.setOption("mode", mode);
        });

        // Обробка відправки форми
        form.on('submit', function (e) {
            e.preventDefault();

            // Валідація вибору категорій і тегів
            var selectedCategories = form.find('.category-checkbox:checked').length;
            var selectedTags = form.find('.tag-checkbox:checked').length;

            if (selectedCategories > maxCategories) {
                alert("Ви можете обрати максимум 3 категорії.");
                return;
            }

            if (selectedTags > maxTags) {
                alert("Ви можете обрати максимум 6 тегів.");
                return;
            }

            // Синхронізація CodeMirror з textarea
            $('#Code').val(editor.getValue());

            // Збирання даних форми
            var formData = form.serializeArray();
            var data = {};
            formData.forEach(function (item) {
                if (data[item.name]) {
                    if (Array.isArray(data[item.name])) {
                        data[item.name].push(item.value);
                    } else {
                        data[item.name] = [data[item.name], item.value];
                    }
                } else {
                    data[item.name] = item.value;
                }
            });

            const categoryCheckboxes = document.querySelectorAll('.category-checkbox:checked');
            const tagCheckboxes = document.querySelectorAll('.tag-checkbox:checked');

            // Отримання ID вибраних категорій
            const selectedCategoriesList = Array.from(categoryCheckboxes).map(checkbox => parseInt(checkbox.value));

            // Отримання ID вибраних тегів
            const selectedTagsList = Array.from(tagCheckboxes).map(checkbox => parseInt(checkbox.value));

            data.Code = $('#Code')[0].value
            data.Categories = selectedCategoriesList
            data.Tags = selectedTagsList
            data.userID = userID

            // Відправлення даних через AJAX
            $.ajax({
                url: submitUrl,
                type: "POST",
                dataType: "json",
                data: data,
                success: function (response) {
                    if (formType === 'add') {
                        if (response.snippetID) {
                            alert(`Сніпет успішно додано! ID: ${response.snippetID}`);
                            window.location.href = `/Snippets/Details/${response.snippetID}`;
                        } else {
                            alert('При створенні сніпета виникла помилка!');
                        }
                    } else if (formType === 'edit') {
                        if (response.success) {
                            alert('Сніпет успішно оновлено!');
                            window.location.href = `/Snippets/Details/${data.ID}`;
                        } else {
                            alert('При оновленні сніпета виникла помилка!');
                        }
                    }
                },
                error: function () {
                    alert('Сталася помилка при відправці запиту.');
                }
            });
        });

        // Автоматичне змінювання висоти textarea для опису та назви
        form.find('textarea#Description, textarea#Title').on('input', function () {
            autoResize(this);
        });

        // Функція для автозміни розміру textarea
        function autoResize(element) {
            element.style.height = 'auto'; // Спочатку встановлюємо висоту на автоматичну
            element.style.height = (element.scrollHeight) + 'px'; // Змінюємо висоту на основі scrollHeight
        }

        // Обробка додавання нових тегів (за потреби)
        form.find('#addTagBtn').on('click', function () {
            var newTag = form.find('#addTag').val().trim();
            if (newTag) {
                // Відправляємо POST-запит на сервер для додавання тега
                $.ajax({
                    url: '/Tags/AddTag',
                    type: 'POST',
                    dataType: 'json',
                    data: {
                        tagName: newTag
                    },
                    success: function (response) {
                        if (response.success) {
                            alert('Тег додано успішно!');
                            loadTags();
                        } else {
                            alert('Не вдалося додати тег. Спробуйте ще раз.');
                        }
                    }
                })
            }
        });
    }

    function loadTags() {
        // Отримання поточних вибраних тегів з DOM
        let selectedTagIds = [];
        $('#selectedTags .selected-tag').each(function () {
            selectedTagIds.push(parseInt($(this).data('id')));
        });

        $.ajax({
            url: '/Tags/GetAllTags', // Шлях до вашого ендпойнту
            type: 'GET',
            dataType: 'json',
            success: function (tags) {
                // Очищаємо поточний список тегів
                $('#dropdownTagList').empty();

                // Додаємо отримані теги до дропдауну з відміткою вибраних
                tags.forEach(function (tag) {
                    const isChecked = selectedTagIds.includes(tag.id) ? 'checked' : '';
                    const checkboxLabel = `
                        <label class="checkbox-label">
                            <input type="checkbox" class="tag-checkbox" value="${tag.id}" ${isChecked} />
                            ${tag.name}
                        </label>
                    `;
                    $('#dropdownTagList').append(checkboxLabel);
                });

                // Оновлюємо відображення вибраних тегів
                updateSelectedTagsDisplay();
            },
            error: function (xhr, status, error) {
                console.error('Error loading tags:', error);
                alert('Сталася помилка при завантаженні тегів.');
            }
        });
    }

    function updateSelectedTagsDisplay() {
        // Функція для оновлення відображення вибраних тегів після перезавантаження списку
        const selectedTags = [];

        $('.tag-checkbox:checked').each(function () {
            selectedTags.push({
                id: parseInt($(this).val()),
                name: $(this).closest('label').text().trim()
            });
        });

        const selectedList = $('#selectedTags');
        selectedList.empty();

        selectedTags.forEach(function (tag) {
            const tagSpan = $('<span>', {
                class: 'selected-tag',
                'data-id': tag.id,
                text: tag.name
            });

            const removeBtn = $('<span>', {
                class: 'remove-tag',
                text: ' ×',
                'aria-label': `Remove ${tag.name}`,
                role: 'button',
                tabindex: '0'
            });

            tagSpan.append(removeBtn);
            selectedList.append(tagSpan);
        });

        // Оновлення плейсхолдера випадайки
        if (selectedTags.length > 0) {
            $('#tagsFilter .dropdown-placeholder').text(`${selectedTags.length} обрано`);
        } else {
            $('#tagsFilter .dropdown-placeholder').text(`--Оберіть теги--`);
        }
    }

    function initializeDropdown(dropdownSelector, checkboxSelector, selectedListSelector, maxSelection, type, hasRemoveButton) {
        const dropdown = $(dropdownSelector);
        const dropdownList = dropdown.find('.dropdown-content');
        const selectedList = $(selectedListSelector);

        // Перемикання видимості списку при кліку на контейнер випадайки
        dropdown.on('click', function (e) {
            e.stopPropagation(); // Запобігаємо поширенню кліку до документа
            $('.dropdown-content').not(dropdownList).hide(); // Приховуємо інші випадайки
            dropdownList.toggle();
        });

        // Приховування випадайки при кліку поза нею
        $(document).on('click', function () {
            dropdownList.hide();
        });

        // Запобігання закриттю випадайки при кліку всередині її вмісту
        dropdownList.on('click', function (e) {
            e.stopPropagation();
        });

        // Делегування події зміни для чекбоксів
        dropdownList.on('change', checkboxSelector, function () {
            updateSelectedItems();
        });

        // Функція для оновлення відображення вибраних елементів
        function updateSelectedItems() {
            const selected = [];
            $(checkboxSelector + ':checked').each(function () {
                selected.push({
                    id: $(this).val(),
                    name: $(this).closest('label').text().trim()
                });
            });

            // Дотримання ліміту вибору
            if (selected.length > maxSelection) {
                alert(`Ви можете обрати максимум ${maxSelection} ${type === 'category' ? 'категорії' : 'теги'}.`);
                $(this).prop('checked', false);
                return;
            }

            // Оновлення відображення вибраних елементів
            selectedList.empty();
            selected.forEach(function (item) {
                const itemTag = $('<span>', {
                    class: `selected-${type}`,
                    'data-id': item.id,
                    text: item.name
                });

                if (hasRemoveButton) {
                    const removeBtn = $('<span>', {
                        class: `remove-${type}`,
                        text: ' ×', // Пробіл перед хрестиком для кращого відступу
                        'aria-label': `Remove ${item.name}`,
                        role: 'button',
                        tabindex: '0' // Робимо фокусованим
                    });

                    // Додаємо хрестик до тегу
                    itemTag.append(removeBtn);
                }

                // Додаємо тег до списку вибраних
                selectedList.append(itemTag);
            });

            // Оновлення плейсхолдера випадайки
            if (selected.length > 0) {
                dropdown.find('.dropdown-placeholder').text(`${selected.length} обрано`);
            } else {
                dropdown.find('.dropdown-placeholder').text(`--Оберіть ${type === 'category' ? 'до 3-х категорій' : 'теги'}--`);
            }
        }

        // Делегування події кліку для видалення категорій/тегів
        if (hasRemoveButton) {
            selectedList.on('click', `.remove-${type}`, function () {
                const itemTag = $(this).parent(`.selected-${type}`);
                const itemId = itemTag.data('id');

                // Відмічаємо відповідний чекбокс
                $(`${checkboxSelector}[value="${itemId}"]`).prop('checked', false);

                // Видаляємо тег з відображення
                itemTag.remove();

                // Оновлення плейсхолдера випадайки
                const selectedCount = $(checkboxSelector + ':checked').length;
                if (selectedCount > 0) {
                    dropdown.find('.dropdown-placeholder').text(`${selectedCount} обрано`);
                } else {
                    dropdown.find('.dropdown-placeholder').text(`--Оберіть ${type === 'category' ? 'до 3-х категорій' : 'теги'}--`);
                }
            });

            // Обробка клавіатурних подій для видалення категорій/тегів
            selectedList.on('keydown', `.remove-${type}`, function (e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    $(this).click();
                }
            });
        }
    }
});
