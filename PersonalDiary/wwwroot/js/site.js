document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-rich-text-editor]').forEach((editor) => {
        const surface = editor.querySelector('[data-editor-surface]');
        const value = editor.querySelector('[data-editor-value]');
        const imagePicker = editor.querySelector('[data-image-picker]');
        const maximumImageSize = 2 * 1024 * 1024;
        let savedRange;

        if (value.value && /<\/?[a-z][\s\S]*>/i.test(value.value)) {
            surface.innerHTML = value.value;
        } else {
            surface.textContent = value.value;
        }

        const saveSelection = () => {
            const selection = window.getSelection();
            if (selection.rangeCount && surface.contains(selection.anchorNode)) {
                savedRange = selection.getRangeAt(0);
            }
        };

        const restoreSelection = () => {
            if (!savedRange) {
                surface.focus();
                return;
            }

            const selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(savedRange);
            surface.focus();
        };

        const syncValue = () => {
            value.value = surface.innerHTML;
        };

        const insertImage = (dataUrl, fileName) => {
            restoreSelection();

            const image = document.createElement('img');
            image.src = dataUrl;
            image.alt = fileName;
            image.className = 'diary-inline-image';

            if (savedRange && surface.contains(savedRange.commonAncestorContainer)) {
                savedRange.deleteContents();
                savedRange.insertNode(image);
                savedRange.setStartAfter(image);
                savedRange.collapse(true);
                const selection = window.getSelection();
                selection.removeAllRanges();
                selection.addRange(savedRange);
            } else {
                surface.appendChild(image);
            }

            syncValue();
            saveSelection();
        };

        const readImage = (file) => {
            if (!file.type.startsWith('image/')) {
                window.alert(`${file.name} is not a supported image file.`);
                return;
            }

            if (file.size > maximumImageSize) {
                window.alert(`${file.name} is larger than the 2 MB image limit.`);
                return;
            }

            const reader = new FileReader();
            reader.addEventListener('load', () => insertImage(reader.result, file.name));
            reader.readAsDataURL(file);
        };

        const executeCommand = (command, commandValue = null) => {
            restoreSelection();
            if (command === 'createLink') {
                const url = window.prompt('Enter the link URL:', 'https://');
                if (!url) {
                    return;
                }
                commandValue = url;
            }

            document.execCommand(command, false, commandValue);
            syncValue();
            saveSelection();
        };

        surface.addEventListener('input', syncValue);
        surface.addEventListener('mouseup', saveSelection);
        surface.addEventListener('keyup', saveSelection);
        surface.addEventListener('focus', saveSelection);

        imagePicker?.addEventListener('change', () => {
            Array.from(imagePicker.files ?? []).forEach(readImage);
            imagePicker.value = '';
        });

        editor.querySelectorAll('[data-command]').forEach((control) => {
            if (control.matches('select')) {
                control.addEventListener('change', () => {
                    executeCommand(control.dataset.command, control.value);
                    control.selectedIndex = 0;
                });
            } else if (control.matches('input[type="color"]')) {
                control.addEventListener('click', saveSelection);
                control.addEventListener('input', () => executeCommand(control.dataset.command, control.value));
            } else {
                control.addEventListener('mousedown', (event) => event.preventDefault());
                control.addEventListener('click', () => {
                    if (control.dataset.command === 'insertImage') {
                        saveSelection();
                        imagePicker?.click();
                        return;
                    }

                    executeCommand(control.dataset.command);
                });
            }
        });

        surface.closest('form')?.addEventListener('submit', () => {
            syncValue();
        });
    });
});
