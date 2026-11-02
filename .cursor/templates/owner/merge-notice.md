# Merge notice (chat, Russian)
After every merge (including Dependabot). Several merges of one wake go in one message. Rules: [stage 10](../../lifecycle/10-review-and-merge.md#4-merge).

```markdown
Слито: <PR #NN «заголовок»> (<ссылка>), задача #NN.
Что меняется: <одна фраза для владельца>.
Дальше: деплой на dev, затем приёмка | <другое>.
```

Non-product issue whose criteria need no dev check (no Приёмка): the last line is
```markdown
Дальше: деплой на dev, затем проверка критериев и закрытие из «Разработка».
```
