// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// axiosの全リクエストにCSRFトークンを自動付与
const _csrfToken = document.querySelector('meta[name="RequestVerificationToken"]')?.content;
if (_csrfToken) {
    axios.defaults.headers.common['RequestVerificationToken'] = _csrfToken;
}
