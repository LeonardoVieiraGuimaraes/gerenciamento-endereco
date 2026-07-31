<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>${msg("loginTitle",(realm.displayName!''))}</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="${url.resourcesPath}/css/glass-theme.css">
    <link rel="icon" type="image/svg+xml" href="${url.resourcesPath}/img/favicon.svg" />
    <style>
        .alert {
            padding: 1rem;
            border-radius: 10px;
            margin-bottom: 1.5rem;
            text-align: center;
            font-weight: 500;
        }
        .alert-error {
            background: rgba(239, 68, 68, 0.2);
            border: 1px solid rgba(239, 68, 68, 0.5);
            color: #fca5a5;
        }
        .alert-warning {
            background: rgba(245, 158, 11, 0.2);
            border: 1px solid rgba(245, 158, 11, 0.5);
            color: #fcd34d;
        }
        .alert-success {
            background: rgba(16, 185, 129, 0.2);
            border: 1px solid rgba(16, 185, 129, 0.5);
            color: #6ee7b7;
        }
        .alert-info {
            background: rgba(56, 189, 248, 0.2);
            border: 1px solid rgba(56, 189, 248, 0.5);
            color: #7dd3fc;
        }
    </style>
</head>
<body>
    <button id="theme-toggle" class="theme-toggle" aria-label="Toggle theme">
        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" class="bi bi-moon-fill" viewBox="0 0 16 16">
            <path d="M6 .278a.768.768 0 0 1 .08.858 7.208 7.208 0 0 0-.878 3.46c0 4.021 3.278 7.277 7.318 7.277.527 0 1.04-.055 1.533-.16a.787.787 0 0 1 .81.316.733.733 0 0 1-.031.893A8.349 8.349 0 0 1 8.344 16C3.734 16 0 12.286 0 7.71 0 4.266 2.114 1.312 5.124.06A.752.752 0 0 1 6 .278z"/>
        </svg>
    </button>
    <div class="card-pf">
        <h1 id="kc-page-title">Bem-vindo</h1>
        
        <script>
            const themeToggleBtn = document.getElementById('theme-toggle');
            const root = document.documentElement;
            let savedTheme = localStorage.getItem('theme');
            if (!savedTheme) {
                savedTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
            }
            root.setAttribute('data-theme', savedTheme);

            themeToggleBtn.addEventListener('click', () => {
                const currentTheme = root.getAttribute('data-theme');
                const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
                root.setAttribute('data-theme', newTheme);
                localStorage.setItem('theme', newTheme);
            });
        </script>
        
        <#if message?has_content && (message.type != 'warning' || !isAppInitiatedAction??)>
            <div class="alert alert-${message.type}">
                ${kcSanitize(message.summary)?no_esc}
            </div>
        </#if>

        <form id="kc-form-login" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">
            <div class="form-group" style="margin-bottom: 1.5rem;">
                <label for="username" class="control-label">Usuário</label>
                <input id="username" class="form-control" name="username" value="${(login.username!'')}" type="text" autofocus autocomplete="off" placeholder="Digite seu usuário..." />
            </div>

            <div class="form-group" style="margin-bottom: 1.5rem;">
                <label for="password" class="control-label">Senha</label>
                <input id="password" class="form-control" name="password" type="password" autocomplete="off" placeholder="Digite sua senha..." />
            </div>

            <div class="form-group login-pf-settings" style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem;">
                <#if realm.rememberMe && !usernameHidden??>
                    <div class="checkbox">
                        <label>
                            <#if login.rememberMe??>
                                <input tabindex="3" id="rememberMe" name="rememberMe" type="checkbox" checked> Lembrar de mim
                            <#else>
                                <input tabindex="3" id="rememberMe" name="rememberMe" type="checkbox"> Lembrar de mim
                            </#if>
                        </label>
                    </div>
                </#if>

                <#if realm.resetPasswordAllowed>
                    <span><a tabindex="5" href="${url.loginResetCredentialsUrl}">Esqueceu a senha?</a></span>
                </#if>
            </div>

            <div class="form-group">
                <button type="submit" name="login" id="kc-login" class="btn-primary">Entrar no Sistema</button>
            </div>
            
            <#if realm.password && realm.registrationAllowed && !registrationDisabled??>
                <div id="kc-registration" style="text-align: center; margin-top: 1.5rem;">
                    <span>Não tem uma conta? <a tabindex="6" href="${url.registrationUrl}">Cadastre-se</a></span>
                </div>
            </#if>
        </form>
    </div>
</body>
</html>
