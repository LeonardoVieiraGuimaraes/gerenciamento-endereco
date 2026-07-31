<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>Gerenciamento de Endereços - Keycloak Auth Server</title>
    <style>
        body {
            font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
            background: radial-gradient(circle at 10% 20%, rgba(59, 130, 246, 0.15) 0%, transparent 40%),
                        radial-gradient(circle at 90% 80%, rgba(99, 102, 241, 0.15) 0%, transparent 40%),
                        #0b0f17;
            color: #f8fafc;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            margin: 0;
        }
        .welcome-card {
            background: rgba(17, 24, 39, 0.75);
            backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.12);
            border-radius: 24px;
            padding: 3rem;
            max-width: 500px;
            text-align: center;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.5);
        }
        h1 { font-size: 2rem; font-weight: 800; color: #38bdf8; margin-bottom: 0.5rem; }
        p { color: #94a3b8; font-size: 1rem; line-height: 1.6; margin-bottom: 2rem; }
        .btn {
            display: inline-block;
            background: #2563eb;
            color: #ffffff;
            font-weight: 600;
            padding: 0.85rem 1.75rem;
            border-radius: 12px;
            text-decoration: none;
            transition: all 0.2s ease;
        }
        .btn:hover { background: #1d4ed8; transform: translateY(-2px); }
    </style>
</head>
<body>
    <div class="welcome-card">
        <h1>📍 Gerenciamento de Endereços</h1>
        <p>Servidor de Autenticação & Autorização Keycloak ativo e operacional.</p>
        <a href="/admin/" class="btn">Acessar Console Admin</a>
    </div>
</body>
</html>
