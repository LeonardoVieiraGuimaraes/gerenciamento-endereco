<#macro emailLayout>
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <style type="text/css">
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #0f172a; color: #f8fafc; margin: 0; padding: 40px 20px; }
        .email-container { max-width: 600px; margin: 0 auto; background: #1e293b; border-radius: 16px; border: 1px solid rgba(148, 163, 184, 0.2); overflow: hidden; box-shadow: 0 10px 30px rgba(0,0,0,0.5); }
        .email-header { background: #0f172a; padding: 24px; text-align: center; border-bottom: 1px solid rgba(148, 163, 184, 0.2); }
        .email-title { font-size: 20px; font-weight: 800; color: #3b82f6; text-decoration: none; }
        .email-body { padding: 32px; line-height: 1.6; font-size: 15px; color: #cbd5e1; }
        .email-footer { background: #0f172a; padding: 20px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid rgba(148, 163, 184, 0.1); }
        .button { display: inline-block; background-color: #3b82f6; color: #ffffff !important; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: 600; margin-top: 16px; }
    </style>
</head>
<body>
    <div class="email-container">
        <div class="email-header">
            <span class="email-title">📍 Gerenciamento de Endereços</span>
        </div>
        <div class="email-body">
            <#nested>
        </div>
        <div class="email-footer">
            &copy; ${.now?string('yyyy')} Gerenciamento de Endereços. Todos os direitos reservados.
        </div>
    </div>
</body>
</html>
</#macro>
