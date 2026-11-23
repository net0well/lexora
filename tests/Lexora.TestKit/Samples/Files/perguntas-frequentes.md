# Perguntas frequentes

Respostas rápidas sobre o uso da central de documentos.

## Como envio um arquivo?

Use o botão **Enviar** na página da base e escolha um arquivo PDF, DOCX, XLSX, TXT, MD ou CSV.
O limite é de 50 MB por arquivo.

- Arquivos protegidos por senha não são aceitos.
- Planilhas muito grandes podem ser recusadas.

## Quanto tempo leva a indexação?

Em geral, poucos minutos. Exemplo de consulta pela API:

```bash
curl -H "Authorization: Bearer <token>" https://lexora.example.com/api/v1/documents/<id>
```

| Status | Significado |
|---|---|
| indexed | Pronto para busca |
| failed | Veja o motivo informado |
