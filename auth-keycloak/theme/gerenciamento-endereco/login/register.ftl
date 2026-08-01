<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Cadastro - ${realm.displayName!''}</title>
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
    <div class="card-pf" style="max-width: 500px;">
        <div class="brand-mark">
            <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" fill="currentColor" viewBox="0 0 16 16">
                <path d="M8 16s6-5.686 6-10A6 6 0 0 0 2 6c0 4.314 6 10 6 10m0-7a3 3 0 1 1 0-6 3 3 0 0 1 0 6"/>
            </svg>
        </div>
        <span class="brand-kicker">Gerenciamento de Endereços</span>
        <h1 id="kc-page-title">Criar sua conta</h1>

        <#if message?has_content && (message.type != 'warning' || !isAppInitiatedAction??)>
            <div class="alert alert-${message.type}">
                ${kcSanitize(message.summary)?no_esc}
            </div>
        </#if>

        <form id="kc-register-form" action="${url.registrationAction}" method="post">
            <div class="form-group" style="margin-bottom: 1.2rem;">
                <label for="firstName" class="control-label">Nome</label>
                <input type="text" id="firstName" class="form-control" name="firstName" value="${(register.formData.firstName!'')}" autocomplete="given-name" placeholder="Digite seu primeiro nome..." />
            </div>

            <div class="form-group" style="margin-bottom: 1.2rem;">
                <label for="lastName" class="control-label">Sobrenome</label>
                <input type="text" id="lastName" class="form-control" name="lastName" value="${(register.formData.lastName!'')}" autocomplete="family-name" placeholder="Digite seu sobrenome..." />
            </div>

            <div class="form-group" style="margin-bottom: 1.2rem;">
                <label for="email" class="control-label">E-mail</label>
                <input type="email" id="email" class="form-control" name="email" value="${(register.formData.email!'')}" autocomplete="email" placeholder="Digite seu e-mail..." />
            </div>

            <#if !realm.registrationEmailAsUsername>
                <div class="form-group" style="margin-bottom: 1.2rem;">
                    <label for="username" class="control-label">Nome de Usuário</label>
                    <input type="text" id="username" class="form-control" name="username" value="${(register.formData.username!'')}" autocomplete="username" placeholder="Escolha um nome de usuário..." />
                </div>
            </#if>

            <#if passwordRequired??>
                <div class="form-group" style="margin-bottom: 1.2rem;">
                    <label for="password" class="control-label">Senha</label>
                    <input type="password" id="password" class="form-control" name="password" autocomplete="new-password" placeholder="Digite uma senha forte..." />
                </div>

                <div class="form-group" style="margin-bottom: 1.5rem;">
                    <label for="password-confirm" class="control-label">Confirmar Senha</label>
                    <input type="password" id="password-confirm" class="form-control" name="password-confirm" placeholder="Confirme sua senha..." />
                </div>
            </#if>

            <div class="form-group">
                <button type="submit" class="btn-primary">Finalizar Cadastro</button>
            </div>
            
            <div class="divider"><span>ou</span></div>
            <a tabindex="6" href="${url.loginUrl}" class="btn-secondary">Já tenho uma conta</a>
        </form>
    </div>
</body>
</html>
