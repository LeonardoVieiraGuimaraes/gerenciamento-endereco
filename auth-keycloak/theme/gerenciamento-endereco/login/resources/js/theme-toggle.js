/*
 * Comportamentos compartilhados por TODAS as telas do tema (login, cadastro,
 * 2FA, recuperar senha, erro). Carregado via `scripts=` no theme.properties,
 * então não precisa ser duplicado em cada .ftl.
 *
 * 1. Alternância de tema claro/escuro
 * 2. Botão de "olho" para revelar a senha
 *
 * Convenções que precisam bater com o glass-theme.css:
 *   - tema escuro  -> classe `dark-mode` no <html>
 *   - botão do tema -> id `#theme-toggle-btn`
 *   - olho da senha -> `.btn-password-toggle` dentro de um `.input-group`
 */

(function () {
  var ICON_MOON =
    '<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" viewBox="0 0 16 16">' +
    '<path d="M6 .278a.768.768 0 0 1 .08.858 7.208 7.208 0 0 0-.878 3.46c0 4.021 3.278 7.277 7.318 7.277.527 0 1.04-.055 1.533-.16a.787.787 0 0 1 .81.316.733.733 0 0 1-.031.893A8.349 8.349 0 0 1 8.344 16C3.734 16 0 12.286 0 7.71 0 4.266 2.114 1.312 5.124.06A.752.752 0 0 1 6 .278z"/></svg>';

  var ICON_SUN =
    '<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" viewBox="0 0 16 16">' +
    '<path d="M8 11a3 3 0 1 1 0-6 3 3 0 0 1 0 6m0 1a4 4 0 1 0 0-8 4 4 0 0 0 0 8M8 0a.5.5 0 0 1 .5.5v2a.5.5 0 0 1-1 0v-2A.5.5 0 0 1 8 0m0 13a.5.5 0 0 1 .5.5v2a.5.5 0 0 1-1 0v-2A.5.5 0 0 1 8 13m8-5a.5.5 0 0 1-.5.5h-2a.5.5 0 0 1 0-1h2a.5.5 0 0 1 .5.5M3 8a.5.5 0 0 1-.5.5h-2a.5.5 0 0 1 0-1h2A.5.5 0 0 1 3 8m10.657-5.657a.5.5 0 0 1 0 .707l-1.414 1.415a.5.5 0 1 1-.707-.708l1.414-1.414a.5.5 0 0 1 .707 0m-9.193 9.193a.5.5 0 0 1 0 .707L3.05 13.657a.5.5 0 0 1-.707-.707l1.414-1.414a.5.5 0 0 1 .707 0m9.193 2.121a.5.5 0 0 1-.707 0l-1.414-1.414a.5.5 0 0 1 .707-.707l1.414 1.414a.5.5 0 0 1 0 .707M4.464 4.465a.5.5 0 0 1-.707 0L2.343 3.05a.5.5 0 1 1 .707-.707l1.414 1.414a.5.5 0 0 1 0 .708"/></svg>';

  var ICON_EYE =
    '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="currentColor" viewBox="0 0 16 16">' +
    '<path d="M10.5 8a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0"/>' +
    '<path d="M0 8s3-5.5 8-5.5S16 8 16 8s-3 5.5-8 5.5S0 8 0 8m8 3.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7"/></svg>';

  var ICON_EYE_SLASH =
    '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="currentColor" viewBox="0 0 16 16">' +
    '<path d="m10.79 12.912-1.614-1.615a3.5 3.5 0 0 1-4.474-4.474l-2.06-2.06C.938 6.278 0 8 0 8s3 5.5 8 5.5a7 7 0 0 0 2.79-.588M5.21 3.088A7 7 0 0 1 8 2.5c5 0 8 5.5 8 5.5s-.939 1.721-2.641 3.238l-2.062-2.062a3.5 3.5 0 0 0-4.474-4.474z"/>' +
    '<path d="M5.525 7.646a2.5 2.5 0 0 0 2.829 2.829zm4.95.708-2.829-2.83a2.5 2.5 0 0 1 2.829 2.829zm3.171 6-12-12 .708-.708 12 12z"/></svg>';

  var root = document.documentElement;

  /* Aplica o tema o quanto antes (antes do DOM montar) pra evitar "flash"
     de tela clara quando o usuário escolheu escuro. */
  function temaInicial() {
    var salvo = localStorage.getItem('theme');
    if (salvo === 'dark' || salvo === 'light') return salvo;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  function estaEscuro() {
    return root.classList.contains('dark-mode');
  }

  function aplicarTema(tema) {
    root.classList.toggle('dark-mode', tema === 'dark');
  }

  aplicarTema(temaInicial());

  function pronto(fn) {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', fn);
    } else {
      fn();
    }
  }

  pronto(function () {
    /* ---------- 1. Botão de tema claro/escuro ---------- */
    var btn = document.getElementById('theme-toggle-btn');
    if (!btn) {
      btn = document.createElement('button');
      btn.id = 'theme-toggle-btn';
      btn.type = 'button';
      document.body.appendChild(btn);
    }

    function pintarBotaoTema() {
      var escuro = estaEscuro();
      /* No escuro mostramos o sol (ação: clarear) e vice-versa. */
      btn.innerHTML = escuro ? ICON_SUN : ICON_MOON;
      btn.setAttribute('aria-label', escuro ? 'Mudar para tema claro' : 'Mudar para tema escuro');
      btn.setAttribute('title', escuro ? 'Tema claro' : 'Tema escuro');
    }
    pintarBotaoTema();

    btn.addEventListener('click', function () {
      var novo = estaEscuro() ? 'light' : 'dark';
      aplicarTema(novo);
      localStorage.setItem('theme', novo);
      pintarBotaoTema();
    });

    /* ---------- 2. Olho para revelar a senha ---------- */
    var campos = document.querySelectorAll('input[type="password"]');
    Array.prototype.forEach.call(campos, function (campo) {
      if (campo.dataset.temToggle === '1') return;

      /* Telas renderizadas pelo tema base (trocar senha, 2FA) já podem trazer
         o botão de visibilidade do próprio Keycloak — não duplicar o olho. */
      if (campo.parentElement && campo.parentElement.querySelector('button')) return;

      campo.dataset.temToggle = '1';

      /* O CSS ancora o olho em .input-group (position: relative) e já reserva
         o padding-right no input. Reaproveita o contêiner se ele já existir
         (ex.: o .input-icon-wrap do login), senão embrulha o campo. */
      var wrap = campo.parentElement;
      if (!wrap.classList.contains('input-group') && !wrap.classList.contains('input-icon-wrap')) {
        wrap = document.createElement('div');
        wrap.className = 'input-group';
        campo.parentElement.insertBefore(wrap, campo);
        wrap.appendChild(campo);
      }
      wrap.classList.add('input-group');

      var olho = document.createElement('button');
      olho.type = 'button';
      olho.className = 'btn-password-toggle';
      olho.innerHTML = ICON_EYE;
      olho.setAttribute('aria-label', 'Mostrar senha');
      olho.setAttribute('title', 'Mostrar senha');
      wrap.appendChild(olho);

      olho.addEventListener('click', function () {
        var revelando = campo.getAttribute('type') === 'password';
        campo.setAttribute('type', revelando ? 'text' : 'password');
        olho.innerHTML = revelando ? ICON_EYE_SLASH : ICON_EYE;
        var rotulo = revelando ? 'Ocultar senha' : 'Mostrar senha';
        olho.setAttribute('aria-label', rotulo);
        olho.setAttribute('title', rotulo);
        campo.focus();
      });
    });
  });
})();
