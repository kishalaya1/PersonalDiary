document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-rich-text-editor]').forEach((editor) => {
        const surface = editor.querySelector('[data-editor-surface]');
        const value = editor.querySelector('[data-editor-value]');
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
                control.addEventListener('click', () => executeCommand(control.dataset.command));
            }
        });

        surface.closest('form')?.addEventListener('submit', () => {
            syncValue();
        });
    });
});
