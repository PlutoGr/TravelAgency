# Commits
Template: [commit](../../templates/github/commit.md). Russian, impersonal, technical identifiers as in code.

- Title `<тип>(<сервис>): <что сделано>`; the squash commit adds ` (#NN)`.
- Types: `разработка`, `исправление`, `тесты`, `аналитика`, `инфраструктура`, `зависимости`, `документация`.
- Services: `чат`, `каталог`, `бронирование`, `identity`, `медиа`, `шлюз`, `фронт`, `ci`, `деплой`, `бд`, `общее`, `процесс`; wiki `вики`.
- Body for non-trivial commits: «Зачем», «Как работает», «Задача: #NN».
- Never `closes`/`fix`/`fixes` keywords; no `Co-authored-by` for bots.
- Dependabot `commit-message.prefix` per ecosystem (no `include: scope`): nuget `зависимости(общее)`, npm `зависимости(фронт)`, docker `зависимости(деплой)`, github-actions `зависимости(ci)` (`.github/dependabot.yml`).
