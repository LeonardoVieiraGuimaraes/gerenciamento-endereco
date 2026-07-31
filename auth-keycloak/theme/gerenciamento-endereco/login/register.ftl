<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Cadastro - ${realm.displayName!''}</title>
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
    <div class="card-pf" style="max-width: 500px;">
        <div class="brand-mark">
            <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" fill="currentColor" viewBox="0 0 16 16">
                <path d="M8 16s6-5.686 6-10A6 6 0 0 0 2 6c0 4.314 6 10 6 10m0-7a3 3 0 1 1 0-6 3 3 0 0 1 0 6"/>
            </svg>
        </div>
        <span class="brand-kicker">Gerenciamento de Endereços</span>
        <h1 id="kc-page-title">Criar sua conta</h1>

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
