(function (global) {
    'use strict';

    const apiBase = '/api/files';

    class ApiError extends Error {
        constructor(message, options = {}) {
            super(message);
            this.name = 'ApiError';
            this.code = options.code || 'unknown';
            this.status = options.status || 0;
            this.correlationId = options.correlationId || null;
        }
    }

    async function parseBody(response) {
        const contentType = response.headers.get('content-type') || '';
        if (contentType.includes('application/json')) {
            try { return await response.json(); } catch { return null; }
        }
        const body = await response.text().catch(() => '');
        return body ? { message: body.slice(0, 500) } : null;
    }

    function randomRequestId() {
        return global.crypto?.randomUUID?.() || Math.random().toString(36).slice(2);
    }

    const FilesClient = {
        upload(file, options) {
            if (!(file instanceof File))
                return Promise.reject(new ApiError('Не выбран файл', { code: 'no_file' }));

            return new Promise((resolve, reject) => {
                const form = new FormData();
                form.append('file', file, file.name);
                const xhr = new XMLHttpRequest();
                xhr.open('POST', apiBase);
                xhr.setRequestHeader('X-Api-Key', options.apiKey);
                xhr.setRequestHeader('X-Request-Id', randomRequestId());
                xhr.upload.onprogress = event => {
                    if (event.lengthComputable && options.onProgress)
                        options.onProgress(Math.round(event.loaded / event.total * 100));
                };
                xhr.onload = () => {
                    let body = null;
                    try { body = JSON.parse(xhr.responseText); } catch { /* non-JSON response */ }
                    const correlationId = xhr.getResponseHeader('X-Request-Id');
                    if (xhr.status >= 200 && xhr.status < 300) {
                        resolve({ data: body, status: xhr.status, correlationId });
                        return;
                    }
                    reject(new ApiError(body?.message || `Ошибка ${xhr.status}`, {
                        code: body?.code || 'client_error', status: xhr.status, correlationId
                    }));
                };
                xhr.onerror = () => reject(new ApiError('Сеть недоступна', { code: 'network_error' }));
                xhr.send(form);
            });
        },

        async download(id, options) {
            if (!/^[0-9a-f]{32}$/i.test(id))
                throw new ApiError('Некорректный ID файла', { code: 'bad_id' });

            const response = await fetch(`${apiBase}/${encodeURIComponent(id)}`, {
                headers: { 'X-Api-Key': options.apiKey, 'X-Request-Id': randomRequestId() },
                credentials: 'same-origin'
            });
            if (!response.ok) {
                const body = await parseBody(response);
                throw new ApiError(body?.message || `Ошибка ${response.status}`, {
                    code: body?.code || 'client_error',
                    status: response.status,
                    correlationId: response.headers.get('X-Request-Id')
                });
            }
            const disposition = response.headers.get('content-disposition') || '';
            const match = /filename\*=UTF-8''([^;]+)/i.exec(disposition);
            return {
                blob: await response.blob(),
                filename: match ? decodeURIComponent(match[1]) : id
            };
        }
    };

    global.FilesClient = FilesClient;
    global.FilesApiError = ApiError;

    const uploadForm = document.querySelector('#upload-form');
    if (!uploadForm) return;

    const fileInput = document.querySelector('#file-input');
    const fileName = document.querySelector('#file-name');
    const result = document.querySelector('#result');
    const progress = document.querySelector('.progress');
    const progressBar = document.querySelector('#progress-bar');

    fileInput.addEventListener('change', () => {
        fileName.textContent = fileInput.files[0]?.name || 'или перетащите его сюда';
    });

    uploadForm.addEventListener('submit', async event => {
        event.preventDefault();
        result.className = 'result';
        result.textContent = '';
        progress.hidden = false;
        progressBar.style.width = '0%';
        try {
            const response = await FilesClient.upload(fileInput.files[0], {
                apiKey: document.querySelector('#api-key').value,
                onProgress: value => { progressBar.style.width = `${value}%`; }
            });
            document.querySelector('#file-id').value = response.data.id;
            result.classList.add('success');
            result.textContent = `Файл сохранён. ID: ${response.data.id}`;
        } catch (error) {
            result.classList.add('error');
            result.textContent = `${error.message}${error.correlationId ? ` · Request ID: ${error.correlationId}` : ''}`;
        }
    });

    document.querySelector('#download-form').addEventListener('submit', async event => {
        event.preventDefault();
        try {
            const response = await FilesClient.download(document.querySelector('#file-id').value.trim(), {
                apiKey: document.querySelector('#api-key').value
            });
            const url = URL.createObjectURL(response.blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = response.filename;
            link.click();
            URL.revokeObjectURL(url);
        } catch (error) {
            result.className = 'result error';
            result.textContent = error.message;
        }
    });
})(window);
