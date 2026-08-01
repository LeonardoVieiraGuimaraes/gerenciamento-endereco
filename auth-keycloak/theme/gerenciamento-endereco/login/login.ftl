<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>${msg("loginTitle",(realm.displayName!''))}</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="${url.resourcesPath}/css/glass-theme.css">
    <link rel="icon" type="image/svg+xml" href="${url.resourcesPath}/img/favicon.svg" />
    <!-- Tema claro/escuro + olho da senha (compartilhado com as telas do tema base) -->
    <script src="${url.resourcesPath}/js/theme-toggle.js"></script>
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
<body class="login-pf-page">
    <div class="card-pf">
        <div class="brand-mark">
            <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" fill="currentColor" viewBox="0 0 16 16">
                <path d="M8 16s6-5.686 6-10A6 6 0 0 0 2 6c0 4.314 6 10 6 10m0-7a3 3 0 1 1 0-6 3 3 0 0 1 0 6"/>
            </svg>
        </div>
        <span class="brand-kicker">Gerenciamento de Endereços</span>
        <h1 id="kc-page-title">Entrar na sua conta</h1>

        <#if message?has_content && (message.type != 'warning' || !isAppInitiatedAction??)>
            <div class="alert alert-${message.type}">
                ${kcSanitize(message.summary)?no_esc}
            </div>
        </#if>

        <form id="kc-form-login" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">
            <div class="form-group" style="margin-bottom: 1.5rem;">
                <label for="username" class="control-label">Usuário</label>
                <div class="input-icon-wrap">
                    <svg class="input-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                        <path d="M8 8a3 3 0 1 0 0-6 3 3 0 0 0 0 6m2-3a2 2 0 1 1-4 0 2 2 0 0 1 4 0m4 8c0 1-1 1-1 1H3s-1 0-1-1 1-4 6-4 6 3 6 4m-1-.004c-.001-.246-.154-.986-.832-1.664C11.516 10.68 10.289 10 8 10s-3.516.68-4.168 1.332c-.678.678-.83 1.418-.832 1.664z"/>
                    </svg>
                    <input id="username" class="form-control" name="username" value="${(login.username!'')}" type="text" autofocus autocomplete="off" placeholder="Digite seu usuário..." />
                </div>
            </div>

            <div class="form-group" style="margin-bottom: 1.5rem;">
                <label for="password" class="control-label">Senha</label>
                <div class="input-icon-wrap">
                    <svg class="input-icon" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                        <path d="M8 1a2 2 0 0 1 2 2v4H6V3a2 2 0 0 1 2-2m3 6V3a3 3 0 0 0-6 0v4a2 2 0 0 0-2 2v5a2 2 0 0 0 2 2h6a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2"/>
                    </svg>
                    <input id="password" class="form-control" name="password" type="password" autocomplete="off" placeholder="Digite sua senha..." />
                </div>
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
                <div class="divider"><span>ou</span></div>
                <a tabindex="6" href="${url.registrationUrl}" class="btn-secondary">Criar uma conta</a>
            </#if>
        </form>
    </div>
</body>
</html>
